import L from 'leaflet'

export function createCursorMarker(): L.CircleMarker {
  return L.circleMarker([0, 0], {
    radius: 6,
    weight: 2,
    color: '#fff',
    fillColor: '#e2431e',
    fillOpacity: 1,
    interactive: false,
  })
}
