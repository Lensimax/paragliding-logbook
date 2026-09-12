import type { TrackPoint } from './types'

export interface ElevationSample {
  lat: number
  lon: number
  elevationM: number | null
}

/**
 * Ground elevation is resolved server-side for a sparse, downsampled subset of the track
 * (backend/src/ParagLog.Infrastructure/Elevation/TrackDownsampler.cs), while the flight-altitude
 * series uses the full-resolution track. Both arrays walk the same chronological path start to
 * end, so each point's ground elevation is linearly interpolated between the two samples that
 * bracket its position (by index fraction, not lat/lon) - this both aligns the two series onto
 * one x-axis and smooths over the gaps left by a deliberately sparse sample set, instead of the
 * stair-stepped line a nearest-sample lookup would produce.
 */
export function alignGroundElevation(points: TrackPoint[], samples: ElevationSample[]): (number | null)[] {
  if (points.length === 0 || samples.length === 0) return points.map(() => null)

  const lastPointIndex = points.length - 1
  const lastSampleIndex = samples.length - 1

  return points.map((_, i) => {
    const position = lastPointIndex === 0 ? 0 : (i / lastPointIndex) * lastSampleIndex
    const lowerIndex = Math.floor(position)
    const upperIndex = Math.min(lowerIndex + 1, lastSampleIndex)
    const fraction = position - lowerIndex
    const lower = samples[lowerIndex].elevationM

    // Exactly on a sample (including both track endpoints): use it as-is, missing or not -
    // there's no second sample to interpolate from, and fabricating one would hide a genuine gap.
    if (fraction === 0 || lowerIndex === upperIndex) return lower

    const upper = samples[upperIndex].elevationM
    if (lower === null || upper === null) return null
    return lower + (upper - lower) * fraction
  })
}
