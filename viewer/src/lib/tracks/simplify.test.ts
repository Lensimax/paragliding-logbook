import { describe, expect, it } from 'vitest'
import sampleGpx from './fixtures/sample.gpx?raw'
import { parseGpx } from './parseGpx'
import { simplifyTrack } from './simplify'
import type { TrackPoint } from './types'

function point(lat: number, lon: number): TrackPoint {
  return { lat, lon, time: '2026-01-01T00:00:00.000Z', gpsElevation: null, baroElevation: null }
}

describe('simplifyTrack', () => {
  it('keeps tracks of two points or fewer unchanged', () => {
    const points = [point(0, 0), point(1, 1)]
    expect(simplifyTrack(points, 1)).toEqual(points)
  })

  it('collapses points that lie on a straight line down to the endpoints', () => {
    const points = [point(0, 0), point(1, 1), point(2, 2), point(3, 3)]
    const simplified = simplifyTrack(points, 0.0001)

    expect(simplified).toEqual([points[0], points[3]])
  })

  it('keeps a point that deviates from the line beyond the tolerance', () => {
    const points = [point(0, 0), point(1, 5), point(2, 0)]
    const simplified = simplifyTrack(points, 0.5)

    expect(simplified).toHaveLength(3)
    expect(simplified[1]).toEqual(points[1])
  })

  it('drops a deviation smaller than the tolerance', () => {
    const points = [point(0, 0), point(1, 0.001), point(2, 0)]
    const simplified = simplifyTrack(points, 0.1)

    expect(simplified).toEqual([points[0], points[2]])
  })

  it('always keeps the first and last point of a real track', () => {
    const track = parseGpx(sampleGpx)
    const simplified = simplifyTrack(track.points, 0.01)

    expect(simplified[0]).toBe(track.points[0])
    expect(simplified[simplified.length - 1]).toBe(track.points[track.points.length - 1])
    expect(simplified.length).toBeLessThanOrEqual(track.points.length)
  })
})
