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

  it('keeps matching chronologically even when the track loops back near an earlier position', () => {
    // A landing pattern that circles back close to takeoff: point 3 sits right next to point 0,
    // but should still resolve to the late-flight sample (400), not snap back to the early one
    // (100), which is what a plain "nearest across the whole samples array" search would do.
    const points = [point(0, 0), point(1, 0), point(2, 0), point(0.05, 0.05)]
    const samples = [
      { lat: 0, lon: 0, elevationM: 100 },
      { lat: 1, lon: 0, elevationM: 200 },
      { lat: 2, lon: 0, elevationM: 300 },
      { lat: 0.1, lon: 0.1, elevationM: 400 },
    ]

    expect(alignGroundElevation(points, samples)).toEqual([100, 200, 300, 400])
  })
})
