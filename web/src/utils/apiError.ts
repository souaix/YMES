/** Keep upstream HTML error pages out of data-entry forms. */
export async function apiErrorMessage(response: Response): Promise<string> {
  const fallback = `資料服務回應異常（HTTP ${response.status}），請重新查詢確認資料後再操作。`
  const text = await response.text()
  const isHtml = response.headers.get('content-type')?.toLowerCase().includes('text/html')
    || /^\s*(?:<!doctype\s+html|<html)/i.test(text)
  if (isHtml) return fallback
  try {
    const problem: unknown = JSON.parse(text)
    if (problem && typeof problem === 'object') {
      const fields = problem as Record<string, unknown>
      for (const key of ['detail', 'message', 'title']) {
        const value = fields[key]
        if (typeof value === 'string' && value.trim()) return value.trim()
      }
    }
  } catch { /* A short plain-text upstream error is still useful. */ }
  return text.trim() && text.length <= 500 ? text.trim() : fallback
}
