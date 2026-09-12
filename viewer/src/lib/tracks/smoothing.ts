/**
 * Centered moving average, used to soften raw GPS/baro altitude jitter for display without
 * distorting real climbs/descents. `NaN` entries (missing altitude - see AltitudeProfile) are
 * excluded from each window's average rather than propagated, so a few gaps don't blank out
 * their neighbors too.
 */
export function movingAverage(values: number[], windowSize: number): number[] {
  if (windowSize <= 1) return values.slice()

  const half = Math.floor(windowSize / 2)
  return values.map((_, i) => {
    let sum = 0
    let count = 0
    for (let j = Math.max(0, i - half); j <= Math.min(values.length - 1, i + half); j++) {
      const v = values[j]
      if (!Number.isNaN(v)) {
        sum += v
        count++
      }
    }
    return count > 0 ? sum / count : NaN
  })
}
