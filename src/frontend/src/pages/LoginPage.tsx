import { useState, type FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { getApiErrorMessage } from '../api/httpClient'
import { useAuth } from '../auth/AuthContext'

interface RedirectState {
  from?: { pathname?: string }
}

export function LoginPage() {
  const { user, isLoading, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  const [errorMessage, setErrorMessage] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  if (isLoading) {
    return (
      <main className="auth-loading" aria-live="polite">
        Verificando sesión…
      </main>
    )
  }

  if (user) return <Navigate to="/dashboard" replace />

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setErrorMessage('')
    setIsSubmitting(true)

    try {
      await login({ userName, password })
      const redirectTo = (location.state as RedirectState | null)?.from?.pathname
      navigate(redirectTo || '/dashboard', { replace: true })
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="login-page">
      <section className="login-card" aria-labelledby="login-title">
        <div className="login-brand-mark" aria-hidden="true">GE</div>
        <p className="eyebrow">GESTIÓN EMPRESARIAL RD</p>
        <h1 id="login-title">Iniciar sesión</h1>
        <p className="login-description">
          Accede con la cuenta que te asignó el administrador.
        </p>

        <form className="login-form" onSubmit={handleSubmit}>
          <label htmlFor="userName">Usuario</label>
          <input
            id="userName"
            name="userName"
            type="text"
            autoComplete="username"
            value={userName}
            onChange={(event) => setUserName(event.target.value)}
            required
            disabled={isSubmitting}
          />

          <label htmlFor="password">Contraseña</label>
          <input
            id="password"
            name="password"
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
            disabled={isSubmitting}
          />

          {errorMessage && (
            <p className="login-error" role="alert">{errorMessage}</p>
          )}

          <button className="login-submit" type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Comprobando…' : 'Entrar'}
          </button>
        </form>
      </section>
    </main>
  )
}
