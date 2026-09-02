import { describe, expect, it } from 'vitest'
import volBelvedere from './fixtures/vol-belvedere-maxime.gpx?raw'
import { parseGpx } from './parseGpx'
import { computeTrackStats } from './stats'

describe('a real Suunto app export (route points, no timestamps)', () => {
  it('parses via the <rte><rtept> fallback', () => {
    const track = parseGpx(volBelvedere)

    expect(track.points.length).toBeGreaterThan(1000)
    expect(track.points[0].lat).toBeCloseTo(45.094578, 5)
    expect(track.points[0].lon).toBeCloseTo(5.579108, 5)
  })

  it('has no timestamps, since this export omits <time> entirely', () => {
    const track = parseGpx(volBelvedere)

    expect(track.points.every((p) => p.time === null)).toBe(true)
  })

  it('still computes distance and altitude stats without timestamps', () => {
    const track = parseGpx(volBelvedere)
    const stats = computeTrackStats(track)

    expect(stats.startedAt).toBeNull()
    expect(stats.endedAt).toBeNull()
    expect(stats.durationSeconds).toBeNull()
    expect(stats.maxAltitudeM).not.toBeNull()
    expect(stats.distanceKm).toBeGreaterThan(0)
    expect(stats.takeoffLat).toBeCloseTo(45.094578, 5)
  })
})
