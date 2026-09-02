import { createContext, useContext, useState, type ReactNode } from 'react'

interface SelectedActivityContextValue {
  selectedActivityId: string | null
  setSelectedActivityId: (id: string | null) => void
}

const SelectedActivityContext = createContext<SelectedActivityContextValue | null>(null)

export function SelectedActivityProvider({ children }: { children: ReactNode }) {
  const [selectedActivityId, setSelectedActivityId] = useState<string | null>(null)
  return (
    <SelectedActivityContext.Provider value={{ selectedActivityId, setSelectedActivityId }}>
      {children}
    </SelectedActivityContext.Provider>
  )
}

export function useSelectedActivity(): SelectedActivityContextValue {
  const context = useContext(SelectedActivityContext)
  if (!context) throw new Error('useSelectedActivity must be used within a SelectedActivityProvider')
  return context
}
