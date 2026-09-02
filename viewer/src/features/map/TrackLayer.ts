import L from 'leaflet'
import { simplifyTrack } from '../../lib/tracks/simplify'
import type { TrackPoint } from '../../lib/tracks/types'

// ~5m at the equator. Full resolution stays in the parsed track for stats/profile use;
// only the rendered layer is simplified, per SPEC.md.
const SIMPLIFY_TOLERANCE_DEGREES = 0.00005

export function createTrackLayer(points: TrackPoint[]): L.Polyline {
  const simplified = simplifyTrack(points, SIMPLIFY_TOLERANCE_DEGREES)
  const latLngs = simplified.map((p) => L.latLng(p.lat, p.lon))
  return L.polyline(latLngs, { color: '#e2431e', weight: 3 })
}

export function trackBounds(points: TrackPoint[]): L.LatLngBounds {
  return L.latLngBounds(points.map((p) => L.latLng(p.lat, p.lon)))
}
