import { act, renderHook, waitFor } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { usePanelStack } from './usePanelStack'
import type { PanelView } from './types'

const root: PanelView = { key: 'root', title: 'Root', render: () => null }
const detail: PanelView = { key: 'detail', title: 'Detail', render: () => null }

describe('usePanelStack', () => {
  it('starts at the root view with depth 1', () => {
    const { result } = renderHook(() => usePanelStack(root))
    expect(result.current.current).toBe(root)
    expect(result.current.depth).toBe(1)
  })

  it('push adds a view and increases depth', () => {
    const { result } = renderHook(() => usePanelStack(root))

    act(() => result.current.push(detail))

    expect(result.current.current).toBe(detail)
    expect(result.current.depth).toBe(2)
  })

  it('pop navigates back to the previous view via browser history', async () => {
    const { result } = renderHook(() => usePanelStack(root))

    act(() => result.current.push(detail))
    expect(result.current.depth).toBe(2)

    act(() => result.current.pop())
    // history.back() dispatches popstate asynchronously even in jsdom.
    await waitFor(() => expect(result.current.depth).toBe(1))

    expect(result.current.current).toBe(root)
  })
})
