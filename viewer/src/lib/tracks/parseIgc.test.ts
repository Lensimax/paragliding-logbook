import { describe, expect, it } from 'vitest'
import sampleIgc from './fixtures/sample.igc?raw'
import { parseIgc } from './parseIgc'

describe('parseIgc', () => {
  it('parses every valid B-record fix from a real IGC file', () => {
    const track = parseIgc(sampleIgc)

    expect(track.points).toHaveLength(10)
  })

  it('reads lat/lon/altitudes/time for the first fix, combining the HFDTE date with the B-record time', () => {
    const track = parseIgc(sampleIgc)
    const first = track.points[0]

    expect(first.lat).toBeCloseTo(45.8637, 4)
    expect(first.lon).toBeCloseTo(6.2911, 4)
    expect(first.baroElevation).toBe(1000)
    expect(first.gpsElevation).toBe(1000)
    expect(first.time).toBe('2026-03-05T09:00:00.000Z')
  })

  it('reads the last fix', () => {
    const track = parseIgc(sampleIgc)
    const last = track.points[track.points.length - 1]

    expect(last.baroElevation).toBe(700)
    expect(last.time).toBe('2026-03-05T09:27:00.000Z')
  })

  it('skips B-records without a valid 3D fix', () => {
    const withInvalidFix =
      'HFDTE050326\n' +
      'B0900004551822N00617466EV0100001000\n' + // V = not a valid fix
      'B0903004551900N00617550EA0110001100\n'
    const track = parseIgc(withInvalidFix)

    expect(track.points).toHaveLength(1)
    expect(track.points[0].baroElevation).toBe(1100)
  })

  it('rolls the date forward when the time of day wraps past midnight', () => {
    const crossingMidnight =
      'HFDTE050326\n' +
      'B2359004551822N00617466EA0100001000\n' +
      'B0001004551900N00617550EA0110001100\n'
    const track = parseIgc(crossingMidnight)

    expect(track.points[0].time).toBe('2026-03-05T23:59:00.000Z')
    expect(track.points[1].time).toBe('2026-03-06T00:01:00.000Z')
  })

  it('rejects a file with no valid B-record fixes', () => {
    expect(() => parseIgc('HFDTE050326\nHFPLTPILOTINCHARGE:Nobody\n')).toThrow(/no valid/i)
  })
})
