import { describe, expect, it } from 'vitest'
import { fromDatetimeLocalValue, localDateOf, toDatetimeLocalValue } from './datetime'

describe('datetime', () => {
  it('round-trips an ISO datetime through the local value conversion at minute precision', () => {
    const iso = new Date('2026-03-05T14:30:00.000Z').toISOString()
    const local = toDatetimeLocalValue(iso)
    const roundTripped = fromDatetimeLocalValue(local)

    expect(new Date(roundTripped).getTime()).toBe(new Date(iso).getTime())
  })

  it('extracts the calendar date portion of a datetime-local value', () => {
    expect(localDateOf('2026-03-05T14:30')).toBe('2026-03-05')
  })
})
