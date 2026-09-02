import { render } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { FlightMap } from './FlightMap'
import type { TrackPoint } from '../../lib/tracks/types'

const points: TrackPoint[] = [
  { lat: 45.8637, lon: 6.2911, time: '2026-03-05T09:00:00.000Z', gpsElevation: 1000, baroElevation: 1000 },
  { lat: 45.865, lon: 6.2925, time: '2026-03-05T09:03:00.000Z', gpsElevation: 1100, baroElevation: 1100 },
  { lat: 45.8663, lon: 6.2939, time: '2026-03-05T09:06:00.000Z', gpsElevation: 1200, baroElevation: 1200 },
]

describe('FlightMap', () => {
  it('mounts a Leaflet map with a track polyline without crashing', async () => {
    const { container, unmount } = render(<FlightMap points={points} />)

    expect(container.querySelector('.leaflet-container')).not.toBeNull()

    // Let Leaflet's rAF-scheduled canvas redraw complete before tearing down the DOM,
    // otherwise it fires against an already-removed canvas and throws.
    await new Promise((resolve) => requestAnimationFrame(resolve))
    unmount()
  })

  it('mounts with an empty track (no crash before the track loads)', async () => {
    const { container, unmount } = render(<FlightMap points={[]} />)

    expect(container.querySelector('.leaflet-container')).not.toBeNull()

    // Let Leaflet's rAF-scheduled canvas redraw complete before tearing down the DOM,
    // otherwise it fires against an already-removed canvas and throws.
    await new Promise((resolve) => requestAnimationFrame(resolve))
    unmount()
  })
})
