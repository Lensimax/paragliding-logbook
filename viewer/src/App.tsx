import { useState } from 'react'
import { LoginForm } from './features/auth/components/LoginForm'
import { RegisterForm } from './features/auth/components/RegisterForm'
import { useAuth } from './features/auth/useAuth'
import './App.css'

function App() {
  const { user, loading, register, login, logout } = useAuth()
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

  return (
    <section id="center">
      <h1>Welcome, {user.username}</h1>
      <button type="button" onClick={() => void logout()}>
        Log out
      </button>
    </section>
  )
}

export default App
