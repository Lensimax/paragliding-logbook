import { render } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { AltitudeProfile } from './AltitudeProfile'
import type { TrackPoint } from '../../lib/tracks/types'

const points: TrackPoint[] = [
  { lat: 45.8637, lon: 6.2911, time: '2026-03-05T09:00:00.000Z', gpsElevation: 1000, baroElevation: 1000 },
  { lat: 45.865, lon: 6.2925, time: '2026-03-05T09:03:00.000Z', gpsElevation: 1100, baroElevation: 1100 },
  { lat: 45.8663, lon: 6.2939, time: '2026-03-05T09:06:00.000Z', gpsElevation: 1200, baroElevation: 1200 },
]

describe('AltitudeProfile', () => {
  it('mounts a uPlot chart without a ground series', () => {
    const { container, unmount } = render(<AltitudeProfile points={points} groundElevationM={null} />)

    expect(container.querySelector('canvas')).not.toBeNull()

    unmount()
  })

  it('mounts with a ground elevation series', () => {
    const { container, unmount } = render(<AltitudeProfile points={points} groundElevationM={[990, 1010, 1050]} />)

    expect(container.querySelector('canvas')).not.toBeNull()

    unmount()
  })

  it('shows no readout before any cursor position is set', () => {
    const { container, unmount } = render(<AltitudeProfile points={points} groundElevationM={null} />)

    expect(container.querySelector('.altitude-profile-readout')).toBeNull()

    unmount()
  })
})
