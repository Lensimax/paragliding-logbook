import type { EquipmentUsage } from '../types'

interface UsageSectionProps {
  usage: EquipmentUsage
}

export function UsageSection({ usage }: UsageSectionProps) {
  return (
    <div className="usage-section">
      <h3>Usage</h3>
      <dl>
        <dt>Hours used</dt>
        <dd>{usage.hours.toFixed(1)}</dd>
        <dt>Activities</dt>
        <dd>{usage.activityCount}</dd>
      </dl>
    </div>
  )
}
