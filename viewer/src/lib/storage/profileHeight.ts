const STORAGE_KEY = 'paraglog.profileHeight'

export function loadProfileHeight(): number | null {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) return null
  const value = Number(raw)
  return Number.isFinite(value) ? value : null
}

export function saveProfileHeight(height: number): void {
  localStorage.setItem(STORAGE_KEY, String(height))
}
