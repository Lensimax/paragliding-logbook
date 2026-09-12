import './profile.css'

interface EmptyProfileProps {
  message: string
  height: number
}

/**
 * Same footprint as AltitudeProfile so selecting an activity without a track doesn't reflow
 * the map above it.
 */
export function EmptyProfile({ message, height }: EmptyProfileProps) {
  return (
    <div className="altitude-profile altitude-profile-empty" style={{ height }}>
      <p>{message}</p>
    </div>
  )
}
