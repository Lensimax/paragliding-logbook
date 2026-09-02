import { useState } from 'react'
import { activityListRootView } from './app/routes'
import { AppProviders } from './app/providers'
import { AppShell } from './components/layout/AppShell'
import { useAuthContext } from './features/auth/AuthContext'
import { LoginForm } from './features/auth/components/LoginForm'
import { RegisterForm } from './features/auth/components/RegisterForm'
import './App.css'

function AppContent() {
  const { user, loading, register, login } = useAuthContext()
  const [mode, setMode] = useState<'login' | 'register'>('login')

  if (loading) {
    return (
      <section id="center">
        <p>Loading…</p>
      </section>
    )
  }

  if (!user) {
    return (
      <section id="center">
        {mode === 'login' ? (
          <LoginForm onLogin={login} onSwitchToRegister={() => setMode('register')} />
        ) : (
          <RegisterForm onRegister={register} onSwitchToLogin={() => setMode('login')} />
        )}
      </section>
    )
  }

  return <AppShell rootView={activityListRootView} />
}

function App() {
  return (
    <AppProviders>
      <AppContent />
    </AppProviders>
  )
}

export default App
