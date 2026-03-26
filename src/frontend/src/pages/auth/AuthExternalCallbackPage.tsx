import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Loader2 } from 'lucide-react'
import type { UserFamily } from '@/types/api'

export default function AuthExternalCallbackPage() {
  const { t } = useTranslation()
  const [searchParams] = useSearchParams()
  const [error, setError] = useState('')

  useEffect(() => {
    const urlError = searchParams.get('error')
    if (urlError) {
      setError(t('auth.external.error'))
      setTimeout(() => { window.location.href = '/auth' }, 3000)
      return
    }

    const token = searchParams.get('token')
    const userId = searchParams.get('userId')
    const email = searchParams.get('email')
    const fullName = searchParams.get('fullName')
    const isSuperAdmin = searchParams.get('isSuperAdmin') === 'true'
    const familiesJson = searchParams.get('families')

    if (!token || !userId || !email || !fullName) {
      setError(t('auth.external.error'))
      setTimeout(() => { window.location.href = '/auth' }, 3000)
      return
    }

    let families: UserFamily[] = []
    try {
      families = familiesJson ? JSON.parse(familiesJson) : []
    } catch {
      families = []
    }

    // Save directly to localStorage (bypass React state to avoid race conditions)
    localStorage.setItem('token', token)
    localStorage.setItem('user', JSON.stringify({
      userId,
      email,
      fullName,
      families,
      isSuperAdmin,
    }))

    // Full page reload so AuthContext initializes fresh with token in localStorage
    window.location.href = families.length > 0 ? '/auth/select-family' : '/auth/create-family'
  }, [searchParams, t])

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center p-4">
        <div className="text-center space-y-4">
          <p className="text-destructive">{error}</p>
          <p className="text-sm text-muted-foreground">{t('auth.external.redirecting')}</p>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen flex items-center justify-center p-4">
      <div className="text-center space-y-4">
        <Loader2 className="h-8 w-8 animate-spin mx-auto" />
        <p className="text-muted-foreground">{t('auth.external.processing')}</p>
      </div>
    </div>
  )
}
