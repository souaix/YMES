export type CapabilityIssueState = 0 | 1 | 2 | -1

export interface EquipmentCapabilityBasisRow {
  capItem: string
  description: string | null
  issueState: CapabilityIssueState
  creator: string
  createDate: string
  reviser: string | null
  reviseDate: string | null
  dataStamp: number
}

export interface EquipmentCapabilityBasisPage {
  page: number
  pageSize: number
  hasMore: boolean
  stateCounts: Record<string, number>
  items: EquipmentCapabilityBasisRow[]
}

export class EquipmentCapabilityBasisError extends Error {
  constructor(message: string, readonly status: number | null, readonly unknownWrite: boolean) {
    super(message)
    this.name = 'EquipmentCapabilityBasisError'
  }
}

function pageValue(value: unknown): EquipmentCapabilityBasisPage {
  if (!value || typeof value !== 'object' || Array.isArray(value)) throw new Error('服務回傳的清單格式不完整。')
  const row = value as Record<string, unknown>
  if (!Number.isSafeInteger(row.page) || !Number.isSafeInteger(row.pageSize) || typeof row.hasMore !== 'boolean' ||
      !row.stateCounts || typeof row.stateCounts !== 'object' || !Array.isArray(row.items)) {
    throw new Error('服務回傳的清單格式不完整。')
  }
  const stateCounts: Record<string, number> = { '0': 0, '1': 0, '2': 0, '-1': 0 }
  for (const [key, count] of Object.entries(row.stateCounts as Record<string, unknown>)) {
    if (!Object.hasOwn(stateCounts, key) || !Number.isSafeInteger(count) || (count as number) < 0) throw new Error('服務回傳的狀態筆數格式不完整。')
    stateCounts[key] = count as number
  }
  const items = (row.items as unknown[]).map(capabilityRow)
  return { page: row.page as number, pageSize: row.pageSize as number, hasMore: row.hasMore, stateCounts, items }
}

function capabilityRow(value: unknown): EquipmentCapabilityBasisRow {
  if (!value || typeof value !== 'object' || Array.isArray(value)) throw new Error('服務回傳的設備能力項目格式不完整。')
  const row = value as Record<string, unknown>
  const state = row.issueState
  if (typeof row.capItem !== 'string' || !(state === 0 || state === 1 || state === 2 || state === -1) ||
      typeof row.creator !== 'string' || typeof row.createDate !== 'string' ||
      !(row.description === null || typeof row.description === 'string') ||
      !(row.reviser === null || typeof row.reviser === 'string') ||
      !(row.reviseDate === null || typeof row.reviseDate === 'string') ||
      typeof row.dataStamp !== 'number' || !Number.isSafeInteger(row.dataStamp) || row.dataStamp < 0) {
    throw new Error('服務回傳的設備能力項目格式不完整。')
  }
  return row as unknown as EquipmentCapabilityBasisRow
}

export function createEmsEquipmentCapabilityBasisService(apiBaseUrl: string, origin: string, transport: typeof fetch = fetch) {
  const configured = apiBaseUrl.trim().replace(/\/+$/, '')
  if (!configured) throw new EquipmentCapabilityBasisError('目前環境尚未設定 API 位址。', null, false)
  const base = new URL(`${configured}/`, origin)
  if (!['https:', 'http:'].includes(base.protocol) || base.username || base.password || base.search || base.hash) {
    throw new EquipmentCapabilityBasisError('API 位址格式不正確。', null, false)
  }
  const endpoint = new URL('api/ems/equipment-capability-bases', base)

  async function call<T>(url: URL, method: string, decoder: (value: unknown) => T, body?: unknown): Promise<T> {
    const writing = method !== 'GET'
    let response: Response
    try {
      response = await transport(url, {
        method,
        credentials: 'include',
        cache: 'no-store',
        ...(body === undefined ? {} : { headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }),
      })
    } catch {
      throw new EquipmentCapabilityBasisError(writing ? '寫入結果尚未確認，請先重新查詢，不要直接重送。' : '目前無法取得資料，請重新查詢。', null, writing)
    }
    if (!response.ok) {
      const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null
      const message = problem?.detail || problem?.title || `操作未完成（${response.status}）。`
      throw new EquipmentCapabilityBasisError(message, response.status, writing && response.status >= 500)
    }
    if (response.status !== 200) throw new EquipmentCapabilityBasisError('服務未回傳完整操作結果，請重新查詢核對。', response.status, writing)
    try {
      return decoder(await response.json())
    } catch {
      throw new EquipmentCapabilityBasisError('服務回應未通過格式核對，請重新查詢實際結果。', response.status, writing)
    }
  }

  return {
    list(issueState: CapabilityIssueState, capItem: string, page = 1, pageSize = 50): Promise<EquipmentCapabilityBasisPage> {
      const url = new URL(endpoint)
      url.searchParams.set('issueState', String(issueState))
      if (capItem.trim()) url.searchParams.set('capItem', capItem.trim())
      url.searchParams.set('page', String(page))
      url.searchParams.set('pageSize', String(pageSize))
      return call(url, 'GET', pageValue)
    },
    create(capItem: string, description: string | null): Promise<EquipmentCapabilityBasisRow> {
      return call(endpoint, 'POST', capabilityRow, { capItem, description })
    },
    update(capItem: string, description: string | null, dataStamp: number): Promise<EquipmentCapabilityBasisRow> {
      const url = new URL('item', endpoint.toString().replace(/\/$/, '') + '/')
      url.searchParams.set('capItem', capItem)
      return call(url, 'PUT', capabilityRow, { description, dataStamp })
    },
    deleteBatch(items: Array<Pick<EquipmentCapabilityBasisRow, 'capItem' | 'issueState' | 'dataStamp'>>): Promise<{ processedCount: number; deletedCount: number; disabledCount: number }> {
      const url = new URL('batch-delete', endpoint.toString().replace(/\/$/, '') + '/')
      return call(url, 'POST', value => {
        if (!value || typeof value !== 'object' || Array.isArray(value)) throw new Error('刪除結果格式不完整。')
        const result = value as Record<string, unknown>
        if (result.processedCount !== items.length || !Number.isSafeInteger(result.deletedCount) || !Number.isSafeInteger(result.disabledCount)) {
          throw new Error('刪除結果筆數不一致。')
        }
        return { processedCount: result.processedCount as number, deletedCount: result.deletedCount as number, disabledCount: result.disabledCount as number }
      }, { items })
    },
    approve(capItem: string, dataStamp: number): Promise<EquipmentCapabilityBasisRow> {
      const url = new URL('approve', endpoint.toString().replace(/\/$/, '') + '/')
      url.searchParams.set('capItem', capItem)
      url.searchParams.set('dataStamp', String(dataStamp))
      return call(url, 'POST', capabilityRow)
    },
  }
}
