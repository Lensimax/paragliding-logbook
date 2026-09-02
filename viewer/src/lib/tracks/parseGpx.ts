import type { ParsedTrack, TrackPoint } from './types'

/**
 * Pure parser: GPX text in, ParsedTrack out. No React, no fetch.
 *
 * Prefers `<trk><trkseg><trkpt>` (the usual export from flight instruments and most apps), and
 * falls back to `<rte><rtept>` when there is no track segment — some apps (observed: Suunto)
 * export a route instead. Timestamps are optional in both: real-world exports sometimes carry
 * only position and elevation, so a missing `<time>` is not a parse error, just a point with
 * `time: null` that stats.ts and the auto-fill will skip.
 */
export function parseGpx(xmlText: string): ParsedTrack {
  const doc = new DOMParser().parseFromString(xmlText, 'application/xml')

  if (doc.getElementsByTagName('parsererror').length > 0) {
    throw new Error('Could not parse GPX file: invalid XML.')
  }

  let pointNodes = Array.from(doc.getElementsByTagName('trkpt'))
  if (pointNodes.length === 0) {
    pointNodes = Array.from(doc.getElementsByTagName('rtept'))
  }
  if (pointNodes.length === 0) {
    throw new Error('GPX file has no track or route points.')
  }

  const points: TrackPoint[] = pointNodes.map((node) => {
    const lat = Number(node.getAttribute('lat'))
    const lon = Number(node.getAttribute('lon'))

    if (!Number.isFinite(lat) || !Number.isFinite(lon)) {
      throw new Error('GPX point is missing latitude or longitude.')
    }

    const timeText = node.getElementsByTagName('time')[0]?.textContent
    const eleText = node.getElementsByTagName('ele')[0]?.textContent
    const elevation = eleText ? Number(eleText) : null

    return {
      lat,
      lon,
      time: timeText ? new Date(timeText).toISOString() : null,
      gpsElevation: elevation,
      baroElevation: elevation,
    }
  })

  return { points }
}
