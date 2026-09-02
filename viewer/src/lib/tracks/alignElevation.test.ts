import { describe, expect, it } from 'vitest'
import { alignGroundElevation } from './alignElevation'
import type { TrackPoint } from './types'

function point(lat: number, lon: number): TrackPoint {
  return { lat, lon, time: '2026-01-01T00:00:00.000Z', gpsElevation: null, baroElevation: null }
}

describe('alignGroundElevation', () => {
  it('matches each point to its nearest elevation sample', () => {
    const points = [point(0, 0), point(10, 10)]
    const samples = [
      { lat: 0.1, lon: 0.1, elevationM: 100 },
      { lat: 9.9, lon: 9.9, elevationM: 900 },
    ]

    expect(alignGroundElevation(points, samples)).toEqual([100, 900])
  })

  it('returns all-null when there are no samples', () => {
    const points = [point(0, 0), point(1, 1)]

    expect(alignGroundElevation(points, [])).toEqual([null, null])
  })

  it('passes through a null elevation from an unresolved sample', () => {
    const points = [point(0, 0)]
    const samples = [{ lat: 0, lon: 0, elevationM: null }]

    expect(alignGroundElevation(points, samples)).toEqual([null])
  })
})
