import { useEffect, useRef, useState } from 'react'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import { createBaseLayers } from './baseLayers'
import { createTrackLayer, trackBounds } from './TrackLayer'
import { useFitBounds } from './useFitBounds'
import type { TrackPoint } from '../../lib/tracks/types'
import './map.css'

interface FlightMapProps {
  points: TrackPoint[]
}

const DEFAULT_CENTER: L.LatLngTuple = [46.8, 8.2] // roughly the Alps, before any track has loaded
const DEFAULT_ZOOM = 7

export function FlightMap({ points }: FlightMapProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const [map, setMap] = useState<L.Map | null>(null)
  const trackLayerRef = useRef<L.Polyline | null>(null)

  useEffect(() => {
    if (!containerRef.current) return

    // preferCanvas: an SVG polyline of 10,000+ points is unusably slow; canvas is not.
    const instance = L.map(containerRef.current, { preferCanvas: true }).setView(DEFAULT_CENTER, DEFAULT_ZOOM)

    const baseLayers = createBaseLayers()
    baseLayers[0].layer.addTo(instance)
    L.control.layers(Object.fromEntries(baseLayers.map((b) => [b.name, b.layer]))).addTo(instance)

    setMap(instance)

    return () => {
      instance.remove()
      setMap(null)
    }
  }, [])

  useEffect(() => {
    if (!map) return

    trackLayerRef.current?.remove()
    trackLayerRef.current = null

    if (points.length > 0) {
      const layer = createTrackLayer(points)
      layer.addTo(map)
      trackLayerRef.current = layer
    }
  }, [map, points])

  useFitBounds(map, points.length > 0 ? trackBounds(points) : null)

  return <div ref={containerRef} className="flight-map" />
}
