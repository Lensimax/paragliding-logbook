export type ActivityKind = 'flight' | 'groundHandling'

export interface ActivitySummary {
  id: string
  type: ActivityKind
  name: string
  startedAt: string
  endedAt: string | null
  durationSeconds: number | null
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
  comment: string | null
  windSpeedKmh: number | null
  windDirection: number | null
  durationSeconds: number | null
  hasTrack: boolean
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
}
