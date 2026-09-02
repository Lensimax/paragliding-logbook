import '@testing-library/jest-dom/vitest'
import { afterEach } from 'vitest'
import { queryClient } from '../app/queryClient'

// The QueryClient is a module-level singleton shared by every test in a file. Without
// clearing it, cached data from an earlier test (well within staleTime) leaks into the next.
afterEach(() => {
  queryClient.clear()
})

// jsdom does not implement matchMedia. Provide a minimal polyfill that evaluates
// "(max-width: Npx)" against the current window.innerWidth, which is all useBreakpoint needs.
if (!window.matchMedia) {
  window.matchMedia = (query: string) => {
    const match = /max-width:\s*(\d+)px/.exec(query)
    const maxWidth = match ? Number(match[1]) : Infinity

    const mql = {
      get matches() {
        return window.innerWidth <= maxWidth
      },
      media: query,
      onchange: null,
      addEventListener: () => {},
      removeEventListener: () => {},
      addListener: () => {},
      removeListener: () => {},
      dispatchEvent: () => false,
    } as unknown as MediaQueryList

    return mql
  }
}

// jsdom's getContext('2d') logs "Not implemented" and returns null unless the native `canvas`
// package is installed. Leaflet's preferCanvas renderer needs a working 2D context; stub one out
// with no-op methods so map rendering doesn't crash under jsdom (nothing here is ever asserted
// on - the FlightMap tests only check that Leaflet initializes without throwing).
function createNoopCanvasContext(): CanvasRenderingContext2D {
  const ctx: Record<string, unknown> = {
    fillStyle: '',
    strokeStyle: '',
    lineWidth: 1,
    lineCap: 'butt',
    lineJoin: 'miter',
    globalAlpha: 1,
    font: '',
  }
  const methods = [
    'translate', 'scale', 'rotate', 'save', 'restore', 'clearRect', 'fillRect', 'strokeRect',
    'beginPath', 'closePath', 'moveTo', 'lineTo', 'arc', 'arcTo', 'bezierCurveTo', 'quadraticCurveTo',
    'rect', 'fill', 'stroke', 'clip', 'setLineDash', 'getLineDash', 'setTransform', 'resetTransform',
    'drawImage', 'fillText', 'strokeText', 'createLinearGradient', 'createRadialGradient',
    'createPattern', 'getImageData', 'putImageData', 'isPointInPath', 'measureText',
  ]
  for (const name of methods) {
    ctx[name] = () =>
      name === 'measureText' ? { width: 0 } : name === 'isPointInPath' ? false : undefined
  }
  return ctx as unknown as CanvasRenderingContext2D
}

Object.defineProperty(HTMLCanvasElement.prototype, 'getContext', {
  value: () => createNoopCanvasContext(),
  writable: true,
  configurable: true,
})

