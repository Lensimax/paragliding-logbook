import { describe, expect, it } from 'vitest'
import sampleGpx from './fixtures/sample.gpx?raw'
import { parseGpx } from './parseGpx'

describe('parseGpx', () => {
  it('parses every track point from a real GPX file', () => {
    const track = parseGpx(sampleGpx)

    expect(track.points).toHaveLength(10)
  })

  it('reads lat/lon/elevation/time for the first point', () => {
    const track = parseGpx(sampleGpx)
    const first = track.points[0]

    expect(first.lat).toBe(45.8637)
    expect(first.lon).toBe(6.2911)
    expect(first.gpsElevation).toBe(1000)
    expect(first.baroElevation).toBe(1000)
    expect(first.time).toBe('2026-03-05T09:00:00.000Z')
  })

  it('reads the last point', () => {
    const track = parseGpx(sampleGpx)
    const last = track.points[track.points.length - 1]

    expect(last.lat).toBe(45.85)
    expect(last.gpsElevation).toBe(700)
    expect(last.time).toBe('2026-03-05T09:27:00.000Z')
  })

  it('treats GPX elevation as both the GPS and pressure channel', () => {
    const track = parseGpx(sampleGpx)

    for (const point of track.points) {
      expect(point.baroElevation).toBe(point.gpsElevation)
    }
  })

  it('rejects invalid XML', () => {
    expect(() => parseGpx('<gpx><trk><trkseg>')).toThrow(/parse/i)
  })

  it('rejects a GPX file with no track or route points', () => {
    const empty = '<?xml version="1.0"?><gpx version="1.1"><trk><trkseg></trkseg></trk></gpx>'
    expect(() => parseGpx(empty)).toThrow(/no track or route points/i)
  })

  it('treats a missing timestamp as a null time rather than a parse error', () => {
    const noTime =
      '<?xml version="1.0"?><gpx version="1.1"><trk><trkseg>' +
      '<trkpt lat="45.86" lon="6.29"><ele>1000</ele></trkpt>' +
      '</trkseg></trk></gpx>'
    const track = parseGpx(noTime)

    expect(track.points[0].time).toBeNull()
    expect(track.points[0].gpsElevation).toBe(1000)
  })

  it('falls back to route points when there is no track segment', () => {
    const routeOnly =
      '<?xml version="1.0"?><gpx version="1.1"><rte>' +
      '<rtept lat="45.094578" lon="5.579108"><ele>1035.2</ele></rtept>' +
      '<rtept lat="45.09614" lon="5.58381"><ele>1080.0</ele></rtept>' +
      '</rte></gpx>'
    const track = parseGpx(routeOnly)

    expect(track.points).toHaveLength(2)
    expect(track.points[0].lat).toBe(45.094578)
    expect(track.points[0].time).toBeNull()
  })
})
