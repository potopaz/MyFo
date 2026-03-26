import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/contexts/AuthContext'
import api from '@/lib/api'
import type { SelectFamilyResponse } from '@/types/api'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Plus, Shield, Users, Loader2 } from 'lucide-react'

export default function SelectFamilyPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { fullName, families, isSuperAdmin, logout } = useAuth()

  const [loading, setLoading] = useState<string | null>(null)
  const [error, setError] = useState('')

  const handleSelectFamily = async (familyId: string) => {
    setError('')
    setLoading(familyId)
    try {
      const { data } = await api.post<SelectFamilyResponse>('/auth/select-family', { familyId })

      // Update token with the full JWT (includes family_id)
      localStorage.setItem('token', data.token)
      // Force page reload to re-initialize AuthContext with new token
      window.location.href = '/'
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail
      setError(msg || t('auth.selectFamily.error'))
    } finally {
      setLoading(null)
    }
  }

  const handleLogout = () => {
    logout()
    navigate('/auth')
  }

  const getRoleLabel = (role: string) => {
    switch (role) {
      case 'FamilyAdmin':
        return t('auth.selectFamily.admin')
      case 'Member':
        return t('auth.selectFamily.member')
      default:
        return role
    }
  }

  return (
    <div
      className="min-h-screen flex items-center justify-center p-4 bg-cover bg-center bg-no-repeat"
      style={{ backgroundImage: 'url(/fondo_login.png)' }}
    >
      <Card className="w-full max-w-lg">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl">
            {t('auth.selectFamily.greeting', { name: fullName })}
          </CardTitle>
          <p className="text-sm text-muted-foreground">
            {t('auth.selectFamily.title')}
          </p>
        </CardHeader>
        <CardContent className="space-y-4">
          {error && (
            <div className="bg-destructive/10 text-destructive text-sm p-3 rounded-md">
              {error}
            </div>
          )}

          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            {families.map((family) => (
              <button
                key={family.familyId}
                onClick={() => handleSelectFamily(family.familyId)}
                disabled={loading !== null}
                className="flex items-center gap-3 rounded-xl border bg-card p-4 text-left transition-colors hover:bg-muted/50 disabled:opacity-50"
              >
                <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-primary/10">
                  {loading === family.familyId ? (
                    <Loader2 className="h-5 w-5 animate-spin text-primary" />
                  ) : (
                    <Users className="h-5 w-5 text-primary" />
                  )}
                </div>
                <div className="min-w-0 flex-1">
                  <p className="text-sm font-medium truncate">{family.familyName}</p>
                  <p className="text-xs text-muted-foreground">{getRoleLabel(family.role)}</p>
                </div>
              </button>
            ))}

            <button
              onClick={() => navigate('/auth/create-family')}
              disabled={loading !== null}
              className="flex items-center gap-3 rounded-xl border border-dashed p-4 text-left transition-colors hover:bg-muted/50 disabled:opacity-50"
            >
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-muted">
                <Plus className="h-5 w-5 text-muted-foreground" />
              </div>
              <div>
                <p className="text-sm font-medium">{t('auth.selectFamily.createNew')}</p>
              </div>
            </button>

            {isSuperAdmin && (
              <button
                onClick={() => navigate('/admin/families')}
                disabled={loading !== null}
                className="flex items-center gap-3 rounded-xl border p-4 text-left transition-colors hover:bg-muted/50 disabled:opacity-50"
              >
                <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-orange-500/10">
                  <Shield className="h-5 w-5 text-orange-600 dark:text-orange-400" />
                </div>
                <div>
                  <p className="text-sm font-medium">{t('auth.selectFamily.superAdmin')}</p>
                </div>
              </button>
            )}
          </div>

          <div className="text-center pt-2">
            <button
              onClick={handleLogout}
              className="text-sm text-muted-foreground hover:text-foreground"
            >
              {t('auth.selectFamily.logout')}
            </button>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
