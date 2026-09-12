import { useEffect, useRef, useState } from 'react'
import uPlot from 'uplot'
import 'uplot/dist/uPlot.min.css'
import { useSharedCursor } from '../../hooks/useSharedCursor'
import { formatDateTime } from '../../lib/format/datetime'
import { movingAverage } from '../../lib/tracks/smoothing'
import { cumulativeDistanceKm } from '../../lib/tracks/stats'
import { ALTITUDE_COLOR, GROUND_COLOR, buildProfileOptions, formatClockTime, formatDistanceKm } from './profileOptions'
import type { TrackPoint } from '../../lib/tracks/types'
import './profile.css'

const ALTITUDE_SMOOTHING_WINDOW = 5

interface AltitudeProfileProps {
  points: TrackPoint[]
  groundElevationM: (number | null)[] | null
  height: number
}

export function AltitudeProfile({ points, groundElevationM, height }: AltitudeProfileProps) {
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
  // Raw GPS/baro altitude is jittery point-to-point; smooth it for display only - stats.ts
  // computes max altitude and altitude gain from the unsmoothed track.
  const altitudes = movingAverage(points.map((p) => p.baroElevation ?? NaN), ALTITUDE_SMOOTHING_WINDOW)

  useEffect(() => {
    const container = containerRef.current
    if (!container) return

    const data: uPlot.AlignedData = groundElevationM
      ? [xValues, altitudes, groundElevationM.map((v) => v ?? NaN)]
      : [xValues, altitudes]

    const xAxis = hasTime
      ? { label: 'Time', format: (seconds: number) => formatClockTime(startMs + seconds * 1000) }
      : { label: 'Distance (km)', format: formatDistanceKm }

    function buildPlot() {
      const style = getComputedStyle(document.documentElement)
      const colors = { text: style.getPropertyValue('--text').trim(), grid: style.getPropertyValue('--border').trim() }
      const options = buildProfileOptions(container!.clientWidth, container!.clientHeight, groundElevationM !== null, xAxis, colors)
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
      const built = new uPlot(options, data, container!)
      bindZoomPan(built, xValues[0], xValues[xValues.length - 1])
      return built
    }

    let plot = buildPlot()
    plotRef.current = plot

    const resizeObserver = new ResizeObserver(() => {
      plot.setSize({ width: container.clientWidth, height: container.clientHeight })
    })
    resizeObserver.observe(container)

    // uPlot draws axis text onto a canvas, so it doesn't pick up CSS variable changes on its
    // own - rebuild with the new theme's colors when the OS/browser color scheme flips.
    const darkMediaQuery = window.matchMedia('(prefers-color-scheme: dark)')
    const handleThemeChange = () => {
      plot.destroy()
      plot = buildPlot()
      plotRef.current = plot
    }
    darkMediaQuery.addEventListener('change', handleThemeChange)

    return () => {
      darkMediaQuery.removeEventListener('change', handleThemeChange)
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
    <div className="altitude-profile" style={{ height }}>
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
          <span style={{ color: ALTITUDE_COLOR }}>
            {altitudes[cursorIndex] != null && !Number.isNaN(altitudes[cursorIndex]) ? `${Math.round(altitudes[cursorIndex])} m` : '—'}
          </span>
          {groundElevationM && (
            <span style={{ color: GROUND_COLOR }}>
              {groundElevationM[cursorIndex] != null ? `${Math.round(groundElevationM[cursorIndex]!)} m ground` : '—'}
            </span>
          )}
        </div>
      )}
    </div>
  )
}

const MIN_ZOOM_SPAN_RATIO = 0.02
const ZOOM_STEP = 0.85

/** Wires up mouse-wheel zoom (centered on the cursor) and left-click-drag pan on the x scale,
 * in place of uPlot's default drag-to-select-and-zoom (disabled via cursor.drag in profileOptions). */
function bindZoomPan(plot: uPlot, dataMin: number, dataMax: number) {
  const over = plot.over
  const fullRange = dataMax - dataMin
  if (!(fullRange > 0)) return

  function clampRange(min: number, max: number): [number, number] {
    const span = Math.min(max - min, fullRange)
    let clampedMin = min
    let clampedMax = min + span
    if (clampedMin < dataMin) {
      clampedMin = dataMin
      clampedMax = dataMin + span
    }
    if (clampedMax > dataMax) {
      clampedMax = dataMax
      clampedMin = dataMax - span
    }
    return [clampedMin, clampedMax]
  }

  function handleWheel(event: WheelEvent) {
    event.preventDefault()
    const rect = over.getBoundingClientRect()
    const cursorVal = plot.posToVal(event.clientX - rect.left, 'x')
    const scale = plot.scales.x
    const min = scale.min ?? dataMin
    const max = scale.max ?? dataMax
    const range = max - min
    const factor = event.deltaY < 0 ? ZOOM_STEP : 1 / ZOOM_STEP
    const newRange = Math.min(fullRange, Math.max(fullRange * MIN_ZOOM_SPAN_RATIO, range * factor))
    const ratio = (cursorVal - min) / range
    const newMin = cursorVal - ratio * newRange
    const [clampedMin, clampedMax] = clampRange(newMin, newMin + newRange)
    plot.setScale('x', { min: clampedMin, max: clampedMax })
  }

  function handlePointerDown(event: PointerEvent) {
    if (event.button !== 0) return
    event.preventDefault()
    const rect = over.getBoundingClientRect()
    const scale = plot.scales.x
    const start = { x: event.clientX, min: scale.min ?? dataMin, max: scale.max ?? dataMax }

    function handlePointerMove(moveEvent: PointerEvent) {
      const deltaPx = moveEvent.clientX - start.x
      const deltaVal = (deltaPx / rect.width) * (start.max - start.min)
      const [clampedMin, clampedMax] = clampRange(start.min - deltaVal, start.max - deltaVal)
      plot.setScale('x', { min: clampedMin, max: clampedMax })
    }

    function handlePointerUp() {
      document.removeEventListener('pointermove', handlePointerMove)
      document.removeEventListener('pointerup', handlePointerUp)
    }

    document.addEventListener('pointermove', handlePointerMove)
    document.addEventListener('pointerup', handlePointerUp)
  }

  over.addEventListener('wheel', handleWheel, { passive: false })
  over.addEventListener('pointerdown', handlePointerDown)
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
