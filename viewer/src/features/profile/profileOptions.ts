import type uPlot from 'uplot'

export function formatOffset(seconds: number): string {
  const sign = seconds < 0 ? '-' : ''
  const abs = Math.abs(seconds)
  const minutes = Math.floor(abs / 60)
  const secs = Math.floor(abs % 60)
  return `${sign}${minutes}:${secs.toString().padStart(2, '0')}`
}

export function formatDistanceKm(km: number): string {
  return `${km.toFixed(1)} km`
}

interface XAxis {
  label: string
  format: (value: number) => string
}

export function buildProfileOptions(width: number, height: number, hasGround: boolean, xAxis: XAxis): uPlot.Options {
  const series: uPlot.Series[] = [
    {},
    { label: 'Altitude', stroke: '#e2431e', width: 2, fill: 'rgba(226, 67, 30, 0.15)' },
  ]
  if (hasGround) {
    series.push({ label: 'Ground', stroke: '#8a6d3b', width: 1, fill: 'rgba(138, 109, 59, 0.25)' })
  }

  return {
    width,
    height,
    scales: { x: { time: false } },
    axes: [
      { label: xAxis.label, values: (_u, values) => values.map(xAxis.format) },
      { label: 'Altitude (m)' },
    ],
    series,
    legend: { show: false },
    cursor: { points: { show: false } },
  }
}
