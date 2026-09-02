import type { ParsedTrack, TrackPoint } from './types'

interface IgcDate {
  year: number
  month: number
  day: number
}

function parseHeaderDate(line: string): IgcDate | null {
  const match = /^HFDTE(?:DATE:)?(\d{2})(\d{2})(\d{2})/.exec(line)
  if (!match) return null
  const [, dd, mm, yy] = match
  return { day: Number(dd), month: Number(mm), year: 2000 + Number(yy) }
}

/** DDMMmmm + hemisphere -> decimal degrees. */
function parseLatitude(raw: string, hemisphere: string): number {
  const degrees = Number(raw.slice(0, 2))
  const minutes = Number(raw.slice(2, 7)) / 1000
  const value = degrees + minutes / 60
  return hemisphere === 'S' ? -value : value
}

/** DDDMMmmm + hemisphere -> decimal degrees. */
function parseLongitude(raw: string, hemisphere: string): number {
  const degrees = Number(raw.slice(0, 3))
  const minutes = Number(raw.slice(3, 8)) / 1000
  const value = degrees + minutes / 60
  return hemisphere === 'W' ? -value : value
}

/**
 * Pure parser: IGC text in, ParsedTrack out. B-record layout (fixed width, 1-indexed in the spec):
 * B HHMMSS DDMMmmmN DDDMMmmmE V PPPPP GGGGG ...extensions
 */
export function parseIgc(text: string): ParsedTrack {
  const lines = text.split(/\r?\n/)

  let date: IgcDate = { year: 1970, month: 1, day: 1 }
  for (const line of lines) {
    if (line.startsWith('HFDTE')) {
      const parsed = parseHeaderDate(line)
      if (parsed) date = parsed
      break
    }
  }

  const points: TrackPoint[] = []
  let previousSecondsOfDay = -1
  let dayOffset = 0

  for (const line of lines) {
    if (!line.startsWith('B') || line.length < 35) continue

    const validity = line[24]
    if (validity !== 'A') continue

    const hh = Number(line.slice(1, 3))
    const mm = Number(line.slice(3, 5))
    const ss = Number(line.slice(5, 7))
    const secondsOfDay = hh * 3600 + mm * 60 + ss

    if (secondsOfDay < previousSecondsOfDay) dayOffset += 1
    previousSecondsOfDay = secondsOfDay

    const lat = parseLatitude(line.slice(7, 14), line[14])
    const lon = parseLongitude(line.slice(15, 23), line[23])
    const pressureAlt = Number(line.slice(25, 30))
    const gpsAlt = Number(line.slice(30, 35))

    const time = new Date(Date.UTC(date.year, date.month - 1, date.day + dayOffset, hh, mm, ss))

    points.push({
      lat,
      lon,
      time: time.toISOString(),
      gpsElevation: Number.isFinite(gpsAlt) ? gpsAlt : null,
      baroElevation: Number.isFinite(pressureAlt) ? pressureAlt : null,
    })
  }

  if (points.length === 0) {
    throw new Error('IGC file has no valid B-record fixes.')
  }

  return { points }
}
