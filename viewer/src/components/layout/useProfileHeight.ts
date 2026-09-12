import { useCallback, useState } from 'react'
import { loadProfileHeight, saveProfileHeight } from '../../lib/storage/profileHeight'

const MIN_HEIGHT = 100
const DEFAULT_HEIGHT = 140
const MAX_RATIO = 0.6

function clamp(height: number): number {
  const max = Math.max(window.innerHeight * MAX_RATIO, MIN_HEIGHT)
  return Math.min(Math.max(height, MIN_HEIGHT), max)
}

export function useProfileHeight() {
  const [height, setHeightState] = useState(() => clamp(loadProfileHeight() ?? DEFAULT_HEIGHT))

  const setHeight = useCallback((next: number) => {
    const clamped = clamp(next)
    setHeightState(clamped)
    saveProfileHeight(clamped)
  }, [])

  return { height, setHeight }
}
