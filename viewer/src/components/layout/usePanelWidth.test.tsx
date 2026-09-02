import { act, renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { usePanelWidth } from './usePanelWidth'

describe('usePanelWidth', () => {
  beforeEach(() => {
    localStorage.clear()
    Object.defineProperty(window, 'innerWidth', { value: 1000, configurable: true })
  })

  afterEach(() => {
    localStorage.clear()
  })

  it('defaults to roughly 15% of the viewport width', () => {
    Object.defineProperty(window, 'innerWidth', { value: 3000, configurable: true })
    const { result } = renderHook(() => usePanelWidth())
    expect(result.current.width).toBeCloseTo(450, 0)
  })

  it('clamps to the 320px minimum', () => {
    const { result } = renderHook(() => usePanelWidth())
    act(() => result.current.setWidth(10))
    expect(result.current.width).toBe(320)
  })

  it('clamps to the 40% maximum', () => {
    const { result } = renderHook(() => usePanelWidth())
    act(() => result.current.setWidth(10_000))
    expect(result.current.width).toBe(400)
  })

  it('persists the chosen width across hook instances', () => {
    const first = renderHook(() => usePanelWidth())
    act(() => first.result.current.setWidth(350))

    const second = renderHook(() => usePanelWidth())
    expect(second.result.current.width).toBe(350)
  })
})
