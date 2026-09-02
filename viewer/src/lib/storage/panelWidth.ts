const STORAGE_KEY = 'paraglog.panelWidth'

export function loadPanelWidth(): number | null {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) return null
  const value = Number(raw)
  return Number.isFinite(value) ? value : null
}

export function savePanelWidth(width: number): void {
  localStorage.setItem(STORAGE_KEY, String(width))
}
