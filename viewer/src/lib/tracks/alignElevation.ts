import type { TrackPoint } from './types'

export interface ElevationSample {
  lat: number
  lon: number
  elevationM: number | null
}

/**
 * Ground elevation is resolved server-side for a sparse, downsampled subset of the track
 * (SPEC.md: 300-500 points), while the flight-altitude series uses the full-resolution track.
 * For each full-resolution point, finds the nearest elevation sample so both series share the
 * same x-axis without interpolation.
 *
 * The pointer into `samples` only ever advances, never rewinds: both arrays walk the same
 * chronological path, so a plain nearest-by-distance search over the *whole* samples array can
 * snap a late-flight point (e.g. a landing pattern circling back near takeoff, or a thermal the
 * pilot re-crosses) to an early-flight sample that happens to sit at a nearby lat/lon - producing
 * a corrupted, jumpy ground line near the end of the activity.
 */
export function alignGroundElevation(points: TrackPoint[], samples: ElevationSample[]): (number | null)[] {
  if (samples.length === 0) return points.map(() => null)

  let sampleIndex = 0
  return points.map((point) => {
    while (
      sampleIndex < samples.length - 1 &&
      squaredDistance(point, samples[sampleIndex + 1]) <= squaredDistance(point, samples[sampleIndex])
    ) {
      sampleIndex++
    }
    return samples[sampleIndex].elevationM
  })
}

function squaredDistance(point: TrackPoint, sample: ElevationSample): number {
  const dLat = point.lat - sample.lat
  const dLon = point.lon - sample.lon
  return dLat * dLat + dLon * dLon
}
