import { useCallback, useSyncExternalStore } from 'react'

// A single module-scoped value, not per-component state: the map marker and the profile
// cursor both read (and write) the same store, so hovering either one moves the other.
let cursorTimeOffsetSeconds: number | null = null
const listeners = new Set<() => void>()

function subscribe(listener: () => void) {
  listeners.add(listener)
  return () => listeners.delete(listener)
}

function getSnapshot() {
  return cursorTimeOffsetSeconds
}

/** Owns a single shared cursor position (seconds since activity start) for the map and profile. */
export function useSharedCursor() {
  const timeOffsetSeconds = useSyncExternalStore(subscribe, getSnapshot)

  const setTimeOffsetSeconds = useCallback((next: number | null) => {
    if (next === cursorTimeOffsetSeconds) return // breaks the setCursor <-> store update loop
    cursorTimeOffsetSeconds = next
    listeners.forEach((listener) => listener())
  }, [])

  return { timeOffsetSeconds, setTimeOffsetSeconds }
}

/** Test-only: the store is a module-level singleton, so tests must reset it between cases. */
export function resetSharedCursorForTests() {
  cursorTimeOffsetSeconds = null
}
