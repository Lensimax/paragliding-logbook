import '@testing-library/jest-dom/vitest'

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

