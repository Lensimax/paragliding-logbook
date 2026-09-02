import { useEffect, useState } from 'react'

/** True while the viewport is at or below `maxWidthPx`. */
export function useBreakpoint(maxWidthPx: number): boolean {
  const query = `(max-width: ${maxWidthPx}px)`
  const [matches, setMatches] = useState(() => window.matchMedia(query).matches)

  useEffect(() => {
    const mql = window.matchMedia(query)
    const onChange = () => setMatches(mql.matches)
    onChange()
    mql.addEventListener('change', onChange)
    return () => mql.removeEventListener('change', onChange)
  }, [query])

  return matches
}
