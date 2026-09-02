export type ActivityKind = 'flight' | 'groundHandling'
export type TrackFileFormat = 'gpx' | 'igc'

export interface ActivitySummary {
  id: string
  type: ActivityKind
  name: string
  startedAt: string
  endedAt: string | null
  durationSeconds: number | null
}

export interface TrackInfo {
  filename: string
  format: TrackFileFormat
  sizeBytes: number
  sha256: string
}

export interface ActivityDetail {
  id: string
  type: ActivityKind
  name: string
  startedAt: string
  endedAt: string | null
  localDate: string
  localTz: string | null
  takeoffLocation: string | null
  landingLocation: string | null
  takeoffLat: number | null
  takeoffLon: number | null
  comment: string | null
  maxAltitudeM: number | null
  altitudeGainM: number | null
  distanceKm: number | null
  windSpeedKmh: number | null
  windDirection: number | null
  durationSeconds: number | null
  track: TrackInfo | null
  hasElevation: boolean
  equipmentIds: string[]
}

export interface ActivityListResponse {
  items: ActivitySummary[]
  nextCursor: string | null
}

export interface ActivityPayload {
  type: ActivityKind
  name: string
  startedAt: string
  endedAt: string | null
  localDate: string
  localTz: string | null
  takeoffLocation: string | null
  landingLocation: string | null
  comment: string | null
  windSpeedKmh: number | null
  windDirection: number | null
  equipmentIds: string[]
  takeoffLat?: number | null
  takeoffLon?: number | null
  maxAltitudeM?: number | null
  altitudeGainM?: number | null
  distanceKm?: number | null
}
