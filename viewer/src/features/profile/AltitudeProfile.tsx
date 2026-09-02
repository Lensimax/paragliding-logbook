import { useEffect, useRef } from 'react'
import uPlot from 'uplot'
import 'uplot/dist/uPlot.min.css'
import { useSharedCursor } from '../../hooks/useSharedCursor'
import { formatDateTime } from '../../lib/format/datetime'
import { buildProfileOptions } from './profileOptions'
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

  const startMs = points.length > 0 ? new Date(points[0].time!).getTime() : 0
  const timeOffsets = points.map((p) => (new Date(p.time!).getTime() - startMs) / 1000)
  const altitudes = points.map((p) => p.baroElevation ?? NaN)

  useEffect(() => {
    const container = containerRef.current
    if (!container) return

    const data: uPlot.AlignedData = groundElevationM
      ? [timeOffsets, altitudes, groundElevationM.map((v) => v ?? NaN)]
      : [timeOffsets, altitudes]

    const options = buildProfileOptions(container.clientWidth, PROFILE_HEIGHT, groundElevationM !== null)
    options.hooks = {
      setCursor: [
        (u) => {
          const idx = u.cursor.idx
          setTimeOffsetSeconds(idx == null ? null : (u.data[0][idx] as number))
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
  }, [points, groundElevationM])

  // Reflect an externally-set cursor (e.g. hovering the map) onto this chart's own crosshair.
  useEffect(() => {
    const plot = plotRef.current
    if (!plot || timeOffsetSeconds === null) return
    const left = plot.valToPos(timeOffsetSeconds, 'x')
    plot.setCursor({ left, top: -1 })
  }, [timeOffsetSeconds])

  const cursorIndex = nearestIndex(timeOffsets, timeOffsetSeconds)

  return (
    <div className="altitude-profile">
      <div ref={containerRef} className="altitude-profile-chart" />
      {cursorIndex !== null && (
        <div className="altitude-profile-readout">
          <span>{formatReadoutOffset(timeOffsets[cursorIndex])}</span>
          <span>{formatDateTime(points[cursorIndex].time!)}</span>
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
