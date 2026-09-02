export interface TrackPoint {
  lat: number
  lon: number
  /** ISO 8601 UTC. Null when the source has no per-point timestamp (some GPX exports omit it). */
  time: string | null
  /** GPS altitude — use when comparing against terrain. Null when the source has no altitude. */
  gpsElevation: number | null
  /** Pressure altitude — use for gain and climb rate. Falls back to gpsElevation for GPX, which has only one channel. */
  baroElevation: number | null
}

export interface ParsedTrack {
  points: TrackPoint[]
}

export interface TrackStats {
  /** Null when no point in the track carries a timestamp. */
  startedAt: string | null
  endedAt: string | null
  durationSeconds: number | null
  maxAltitudeM: number | null
  altitudeGainM: number | null
  distanceKm: number
  takeoffLat: number | null
  takeoffLon: number | null
}
