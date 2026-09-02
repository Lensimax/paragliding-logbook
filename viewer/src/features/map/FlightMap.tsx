import { useEffect, useRef, useState } from 'react'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import { useSharedCursor } from '../../hooks/useSharedCursor'
import { createBaseLayers } from './baseLayers'
import { createCursorMarker } from './CursorMarker'
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
  const cursorMarkerRef = useRef<L.CircleMarker | null>(null)
  const { timeOffsetSeconds, setTimeOffsetSeconds } = useSharedCursor()

  // Points with a real timestamp, in order - the shared cursor is a time offset, so points
  // without one (some real-world GPX exports have none) can't participate.
  const timedPoints = points.filter((p) => p.time !== null)
  const startMs = timedPoints.length > 0 ? new Date(timedPoints[0].time!).getTime() : 0

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

      if (timedPoints.length > 0) {
        layer.on('mousemove', (event: L.LeafletMouseEvent) => {
          const index = nearestPointIndex(timedPoints, event.latlng)
          const offsetSeconds = (new Date(timedPoints[index].time!).getTime() - startMs) / 1000
          setTimeOffsetSeconds(offsetSeconds)
        })
        layer.on('mouseout', () => setTimeOffsetSeconds(null))
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [map, points])

  // Reflect the shared cursor (e.g. hovering the altitude profile) as a marker on the track.
  useEffect(() => {
    if (!map) return

    if (timeOffsetSeconds === null || timedPoints.length === 0) {
      cursorMarkerRef.current?.remove()
      cursorMarkerRef.current = null
      return
    }

    const index = nearestIndexByTimeOffset(timedPoints, startMs, timeOffsetSeconds)
    const point = timedPoints[index]

    if (!cursorMarkerRef.current) {
      cursorMarkerRef.current = createCursorMarker().addTo(map)
    }
    cursorMarkerRef.current.setLatLng([point.lat, point.lon])
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [map, timeOffsetSeconds, points])

  useFitBounds(map, points.length > 0 ? trackBounds(points) : null)

  return <div ref={containerRef} className="flight-map" />
}

function nearestPointIndex(points: TrackPoint[], latlng: L.LatLng): number {
  let closest = 0
  let closestDistance = squaredDistance(points[0], latlng)
  for (let i = 1; i < points.length; i++) {
    const distance = squaredDistance(points[i], latlng)
    if (distance < closestDistance) {
      closestDistance = distance
      closest = i
    }
  }
  return closest
}

function squaredDistance(point: TrackPoint, latlng: L.LatLng): number {
  const dLat = point.lat - latlng.lat
  const dLon = point.lon - latlng.lng
  return dLat * dLat + dLon * dLon
}

function nearestIndexByTimeOffset(points: TrackPoint[], startMs: number, targetSeconds: number): number {
  let closest = 0
  let closestDistance = Infinity
  for (let i = 0; i < points.length; i++) {
    const offset = (new Date(points[i].time!).getTime() - startMs) / 1000
    const distance = Math.abs(offset - targetSeconds)
    if (distance < closestDistance) {
      closestDistance = distance
      closest = i
    }
  }
  return closest
}
