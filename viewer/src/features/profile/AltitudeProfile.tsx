import { useEffect, useRef, useState } from 'react'
import uPlot from 'uplot'
import 'uplot/dist/uPlot.min.css'
import { useSharedCursor } from '../../hooks/useSharedCursor'
import { formatDateTime } from '../../lib/format/datetime'
import { cumulativeDistanceKm } from '../../lib/tracks/stats'
import { buildProfileOptions, formatDistanceKm, formatOffset } from './profileOptions'
import type { TrackPoint } from '../../lib/tracks/types'
import './profile.css'

interface AltitudeProfileProps {
  points: TrackPoint[]
  groundElevationM: (number | null)[] | null
}

const PROFILE_HEIGHT = 140

export function AltitudeProfile({ points, groundElevationM }: AltitudeProfileProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const plotRef = useRef<uPlot | null>(null)
  const { timeOffsetSeconds, setTimeOffsetSeconds } = useSharedCursor()
  // Some real-world GPX exports omit <time> entirely. The shared cursor is a time offset, which
  // that domain can't produce, so those tracks fall back to a distance-based x-axis with a
  // purely local cursor (no sync with the map's hover, which skips the same tracks for the
  // same reason - see FlightMap's timedPoints guard).
  const hasTime = points.length > 0 && points.every((p) => p.time !== null)
  const [localCursorIndex, setLocalCursorIndex] = useState<number | null>(null)

  const startMs = hasTime ? new Date(points[0].time!).getTime() : 0
  const xValues = hasTime ? points.map((p) => (new Date(p.time!).getTime() - startMs) / 1000) : cumulativeDistanceKm(points)
  const altitudes = points.map((p) => p.baroElevation ?? NaN)

  useEffect(() => {
    const container = containerRef.current
    if (!container) return

    const data: uPlot.AlignedData = groundElevationM
      ? [xValues, altitudes, groundElevationM.map((v) => v ?? NaN)]
      : [xValues, altitudes]

    const xAxis = hasTime
      ? { label: 'Time since start', format: formatOffset }
      : { label: 'Distance (km)', format: formatDistanceKm }
    const options = buildProfileOptions(container.clientWidth, PROFILE_HEIGHT, groundElevationM !== null, xAxis)
    options.hooks = {
      setCursor: [
        (u) => {
          const idx = u.cursor.idx ?? null
          if (hasTime) {
            setTimeOffsetSeconds(idx == null ? null : (u.data[0][idx] as number))
          } else {
            setLocalCursorIndex(idx)
          }
        },
      ],
    }

    const plot = new uPlot(options, data, container)
    plotRef.current = plot

    const resizeObserver = new ResizeObserver(() => {
      plot.setSize({ width: container.clientWidth, height: PROFILE_HEIGHT })
    })
    resizeObserver.observe(container)

    return () => {
      resizeObserver.disconnect()
      plot.destroy()
      plotRef.current = null
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [points, groundElevationM, hasTime])

  // Reflect an externally-set cursor (e.g. hovering the map) onto this chart's own crosshair.
  useEffect(() => {
    const plot = plotRef.current
    if (!plot || !hasTime || timeOffsetSeconds === null) return
    const left = plot.valToPos(timeOffsetSeconds, 'x')
    plot.setCursor({ left, top: -1 })
  }, [timeOffsetSeconds, hasTime])

  const cursorIndex = hasTime ? nearestIndex(xValues, timeOffsetSeconds) : localCursorIndex

  return (
    <div className="altitude-profile">
      <div ref={containerRef} className="altitude-profile-chart" />
      {cursorIndex !== null && (
        <div className="altitude-profile-readout">
          {hasTime ? (
            <>
              <span>{formatReadoutOffset(xValues[cursorIndex])}</span>
              <span>{formatDateTime(points[cursorIndex].time!)}</span>
            </>
          ) : (
            <span>{formatDistanceKm(xValues[cursorIndex])}</span>
          )}
          <span>{altitudes[cursorIndex] != null && !Number.isNaN(altitudes[cursorIndex]) ? `${Math.round(altitudes[cursorIndex])} m` : '—'}</span>
          {groundElevationM && (
            <span>
              {groundElevationM[cursorIndex] != null ? `${Math.round(groundElevationM[cursorIndex]!)} m ground` : '—'}
            </span>
          )}
        </div>
      )}
    </div>
  )
}

function nearestIndex(values: number[], target: number | null): number | null {
  if (target === null || values.length === 0) return null

  let closest = 0
  let closestDistance = Math.abs(values[0] - target)
  for (let i = 1; i < values.length; i++) {
    const distance = Math.abs(values[i] - target)
    if (distance < closestDistance) {
      closestDistance = distance
      closest = i
    }
  }
  return closest
}

function formatReadoutOffset(seconds: number): string {
  const minutes = Math.floor(seconds / 60)
  const secs = Math.floor(seconds % 60)
  return `${minutes}m ${secs.toString().padStart(2, '0')}s`
}
