import type { ParsedTrack, TrackStats } from './types'

const EARTH_RADIUS_KM = 6371

function toRadians(degrees: number): number {
  return (degrees * Math.PI) / 180
}

function haversineKm(lat1: number, lon1: number, lat2: number, lon2: number): number {
  const dLat = toRadians(lat2 - lat1)
  const dLon = toRadians(lon2 - lon1)
  const a =
    Math.sin(dLat / 2) ** 2 + Math.cos(toRadians(lat1)) * Math.cos(toRadians(lat2)) * Math.sin(dLon / 2) ** 2
  return 2 * EARTH_RADIUS_KM * Math.asin(Math.sqrt(a))
}

/** Running distance (km) up to and including each point - used as the profile's x-axis when a track has no timestamps. */
export function cumulativeDistanceKm(points: { lat: number; lon: number }[]): number[] {
  const distances = [0]
  for (let i = 1; i < points.length; i++) {
    distances.push(distances[i - 1] + haversineKm(points[i - 1].lat, points[i - 1].lon, points[i].lat, points[i].lon))
  }
  return distances
}

/** Sum of positive altitude deltas between consecutive fixes. */
function totalGain(altitudes: number[]): number {
  let gain = 0
  for (let i = 1; i < altitudes.length; i++) {
    const delta = altitudes[i] - altitudes[i - 1]
    if (delta > 0) gain += delta
  }
  return gain
}

/**
 * Pure aggregation: ParsedTrack in, TrackStats out. Never assumes a fixed sample interval.
 * Distance and altitude stats don't need timestamps, so they're always computed; duration is
 * null when no point in the track carries a timestamp (some real-world GPX exports omit it).
 */
export function computeTrackStats(track: ParsedTrack): TrackStats {
  const { points } = track
  if (points.length === 0) {
    throw new Error('Cannot compute stats for an empty track.')
  }

  const first = points[0]

  const distances = cumulativeDistanceKm(points)
  const distanceKm = distances[distances.length - 1]

  // Pressure altitude for gain, per SPEC.md: "Use pressure altitude for gain and climb rate."
  const altitudes = points.map((p) => p.baroElevation).filter((a): a is number => a !== null)

  const timedPoints = points.filter((p) => p.time !== null)
  const hasTimes = timedPoints.length > 0
  const startedAt = hasTimes ? timedPoints[0].time : null
  const endedAt = hasTimes ? timedPoints[timedPoints.length - 1].time : null

  return {
    startedAt,
    endedAt,
    durationSeconds:
      startedAt !== null && endedAt !== null
        ? Math.max(0, Math.round((new Date(endedAt).getTime() - new Date(startedAt).getTime()) / 1000))
        : null,
    maxAltitudeM: altitudes.length > 0 ? Math.round(Math.max(...altitudes)) : null,
    altitudeGainM: altitudes.length > 0 ? Math.round(totalGain(altitudes)) : null,
    distanceKm: Math.round(distanceKm * 100) / 100,
    takeoffLat: first.lat,
    takeoffLon: first.lon,
  }
}
