import { decodeSwrItem, type SwrItem } from './wp109'

export interface SwrFields {
  swrType: number | null; customerNo: string | null; opNo: string | null
  deviceNo: string | null; startTime: string | null; endTime: string | null
  customerLotNo: string | null; moNo: string | null; swrDesc: string | null; lotNos: string[]
}
export interface SwrVersionedItem extends SwrItem { version: string }
export interface SwrWriterOptions { fileSizeLimitBytes: number; promptForAbortReason: boolean; businessTimeZone: string }
export interface SwrNumberPreview { swrNo: string; businessTimeZone: string; reserved: false }
export interface SwrDevice { productNo: string; productVersion: string; productName: string; deviceNo: string | null }
export interface SwrDeviceList { items: SwrDevice[]; limit: number; hasMore: boolean }
export interface SwrVoidResult { swrNo: string; outcome: 'voided' | 'deleted'; saved: SwrVersionedItem | null; deletedCount: number; deletedDetails: number }
export type NewAttachmentAction = 'none' | 'replace'
export type EditAttachmentAction = 'keep' | 'clear' | 'replace'

export class SwrWriteError extends Error {
  constructor(message: string, readonly status: number | null, readonly unknownWrite = false) {
    super(message); this.name = 'SwrWriteError'
  }
}
const fields = ['swrType','customerNo','opNo','deviceNo','startTime','endTime','customerLotNo','moNo','swrDesc','lotNos']
function object(value: unknown): Record<string, unknown> {
  if (value === null || typeof value !== 'object' || Array.isArray(value)) throw new Error('回應格式不符契約。')
  return value as Record<string, unknown>
}
function keys(value: Record<string, unknown>, names: string[]): void {
  if (Object.keys(value).length !== names.length || names.some(n => !Object.hasOwn(value,n))) throw new Error('回應欄位不符契約。')
}
function text(value: unknown): string {
  if (typeof value !== 'string') throw new Error('文字格式不符契約。')
  for (const scalar of value) { const n = scalar.codePointAt(0)!; if (n >= 0xD800 && n <= 0xDFFF) throw new Error('文字包含無效Unicode。') }
  if (value.includes('\0')) throw new Error('文字包含不合法字元。')
  return value
}
function nativeText(value: unknown, maximum: number, required = false): string | null {
  if (value === null && !required) return null
  const s = text(value)
  if ([...s].length > maximum || required && /^ *$/.test(s)) throw new Error('文字不符合原生欄位限制。')
  return s
}
function integer(value: unknown, maximum: number): number {
  if (typeof value !== 'number' || !Number.isSafeInteger(value) || value < 0 || value > maximum) throw new Error('數值格式不符契約。')
  return value
}
function wall(value: unknown): string | null {
  if (value === null) return null
  const s = text(value), m = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})(?:\.(\d{1,6}))?$/.exec(s)
  if (!m) throw new Error('開始與結束時間須為無時區時間，最多六位小數。')
  const [y,mo,d,h,min,sec] = m.slice(1,7).map(Number)
  const leap = y % 4 === 0 && (y % 100 !== 0 || y % 400 === 0)
  if (y < 1 || mo < 1 || mo > 12 || d < 1 || d > [31,leap?29:28,31,30,31,30,31,31,30,31,30,31][mo-1] || h > 23 || min > 59 || sec > 59) throw new Error('日期時間不合法。')
  return s
}
function wallOrder(s: string): string { return s.slice(0,19) + '.' + (s.split('.')[1] || '').padEnd(6,'0') }
export function validateSwrFields(value: SwrFields): SwrFields {
  try {
    keys(object(value),fields)
    if (value.swrType !== 0 && value.swrType !== 1) throw new Error('請選擇SWRType。')
    nativeText(value.customerNo,50,true); nativeText(value.opNo,20,true); nativeText(value.swrDesc,400,true)
    nativeText(value.deviceNo,50); nativeText(value.customerLotNo,50); nativeText(value.moNo,50)
    const start = wall(value.startTime), end = wall(value.endTime)
    if ((start === null) !== (end === null) || start !== null && end !== null && wallOrder(end) <= wallOrder(start)) throw new Error('開始及結束須一起設定，結束必須較晚。')
    if (!Array.isArray(value.lotNos) || value.lotNos.length !== 0) throw new Error('目前僅提供未指定Lot的單據寫入。')
    return { ...value, lotNos:[] }
  } catch (e) { throw new SwrWriteError(e instanceof Error ? e.message : '表單內容無效。',400) }
}
export function validSwrVersion(value: unknown): value is string { return typeof value === 'string' && /^wp109-v1:[a-f0-9]{64}$/.test(value) }
export function decodeVersionedSwr(value: unknown, rawKey?: string): SwrVersionedItem {
  const row = object(value); keys(row,['item','details','version'])
  const item = object(row.item), key = text(item.swrNo)
  nativeText(key,20,true)
  if (rawKey !== undefined && key !== rawKey || !validSwrVersion(row.version)) throw new Error('單張身份或版本不符原始資料。')
  return { ...decodeSwrItem({ item:row.item,details:row.details },key),version:row.version }
}
function decodeOptions(value: unknown): SwrWriterOptions {
  const row = object(value); keys(row,['fileSizeLimitBytes','promptForAbortReason','businessTimeZone'])
  integer(row.fileSizeLimitBytes,Number.MAX_SAFE_INTEGER)
  if (typeof row.promptForAbortReason !== 'boolean' || row.businessTimeZone !== 'Asia/Taipei') throw new Error('服務設定未就緒。')
  return row as unknown as SwrWriterOptions
}
function decodePreview(value: unknown): SwrNumberPreview {
  const row = object(value); keys(row,['swrNo','businessTimeZone','reserved'])
  if (!/^SWR\d{6}(?!000)\d{3}$/.test(text(row.swrNo)) || row.businessTimeZone !== 'Asia/Taipei' || row.reserved !== false) throw new Error('預覽單號格式不符契約。')
  return row as unknown as SwrNumberPreview
}
function decodeDevices(value: unknown, limit: number): SwrDeviceList {
  const row = object(value); keys(row,['items','limit','hasMore'])
  if (!Array.isArray(row.items) || row.limit !== limit || row.items.length > limit || typeof row.hasMore !== 'boolean') throw new Error('產品查找格式不符契約。')
  const items = row.items.map(value => { const r = object(value); keys(r,['productNo','productVersion','productName','deviceNo']); text(r.productNo); text(r.productVersion); text(r.productName); nativeText(r.deviceNo,50); return r as unknown as SwrDevice })
  return { items,limit,hasMore:row.hasMore }
}
function decodeVoid(value: unknown, rawKey: string, originalStatus: number): SwrVoidResult {
  const row = object(value); keys(row,['swrNo','outcome','saved','deletedCount','deletedDetails'])
  if (row.swrNo !== rawKey || row.deletedDetails !== 0) throw new Error('作廢回應身份不符。')
  if (originalStatus === 2) {
    const saved = decodeVersionedSwr(row.saved,rawKey)
    if (row.outcome !== 'voided' || row.deletedCount !== 0 || saved.item.status !== 99 || saved.details.length !== 0) throw new Error('作廢結果不符契約。')
    return { swrNo:rawKey,outcome:'voided',saved,deletedCount:0,deletedDetails:0 }
  }
  if (![0,99].includes(originalStatus) || row.outcome !== 'deleted' || row.saved !== null || row.deletedCount !== 1) throw new Error('刪除結果不符契約。')
  return { swrNo:rawKey,outcome:'deleted',saved:null,deletedCount:1,deletedDetails:0 }
}

