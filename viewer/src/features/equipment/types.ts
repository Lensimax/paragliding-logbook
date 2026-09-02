export type EquipmentKind = 'wing' | 'harness' | 'reserve' | 'helmet' | 'radio' | 'variometer' | 'other'

export interface EquipmentSummary {
  id: string
  displayName: string
  type: EquipmentKind
  autoAdd: boolean
  retired: boolean
}

export interface EquipmentRevision {
  id: string
  revisionDate: string
  comment: string | null
}

export interface EquipmentUsage {
  activityCount: number
  hours: number
}

export interface EquipmentDetail {
  id: string
  displayName: string
  type: EquipmentKind
  brand: string | null
  model: string | null
  purchaseDate: string | null
  nextRevisionDate: string | null
  autoAdd: boolean
  retired: boolean
  revisions: EquipmentRevision[]
  usage: EquipmentUsage
}

export interface RevisionInput {
  revisionDate: string
  comment: string | null
}

export interface EquipmentPayload {
  displayName: string
  type: EquipmentKind
  brand: string | null
  model: string | null
  purchaseDate: string | null
  nextRevisionDate: string | null
  autoAdd: boolean
  revisions: RevisionInput[]
}
