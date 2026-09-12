import { describe, expect, it } from 'vitest'
import { alignGroundElevation } from './alignElevation'
import type { TrackPoint } from './types'

function point(lat: number, lon: number): TrackPoint {
  return { lat, lon, time: '2026-01-01T00:00:00.000Z', gpsElevation: null, baroElevation: null }
}

describe('alignGroundElevation', () => {
  it('maps each point onto the sample at the same fraction of the track', () => {
    const points = [point(0, 0), point(10, 10)]
    const samples = [
      { lat: 0, lon: 0, elevationM: 100 },
      { lat: 10, lon: 10, elevationM: 900 },
    ]

    expect(alignGroundElevation(points, samples)).toEqual([100, 900])
  })

  it('linearly interpolates between the two bracketing samples', () => {
    // 5 points span the same range as 3 samples one-to-one at indices 0, 2, 4 -> fractions
    // 0, 0.5, 1 map onto sample fractions 0, 1, 2, i.e. sample indices 0, 1, 2 exactly, while
    // the in-between points (indices 1 and 3) fall halfway between two samples.
    const points = [point(0, 0), point(0, 0), point(0, 0), point(0, 0), point(0, 0)]
    const samples = [
      { lat: 0, lon: 0, elevationM: 100 },
      { lat: 0, lon: 0, elevationM: 200 },
      { lat: 0, lon: 0, elevationM: 400 },
    ]

    expect(alignGroundElevation(points, samples)).toEqual([100, 150, 200, 300, 400])
  })

  it('returns all-null when there are no samples', () => {
    const points = [point(0, 0), point(1, 1)]

    expect(alignGroundElevation(points, [])).toEqual([null, null])
  })

  it('returns null for a single, unresolved sample', () => {
    const points = [point(0, 0)]
    const samples = [{ lat: 0, lon: 0, elevationM: null }]

    expect(alignGroundElevation(points, samples)).toEqual([null])
  })

  it('falls back to the non-null neighbor when one bracketing sample is unresolved', () => {
    const points = [point(0, 0), point(0, 0), point(0, 0)]
    const samples = [
      { lat: 0, lon: 0, elevationM: 100 },
      { lat: 0, lon: 0, elevationM: null },
      { lat: 0, lon: 0, elevationM: 300 },
    ]

    expect(alignGroundElevation(points, samples)).toEqual([100, null, 300])
  })

  it('stays chronologically aligned when the track loops back near an earlier position', () => {
    // A landing pattern circling back close to takeoff no longer matters - alignment is purely
    // by index fraction now, so the geographic proximity of point 3 to point 0 is irrelevant.
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
