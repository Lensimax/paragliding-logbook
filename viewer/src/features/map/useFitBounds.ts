import { useEffect } from 'react'
import type L from 'leaflet'

export function useFitBounds(map: L.Map | null, bounds: L.LatLngBounds | null) {
  useEffect(() => {
    if (!map || !bounds || !bounds.isValid()) return
    map.fitBounds(bounds, { padding: [24, 24] })
  }, [map, bounds])
}
