import { describe, expect, it } from 'vitest'
import sampleGpx from './fixtures/sample.gpx?raw'
import sampleIgc from './fixtures/sample.igc?raw'
import { parseGpx } from './parseGpx'
import { parseIgc } from './parseIgc'
import { computeTrackStats } from './stats'

describe('computeTrackStats', () => {
  it('computes duration, max altitude and gain from the sample GPX flight', () => {
    const track = parseGpx(sampleGpx)
    const stats = computeTrackStats(track)

    expect(stats.startedAt).toBe('2026-03-05T09:00:00.000Z')
    expect(stats.endedAt).toBe('2026-03-05T09:27:00.000Z')
    expect(stats.durationSeconds).toBe(27 * 60)
    // Ascends 1000 -> 1500 in five 100m steps, then glides down: gain is only the climb.
    expect(stats.maxAltitudeM).toBe(1500)
    expect(stats.altitudeGainM).toBe(500)
    expect(stats.takeoffLat).toBe(45.8637)
    expect(stats.takeoffLon).toBe(6.2911)
    expect(stats.distanceKm).toBeGreaterThan(0)
  })

  it('computes the same stats from the equivalent IGC flight', () => {
    const track = parseIgc(sampleIgc)
    const stats = computeTrackStats(track)

    expect(stats.durationSeconds).toBe(27 * 60)
    expect(stats.maxAltitudeM).toBe(1500)
    expect(stats.altitudeGainM).toBe(500)
  })

  it('never counts descent toward the gain', () => {
    const track = parseGpx(sampleGpx)
    const stats = computeTrackStats(track)

    // Five climbing legs of 100m each; if descent were counted the gain would be inflated.
    expect(stats.altitudeGainM).toBeLessThan(1000)
  })

  it('throws for an empty track', () => {
    expect(() => computeTrackStats({ points: [] })).toThrow(/empty/i)
  })

  it('reports null altitude stats when no fix has an altitude', () => {
    const track = {
      points: [
        { lat: 45.86, lon: 6.29, time: '2026-01-01T09:00:00.000Z', gpsElevation: null, baroElevation: null },
        { lat: 45.87, lon: 6.30, time: '2026-01-01T09:01:00.000Z', gpsElevation: null, baroElevation: null },
      ],
    }
    const stats = computeTrackStats(track)

    expect(stats.maxAltitudeM).toBeNull()
    expect(stats.altitudeGainM).toBeNull()
  })

  it('reports null duration when no fix has a timestamp, but still computes distance', () => {
    const track = {
      points: [
        { lat: 45.86, lon: 6.29, time: null, gpsElevation: 1000, baroElevation: 1000 },
        { lat: 45.87, lon: 6.3, time: null, gpsElevation: 1100, baroElevation: 1100 },
      ],
    }
    const stats = computeTrackStats(track)

    expect(stats.startedAt).toBeNull()
    expect(stats.endedAt).toBeNull()
    expect(stats.durationSeconds).toBeNull()
    expect(stats.maxAltitudeM).toBe(1100)
    expect(stats.distanceKm).toBeGreaterThan(0)
  })
})
