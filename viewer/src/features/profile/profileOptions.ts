import type uPlot from 'uplot'

const clockTimeFormatter = new Intl.DateTimeFormat(undefined, { timeStyle: 'medium' })

export function formatOffset(seconds: number): string {
  const sign = seconds < 0 ? '-' : ''
  const abs = Math.abs(seconds)
  const minutes = Math.floor(abs / 60)
  const secs = Math.floor(abs % 60)
  return `${sign}${minutes}:${secs.toString().padStart(2, '0')}`
}

/** Formats a Unix-epoch-ms instant as a local clock time, e.g. "14:32:05". */
export function formatClockTime(ms: number): string {
  return clockTimeFormatter.format(new Date(ms))
}

export function formatDistanceKm(km: number): string {
  return `${km.toFixed(1)} km`
}

interface XAxis {
  label: string
  format: (value: number) => string
}

export interface ProfileColors {
  text: string
  grid: string
}

export const ALTITUDE_COLOR = '#e2431e'
export const GROUND_COLOR = '#8a6d3b'

export function buildProfileOptions(
  width: number,
  height: number,
  hasGround: boolean,
  xAxis: XAxis,
  colors: ProfileColors,
): uPlot.Options {
  const series: uPlot.Series[] = [
    {},
    { label: 'Altitude', stroke: ALTITUDE_COLOR, width: 2, fill: 'rgba(226, 67, 30, 0.15)' },
  ]
  if (hasGround) {
    series.push({ label: 'Ground', stroke: GROUND_COLOR, width: 1, fill: 'rgba(138, 109, 59, 0.25)' })
  }

  const axisStyle = {
    stroke: colors.text,
    grid: { stroke: colors.grid },
    ticks: { stroke: colors.grid },
  }

  return {
    width,
    height,
    scales: { x: { time: false } },
    axes: [
      { label: xAxis.label, values: (_u, values) => values.map(xAxis.format), ...axisStyle },
      { label: 'Altitude (m)', ...axisStyle },
    ],
    series,
    legend: { show: false },
    // Drag-to-pan and wheel-to-zoom are wired up manually (see AltitudeProfile's bindZoomPan) -
    // disable uPlot's built-in drag-to-select-and-zoom so the two don't fight over the mouse.
    cursor: { drag: { x: false, y: false, setScale: false } },
  }
}
