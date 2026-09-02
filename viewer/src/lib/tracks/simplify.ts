import type { TrackPoint } from './types'

function perpendicularDistance(point: TrackPoint, start: TrackPoint, end: TrackPoint): number {
  const dx = end.lat - start.lat
  const dy = end.lon - start.lon

  if (dx === 0 && dy === 0) {
    return Math.hypot(point.lat - start.lat, point.lon - start.lon)
  }

  const t = ((point.lat - start.lat) * dx + (point.lon - start.lon) * dy) / (dx * dx + dy * dy)
  const clampedT = Math.max(0, Math.min(1, t))
  const projLat = start.lat + clampedT * dx
  const projLon = start.lon + clampedT * dy

  return Math.hypot(point.lat - projLat, point.lon - projLon)
}

/**
 * Douglas-Peucker simplification for map rendering. `toleranceDegrees` is a perpendicular-distance
 * threshold in decimal degrees (roughly: 0.00009 ≈ 10m at the equator). Points within tolerance of
 * the line between their neighbors are dropped; the endpoints are always kept.
 */
export function simplifyTrack(points: TrackPoint[], toleranceDegrees: number): TrackPoint[] {
  if (points.length <= 2) return points

  const start = points[0]
  const end = points[points.length - 1]

  let maxDistance = 0
  let maxIndex = 0
  for (let i = 1; i < points.length - 1; i++) {
    const distance = perpendicularDistance(points[i], start, end)
    if (distance > maxDistance) {
      maxDistance = distance
      maxIndex = i
    }
  }

  if (maxDistance <= toleranceDegrees) {
    return [start, end]
  }

  const left = simplifyTrack(points.slice(0, maxIndex + 1), toleranceDegrees)
  const right = simplifyTrack(points.slice(maxIndex), toleranceDegrees)
  return [...left.slice(0, -1), ...right]
}
