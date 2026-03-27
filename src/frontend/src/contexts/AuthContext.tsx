import { createContext, useContext, useState, type ReactNode } from 'react'
import type { AuthResponse, UserFamily } from '@/types/api'
import api from '@/lib/api'

interface AuthState {
  token: string | null
  userId: string | null
  email: string | null
  fullName: string | null
  families: UserFamily[]
  isSuperAdmin: boolean
}

interface AuthContextType extends AuthState {
  login: (email: string, password: string) => Promise<AuthResponse>
  logout: () => void
  updateFullName: (fullName: string) => void
  updateAuthFromResponse: (response: AuthResponse) => void
  isAuthenticated: boolean
  hasFamilySelected: boolean
  currentFamily: UserFamily | null
  isFamilyAdmin: boolean
  // Legacy (invitation flow)
  registerWithInvitation: (data: RegisterWithInvitationData) => Promise<void>
  acceptInvitation: (token: string) => Promise<void>
}

interface RegisterWithInvitationData {
  email: string
  password: string
  fullName: string
  invitationToken: string
}

const AuthContext = createContext<AuthContextType | null>(null)

function decodeJwtClaim(token: string, claim: string): string | null {
  try {
    const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')))
    return payload[claim] ?? null
  } catch {
    return null
  }
}

function loadState(): AuthState {
  const token = localStorage.getItem('token')
  const user = localStorage.getItem('user')
  if (token && user) {
    try {
      const parsed = JSON.parse(user)
      return {
        token,
        ...parsed,
        isSuperAdmin: parsed.isSuperAdmin ?? decodeJwtClaim(token, 'is_super_admin') === 'true',
      }
    } catch {
      // corrupted data
    }
  }
  return { token: null, userId: null, email: null, fullName: null, families: [], isSuperAdmin: false }
}

function saveState(response: AuthResponse) {
  localStorage.setItem('token', response.token)
  localStorage.setItem('user', JSON.stringify({
    userId: response.userId,
    email: response.email,
    fullName: response.fullName,
    families: response.families,
    isSuperAdmin: response.isSuperAdmin,
  }))
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(loadState)

  // Note: localStorage is cleared explicitly in logout().
  // No useEffect needed here — it caused race conditions with external auth callbacks.

  const login = async (email: string, password: string): Promise<AuthResponse> => {
    const { data } = await api.post<AuthResponse>('/auth/login', { email, password })
    saveState(data)
    setState({
      token: data.token,
      userId: data.userId,
      email: data.email,
      fullName: data.fullName,
      families: data.families,
      isSuperAdmin: data.isSuperAdmin,
    })
    return data
  }

  const updateAuthFromResponse = (response: AuthResponse) => {
    saveState(response)
    setState({
      token: response.token,
      userId: response.userId,
      email: response.email,
      fullName: response.fullName,
      families: response.families,
      isSuperAdmin: response.isSuperAdmin,
    })
  }

  const registerWithInvitation = async (registerData: RegisterWithInvitationData) => {
    const { data } = await api.post<AuthResponse>('/auth/register-with-invitation', registerData)
    saveState(data)
    setState({
      token: data.token,
      userId: data.userId,
      email: data.email,
      fullName: data.fullName,
      families: data.families,
      isSuperAdmin: data.isSuperAdmin,
    })
  }

  const acceptInvitation = async (token: string) => {
    const { data } = await api.post<AuthResponse>(`/invitations/${token}/accept`)
    saveState(data)
    setState({
      token: data.token,
      userId: data.userId,
      email: data.email,
      fullName: data.fullName,
      families: data.families,
      isSuperAdmin: data.isSuperAdmin,
    })
  }

  const logout = () => {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    sessionStorage.removeItem('pending_registration')
    setState({ token: null, userId: null, email: null, fullName: null, families: [], isSuperAdmin: false })
  }

  const updateFullName = (fullName: string) => {
    setState((prev) => {
      const next = { ...prev, fullName }
      localStorage.setItem('user', JSON.stringify({
        userId: next.userId,
        email: next.email,
        fullName: next.fullName,
        families: next.families,
        isSuperAdmin: next.isSuperAdmin,
      }))
      return next
    })
  }

  const hasFamilySelected = state.token ? !!decodeJwtClaim(state.token, 'family_id') : false
  const currentFamily = state.families[0] ?? null
  const isFamilyAdmin = currentFamily?.role === 'FamilyAdmin'

  return (
    <AuthContext.Provider value={{
      ...state,
      login,
      logout,
      updateFullName,
      updateAuthFromResponse,
      registerWithInvitation,
      acceptInvitation,
      isAuthenticated: !!state.token,
      hasFamilySelected,
      currentFamily,
      isFamilyAdmin,
    }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used within AuthProvider')
  return context
}
