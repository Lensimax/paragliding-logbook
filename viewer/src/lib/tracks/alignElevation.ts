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
 */
export function alignGroundElevation(points: TrackPoint[], samples: ElevationSample[]): (number | null)[] {
  if (samples.length === 0) return points.map(() => null)

  return points.map((point) => {
    let closest = samples[0]
    let closestDistance = squaredDistance(point, closest)

    for (let i = 1; i < samples.length; i++) {
      const distance = squaredDistance(point, samples[i])
      if (distance < closestDistance) {
        closestDistance = distance
        closest = samples[i]
      }
    }

    return closest.elevationM
  })
}

function squaredDistance(point: TrackPoint, sample: ElevationSample): number {
  const dLat = point.lat - sample.lat
  const dLon = point.lon - sample.lon
  return dLat * dLat + dLon * dLon
}
