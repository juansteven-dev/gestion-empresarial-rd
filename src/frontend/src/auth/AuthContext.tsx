import { createContext, useContext, useEffect, useMemo, useState } from 'react'
import { apiClient, resetCsrfRequestToken } from '../api/httpClient'

export interface AuthUser {
  id: string
  userName: string
  email: string | null
  roles: string[]
}

interface Credentials {
  userName: string
  password: string
}

interface AuthContextValue {
  user: AuthUser | null
  isLoading: boolean
  login: (credentials: Credentials) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let isMounted = true

    apiClient
      .get<AuthUser>('/auth/me')
      .then(({ data }) => {
        if (isMounted) setUser(data)
      })
      .catch(() => {
        if (isMounted) setUser(null)
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isLoading,
      async login(credentials) {
        const { data } = await apiClient.post<AuthUser>('/auth/login', credentials)
        resetCsrfRequestToken()
        setUser(data)
      },
      async logout() {
        await apiClient.post('/auth/logout')
        resetCsrfRequestToken()
        setUser(null)
      },
    }),
    [isLoading, user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth debe usarse dentro de AuthProvider.')
  return context
}
