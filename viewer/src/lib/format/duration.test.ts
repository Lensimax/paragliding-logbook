import { describe, expect, it } from 'vitest'
import { formatDuration } from './duration'

describe('formatDuration', () => {
  it('renders an em dash for missing durations', () => {
    expect(formatDuration(null)).toBe('—')
    expect(formatDuration(undefined)).toBe('—')
  })

  it('renders minutes only under an hour', () => {
    expect(formatDuration(25 * 60)).toBe('25m')
  })

  it('renders whole hours without minutes', () => {
    expect(formatDuration(2 * 3600)).toBe('2h')
  })

  it('renders hours and minutes', () => {
    expect(formatDuration(3600 + 45 * 60)).toBe('1h 45m')
  })
})
