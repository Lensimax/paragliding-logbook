import { describe, expect, it } from 'vitest'
import { movingAverage } from './smoothing'

describe('movingAverage', () => {
  it('returns the input unchanged for a window of 1 or less', () => {
    expect(movingAverage([1, 5, 2], 1)).toEqual([1, 5, 2])
    expect(movingAverage([1, 5, 2], 0)).toEqual([1, 5, 2])
  })

  it('averages a centered window, shrinking it near the edges', () => {
    // window 3: index 0 only has itself and its one right neighbor to average.
    expect(movingAverage([0, 3, 6, 9, 12], 3)).toEqual([1.5, 3, 6, 9, 10.5])
  })

  it('excludes NaN entries from the average instead of propagating them', () => {
    expect(movingAverage([10, NaN, 20], 3)).toEqual([10, 15, 20])
  })

  it('returns NaN only when every value in the window is NaN', () => {
    expect(movingAverage([NaN, NaN, NaN], 3)).toEqual([NaN, NaN, NaN])
  })
})
