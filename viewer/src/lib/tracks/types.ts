export interface TrackPoint {
  lat: number
  lon: number
  /** ISO 8601 UTC. */
  time: string
  /** GPS altitude — use when comparing against terrain. Null when the source has no altitude. */
  gpsElevation: number | null
  /** Pressure altitude — use for gain and climb rate. Falls back to gpsElevation for GPX, which has only one channel. */
  baroElevation: number | null
}

export interface ParsedTrack {
  points: TrackPoint[]
}

export interface TrackStats {
  startedAt: string
  endedAt: string
  durationSeconds: number
  maxAltitudeM: number | null
  altitudeGainM: number | null
  distanceKm: number
  takeoffLat: number | null
  takeoffLon: number | null
}
