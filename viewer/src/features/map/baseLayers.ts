import L from 'leaflet'

export interface BaseLayerDefinition {
  name: string
  layer: L.TileLayer
}

/** Layer switcher options per SPEC.md. IGN Géoportail (France-only, needs an API key) is skipped. */
export function createBaseLayers(): BaseLayerDefinition[] {
  return [
    {
      name: 'Satellite',
      layer: L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
        attribution: 'Tiles &copy; Esri',
        maxZoom: 19,
      }),
    },
    {
      name: 'Topographic',
      layer: L.tileLayer('https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png', {
        attribution: 'Map data: &copy; OpenStreetMap contributors, SRTM | Map style: &copy; OpenTopoMap',
        maxZoom: 17,
      }),
    },
  ]
}
