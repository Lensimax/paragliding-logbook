import { act, renderHook } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { useSharedCursor } from './useSharedCursor'

describe('useSharedCursor', () => {
  it('starts at null', () => {
    const { result } = renderHook(() => useSharedCursor())
    expect(result.current.timeOffsetSeconds).toBeNull()
  })

  it('shares state between independent hook instances (map <-> profile)', () => {
    const a = renderHook(() => useSharedCursor())
    const b = renderHook(() => useSharedCursor())

    act(() => a.result.current.setTimeOffsetSeconds(42))

    expect(a.result.current.timeOffsetSeconds).toBe(42)
    expect(b.result.current.timeOffsetSeconds).toBe(42)
  })

  it('clears back to null', () => {
    const { result } = renderHook(() => useSharedCursor())

    act(() => result.current.setTimeOffsetSeconds(10))
    expect(result.current.timeOffsetSeconds).toBe(10)

    act(() => result.current.setTimeOffsetSeconds(null))
    expect(result.current.timeOffsetSeconds).toBeNull()
  })

  it('setting the same value again is a no-op (breaks the setCursor feedback loop)', () => {
    const { result } = renderHook(() => useSharedCursor())

    act(() => result.current.setTimeOffsetSeconds(5))
    const setterBeforeNoop = result.current.setTimeOffsetSeconds
    act(() => result.current.setTimeOffsetSeconds(5))

    // The setter identity is stable (useCallback with no deps); re-render didn't churn state.
    expect(result.current.setTimeOffsetSeconds).toBe(setterBeforeNoop)
    expect(result.current.timeOffsetSeconds).toBe(5)
  })
})