export function createWp109WriteService(apiBaseUrl: string, origin: string, transport: typeof fetch = fetch) {
  if (!apiBaseUrl.trim()) throw new SwrWriteError('目前環境尚未設定API位址。',null)
  const base = new URL(apiBaseUrl.replace(/\/+$/,'')+'/',origin)
  if (base.origin !== new URL(origin).origin || !['https:','http:'].includes(base.protocol) || base.username || base.password || base.search || base.hash) throw new SwrWriteError('服務必須使用目前網站的API位址。',null)
  const url = (suffix = '', key?: string): URL => { const u = new URL('api/wip/special-work-requests'+suffix,base); if (key !== undefined) u.searchParams.set('swrNo',text(key)); return u }
  async function request<T>(u: URL, method: string, body: BodyInit | undefined, decode: (value: unknown) => T, external?: AbortSignal, json = true): Promise<T> {
    const write = method !== 'GET', controller = new AbortController(), cancel = () => controller.abort()
    if (external?.aborted) throw new SwrWriteError('讀取已取消。',null)
    external?.addEventListener('abort',cancel,{once:true})
    const timeout = setTimeout(cancel,write ? 60000 : 20000)
    try {
      let response: Response
      try { response = await transport(u.toString(),{ method,body,credentials:'same-origin',cache:'no-store',redirect:'error',signal:controller.signal,...(body !== undefined && json ? {headers:{'Content-Type':'application/json'}} : {}) }) }
      catch { throw new SwrWriteError(write ? '尚無法確認處理結果，請重新查詢核對，勿重複送出。' : '讀取未完成，請重試。',null,write) }
      if (!response.ok) {
        let detail: unknown
        try { detail = await response.json() } catch { detail = null }
        const row = detail && typeof detail === 'object' ? detail as Record<string,unknown> : {}
        const fallback: Record<number,string> = {400:'輸入格式無效。',401:'登入已失效，請重新登入。',403:'沒有操作權限。',404:'單據已不存在。',409:'資料已變更，請重新查詢核對。',413:'附件超過大小上限。',422:'表單內容不符合規則。',503:'必要服務尚未就緒，請重新查詢核對。'}
        throw new SwrWriteError(typeof row.detail === 'string' ? row.detail : typeof row.title === 'string' ? row.title : fallback[response.status] || '操作未完成。',response.status,write && response.status >= 500)
      }
      try { if (response.status !== 200) throw new Error('Unexpected success status'); return decode(await response.json()) }
      catch { throw new SwrWriteError(write ? '回應未能確認保存結果，請重新查詢核對，勿重複送出。' : '回應格式不符，請重試。',response.status,write) }
    } finally { clearTimeout(timeout); external?.removeEventListener('abort',cancel) }
  }
  function payload(value: SwrFields, action: NewAttachmentAction | EditAttachmentAction, file: File | null, version?: string): {body:BodyInit;json:boolean} {
    const fields = validateSwrFields(value)
    if (version !== undefined && !validSwrVersion(version)) throw new SwrWriteError('請重新載入完整單張版本。',400)
    if (version === undefined ? !['none','replace'].includes(action) : !['keep','clear','replace'].includes(action)) throw new SwrWriteError('附件動作無效。',400)
    if ((action === 'replace') !== (file !== null)) throw new SwrWriteError('附件動作與所選檔案不符。',400)
    const metadata = { ...fields,...(version !== undefined ? {version} : {}),attachmentAction:action }
    if (!file) return { body:JSON.stringify(metadata),json:true }
    const multipart = new FormData()
    multipart.append('metadata',new Blob([JSON.stringify(metadata)],{type:'application/json'}),'metadata.json')
    multipart.append('attachment',file,'attachment.pdf')
    return { body:multipart,json:false }
  }
  function commandVersion(version: string): void { if (!validSwrVersion(version)) throw new SwrWriteError('請重新載入完整單張版本。',400) }
  return {
    item:(key:string,signal:AbortSignal):Promise<SwrVersionedItem> => request(url('/versioned-item',key),'GET',undefined,v=>decodeVersionedSwr(v,key),signal),
    preview:(signal:AbortSignal):Promise<SwrNumberPreview> => request(url('/number-preview'),'GET',undefined,decodePreview,signal),
    options:(signal:AbortSignal):Promise<SwrWriterOptions> => request(url('/writer-options'),'GET',undefined,decodeOptions,signal),
    devices:(keyword:string,signal:AbortSignal,limit=200):Promise<SwrDeviceList> => { if (!Number.isInteger(limit) || limit < 1 || limit > 500) throw new SwrWriteError('查找上限須為1至500。',400); const u = url('/devices'); u.searchParams.set('keyword',text(keyword)); u.searchParams.set('limit',String(limit)); return request(u,'GET',undefined,v=>decodeDevices(v,limit),signal) },
    create:(value:SwrFields,action:NewAttachmentAction,file:File|null,signal?:AbortSignal):Promise<SwrVersionedItem> => { const p=payload(value,action,file); return request(url(),'POST',p.body,v=>{const r=decodeVersionedSwr(v);if(r.item.status!==0||r.details.length)throw new Error('Create status');return r},signal,p.json) },
    edit:(key:string,value:SwrFields,version:string,action:EditAttachmentAction,file:File|null,signal?:AbortSignal):Promise<SwrVersionedItem> => { const p=payload(value,action,file,version); return request(url('/item',key),'PUT',p.body,v=>{const r=decodeVersionedSwr(v,key);if(r.item.status!==0||r.details.length)throw new Error('Edit status');return r},signal,p.json) },
    approve:(key:string,version:string,signal?:AbortSignal):Promise<SwrVersionedItem> => { commandVersion(version); return request(url('/approve',key),'POST',JSON.stringify({version}),v=>{const r=decodeVersionedSwr(v,key);if(r.item.status!==2||r.details.length)throw new Error('Approve status');return r},signal) },
    void:(key:string,version:string,reason:string,originalStatus:number,signal?:AbortSignal):Promise<SwrVoidResult> => { commandVersion(version); text(reason); return request(url('/void',key),'POST',JSON.stringify({version,abortReason:reason}),v=>decodeVoid(v,key,originalStatus),signal) },
  }
}
