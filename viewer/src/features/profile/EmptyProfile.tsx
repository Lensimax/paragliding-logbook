import './profile.css'

interface EmptyProfileProps {
  message: string
}

/**
 * Same footprint as AltitudeProfile so selecting an activity without a track doesn't reflow
 * the map above it.
 */
export function EmptyProfile({ message }: EmptyProfileProps) {
  return (
    <div className="altitude-profile altitude-profile-empty">
      <p>{message}</p>
    </div>
  )
}
