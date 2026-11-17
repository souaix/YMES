export const materialImportColumns = [
  'MaterialType', 'MaterialNo', 'MaterialName', 'PutInPlace', 'CountWay',
  'CheckLotNo', 'UnitType', 'UnitNo', 'ERPUnitNo',
] as const

function descendants(parent: Document | Element, name: string): Element[] {
  return Array.from(parent.getElementsByTagName('*')).filter((element) => element.localName === name)
}

function normalizePath(path: string): string {
  const parts: string[] = []
  for (const part of path.replace(/^\//, '').split('/')) {
    if (!part || part === '.') continue
    if (part === '..') parts.pop()
    else parts.push(part)
  }
  return parts.join('/')
}

async function readZipEntries(buffer: ArrayBuffer): Promise<Map<string, string>> {
  const view = new DataView(buffer)
  const bytes = new Uint8Array(buffer)
  const lowerBound = Math.max(0, bytes.length - 65_557)
  let endOffset = -1
  for (let offset = bytes.length - 22; offset >= lowerBound; offset--) {
    if (view.getUint32(offset, true) === 0x06054b50) { endOffset = offset; break }
  }
  if (endOffset < 0) throw new Error('Excel 檔案不是有效的 XLSX 壓縮檔。')

  const count = view.getUint16(endOffset + 10, true)
  let offset = view.getUint32(endOffset + 16, true)
  const decoder = new TextDecoder()
  const entries = new Map<string, string>()
  for (let index = 0; index < count; index++) {
    if (view.getUint32(offset, true) !== 0x02014b50) throw new Error('Excel 檔案目錄格式錯誤。')
    const method = view.getUint16(offset + 10, true)
    const compressedSize = view.getUint32(offset + 20, true)
    const nameLength = view.getUint16(offset + 28, true)
    const extraLength = view.getUint16(offset + 30, true)
    const commentLength = view.getUint16(offset + 32, true)
    const localOffset = view.getUint32(offset + 42, true)
    const name = decoder.decode(bytes.subarray(offset + 46, offset + 46 + nameLength))
    if (view.getUint32(localOffset, true) !== 0x04034b50) throw new Error('Excel 檔案內容格式錯誤。')
    const localNameLength = view.getUint16(localOffset + 26, true)
    const localExtraLength = view.getUint16(localOffset + 28, true)
    const dataOffset = localOffset + 30 + localNameLength + localExtraLength
    const compressed = bytes.slice(dataOffset, dataOffset + compressedSize)
    let content: Uint8Array
    if (method === 0) content = compressed
    else if (method === 8) {
      const stream = new DecompressionStream('deflate-raw')
      const writer = stream.writable.getWriter()
      await writer.write(compressed.buffer.slice(compressed.byteOffset, compressed.byteOffset + compressed.byteLength) as ArrayBuffer)
      await writer.close()
      content = new Uint8Array(await new Response(stream.readable).arrayBuffer())
    } else throw new Error(`Excel 壓縮格式 ${method} 尚未支援。`)
    entries.set(name, decoder.decode(content))
    offset += 46 + nameLength + extraLength + commentLength
  }
  return entries
}

function parseXml(source: string, description: string): Document {
  const document = new DOMParser().parseFromString(source, 'application/xml')
  if (document.querySelector('parsererror')) throw new Error(`${description}內容格式錯誤。`)
  return document
}

function columnIndex(reference: string): number {
  const letters = reference.match(/^[A-Z]+/i)?.[0]?.toUpperCase() ?? ''
  let value = 0
  for (const letter of letters) value = value * 26 + letter.charCodeAt(0) - 64
  return Math.max(0, value - 1)
}

export async function parseFirstWorksheetXlsx(file: File): Promise<string[][]> {
  const entries = await readZipEntries(await file.arrayBuffer())
  const workbookText = entries.get('xl/workbook.xml')
  const relationsText = entries.get('xl/_rels/workbook.xml.rels')
  if (!workbookText || !relationsText) throw new Error('找不到 Excel 活頁簿或工作表。')
  const workbook = parseXml(workbookText, 'Excel 活頁簿')
  const relations = parseXml(relationsText, 'Excel 工作表關聯')
  const firstSheet = descendants(workbook, 'sheet')[0]
  if (!firstSheet) throw new Error('Excel 檔案沒有工作表。')
  const relationshipId = firstSheet.getAttribute('r:id')
    ?? firstSheet.getAttributeNS('http://schemas.openxmlformats.org/officeDocument/2006/relationships', 'id')
  const relationship = descendants(relations, 'Relationship').find((element) => element.getAttribute('Id') === relationshipId)
  const target = relationship?.getAttribute('Target')
  if (!target) throw new Error('找不到 Excel 第一個工作表。')
  const sheetText = entries.get(normalizePath(target.startsWith('/') ? target : `xl/${target}`))
  if (!sheetText) throw new Error('無法讀取 Excel 第一個工作表。')

  const sharedText = entries.get('xl/sharedStrings.xml')
  const sharedStrings = sharedText
    ? descendants(parseXml(sharedText, 'Excel 共用字串'), 'si').map((item) => descendants(item, 't').map((text) => text.textContent ?? '').join(''))
    : []
  const sheet = parseXml(sheetText, 'Excel 工作表')
  const rows = descendants(sheet, 'row').filter((row) => row.parentElement?.localName === 'sheetData')
  return rows.map((row) => {
    const values: string[] = []
    for (const cell of Array.from(row.children).filter((child) => child.localName === 'c')) {
      const index = columnIndex(cell.getAttribute('r') ?? '')
      const type = cell.getAttribute('t')
      const value = Array.from(cell.children).find((child) => child.localName === 'v')?.textContent ?? ''
      const text = type === 's' ? (sharedStrings[Number(value)] ?? '')
        : type === 'inlineStr' ? descendants(cell, 't').map((item) => item.textContent ?? '').join('')
          : value
      while (values.length <= index) values.push('')
      values[index] = text.trim()
    }
    return values
  })
}

export const parseMaterialXlsx = parseFirstWorksheetXlsx

export function parseMaterialCsv(source: string): string[][] {
  const rows: string[][] = []
  let row: string[] = []
  let cell = ''
  let quoted = false
  const text = source.replace(/^\uFEFF/, '')
  for (let index = 0; index < text.length; index++) {
    const character = text[index]
    if (character === '"' && quoted && text[index + 1] === '"') { cell += '"'; index++ }
    else if (character === '"') quoted = !quoted
    else if (character === ',' && !quoted) { row.push(cell.trim()); cell = '' }
    else if ((character === '\n' || character === '\r') && !quoted) {
      if (character === '\r' && text[index + 1] === '\n') index++
      row.push(cell.trim())
      if (row.some(Boolean)) rows.push(row)
      row = []
      cell = ''
    } else cell += character
  }
  row.push(cell.trim())
  if (row.some(Boolean)) rows.push(row)
  return rows
}

function xmlEscape(value: string): string {
  return value.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&apos;')
}

function crc32(bytes: Uint8Array): number {
  let crc = 0xffffffff
  for (const byte of bytes) {
    crc ^= byte
    for (let bit = 0; bit < 8; bit++) crc = (crc >>> 1) ^ (crc & 1 ? 0xedb88320 : 0)
  }
  return (crc ^ 0xffffffff) >>> 0
}

function joinBytes(parts: Uint8Array[]): Uint8Array {
  const result = new Uint8Array(parts.reduce((sum, part) => sum + part.length, 0))
  let offset = 0
  for (const part of parts) { result.set(part, offset); offset += part.length }
  return result
}

function makeStoredZip(files: Array<{ name: string; content: string }>): Blob {
  const encoder = new TextEncoder()
  const localParts: Uint8Array[] = []
  const centralParts: Uint8Array[] = []
  let localOffset = 0
  for (const file of files) {
    const name = encoder.encode(file.name)
    const content = encoder.encode(file.content)
    const checksum = crc32(content)
    const local = new Uint8Array(30 + name.length)
    const localView = new DataView(local.buffer)
    localView.setUint32(0, 0x04034b50, true)
    localView.setUint16(4, 20, true)
    localView.setUint32(14, checksum, true)
    localView.setUint32(18, content.length, true)
    localView.setUint32(22, content.length, true)
    localView.setUint16(26, name.length, true)
    local.set(name, 30)
    localParts.push(local, content)

    const central = new Uint8Array(46 + name.length)
    const centralView = new DataView(central.buffer)
    centralView.setUint32(0, 0x02014b50, true)
    centralView.setUint16(4, 20, true)
    centralView.setUint16(6, 20, true)
    centralView.setUint32(16, checksum, true)
    centralView.setUint32(20, content.length, true)
    centralView.setUint32(24, content.length, true)
    centralView.setUint16(28, name.length, true)
    centralView.setUint32(42, localOffset, true)
    central.set(name, 46)
    centralParts.push(central)
    localOffset += local.length + content.length
  }
  const central = joinBytes(centralParts)
  const end = new Uint8Array(22)
  const endView = new DataView(end.buffer)
  endView.setUint32(0, 0x06054b50, true)
  endView.setUint16(8, files.length, true)
  endView.setUint16(10, files.length, true)
  endView.setUint32(12, central.length, true)
  endView.setUint32(16, localOffset, true)
  const bytes = Uint8Array.from(joinBytes([...localParts, central, end]))
  return new Blob([bytes.buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })
}

function xlsxColumnName(index: number): string {
    let value = index + 1
    let result = ''
    while (value > 0) {
      const remainder = (value - 1) % 26
      result = String.fromCharCode(65 + remainder) + result
      value = Math.floor((value - 1) / 26)
    }
    return result
}

export function createXlsxTemplate(columns: readonly string[], sheetName = 'Template', sampleRows: string[][] = []): Blob {
  const rows = [columns, ...sampleRows]
  const sheetRows = rows.map((values, rowIndex) => {
    const cells = values.map((value, columnIndex) => `<c r="${xlsxColumnName(columnIndex)}${rowIndex + 1}" t="inlineStr"><is><t>${xmlEscape(value)}</t></is></c>`).join('')
    return `<row r="${rowIndex + 1}">${cells}</row>`
  }).join('')
  return makeStoredZip([
    { name: '[Content_Types].xml', content: '<?xml version="1.0" encoding="UTF-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>' },
    { name: '_rels/.rels', content: '<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>' },
    { name: 'xl/workbook.xml', content: `<?xml version="1.0" encoding="UTF-8"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="${xmlEscape(sheetName.slice(0, 31))}" sheetId="1" r:id="rId1"/></sheets></workbook>` },
    { name: 'xl/_rels/workbook.xml.rels', content: '<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>' },
    { name: 'xl/worksheets/sheet1.xml', content: `<?xml version="1.0" encoding="UTF-8"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>${sheetRows}</sheetData></worksheet>` },
  ])
}

export function createMaterialImportTemplate(): Blob {
  return createXlsxTemplate(materialImportColumns, 'Material')
}

export function materialImportHeaderIndexes(header: string[]): number[] | null {
  const normalized = header.map((value) => value.replace(/[\s_-]/g, '').toLowerCase())
  const indexes = materialImportColumns.map((column) => normalized.indexOf(column.replace(/[\s_-]/g, '').toLowerCase()))
  return indexes.slice(0, 8).every((index) => index >= 0) ? indexes : null
}
