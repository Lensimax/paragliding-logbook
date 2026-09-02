import type { ParsedTrack, TrackPoint } from './types'

/** Pure parser: GPX text in, ParsedTrack out. No React, no fetch. */
export function parseGpx(xmlText: string): ParsedTrack {
  const doc = new DOMParser().parseFromString(xmlText, 'application/xml')

  if (doc.getElementsByTagName('parsererror').length > 0) {
    throw new Error('Could not parse GPX file: invalid XML.')
  }

  const trkpts = Array.from(doc.getElementsByTagName('trkpt'))
  if (trkpts.length === 0) {
    throw new Error('GPX file has no track points.')
  }

  const points: TrackPoint[] = trkpts.map((node) => {
    const lat = Number(node.getAttribute('lat'))
    const lon = Number(node.getAttribute('lon'))
    const timeText = node.getElementsByTagName('time')[0]?.textContent

    if (!Number.isFinite(lat) || !Number.isFinite(lon)) {
      throw new Error('GPX track point is missing latitude or longitude.')
    }
    if (!timeText) {
      throw new Error('GPX track point is missing a timestamp.')
    }

    const eleText = node.getElementsByTagName('ele')[0]?.textContent
    const elevation = eleText ? Number(eleText) : null

    return {
      lat,
      lon,
      time: new Date(timeText).toISOString(),
      gpsElevation: elevation,
      baroElevation: elevation,
    }
  })

  return { points }
}
