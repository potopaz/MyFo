import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/contexts/AuthContext'
import api from '@/lib/api'
import type { AuthResponse } from '@/types/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { ArrowLeft, Loader2 } from 'lucide-react'

export default function CreateFamilyPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { updateAuthFromResponse } = useAuth()

  const [name, setName] = useState('')
  const [primaryCurrencyCode, setPrimaryCurrencyCode] = useState('ARS')
  const [secondaryCurrencyCode, setSecondaryCurrencyCode] = useState('USD')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!name.trim()) return
    setError('')
    setLoading(true)
    try {
      const { data } = await api.post<AuthResponse>('/auth/create-family', {
        name: name.trim(),
        primaryCurrencyCode: primaryCurrencyCode.toUpperCase(),
        secondaryCurrencyCode: secondaryCurrencyCode.toUpperCase(),
      })

      updateAuthFromResponse(data)
      navigate('/auth/select-family')
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail
      setError(msg || t('auth.createFamily.error'))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div
      className="min-h-screen flex items-center justify-center p-4 bg-cover bg-center bg-no-repeat"
      style={{ backgroundImage: 'url(/fondo_login.png)' }}
    >
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl">MyFO</CardTitle>
          <CardDescription>{t('auth.createFamily.title')}</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            {error && (
              <div className="bg-destructive/10 text-destructive text-sm p-3 rounded-md">
                {error}
              </div>
            )}

            <div className="space-y-2">
              <Label htmlFor="name">{t('auth.createFamily.name')}</Label>
              <Input
                id="name"
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder={t('auth.createFamily.namePlaceholder')}
                required
                autoFocus
                maxLength={100}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="primaryCurrency">{t('auth.createFamily.primaryCurrency')}</Label>
              <Input
                id="primaryCurrency"
                type="text"
                value={primaryCurrencyCode}
                onChange={(e) => setPrimaryCurrencyCode(e.target.value.toUpperCase().slice(0, 3))}
                maxLength={3}
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="secondaryCurrency">{t('auth.createFamily.secondaryCurrency')}</Label>
              <Input
                id="secondaryCurrency"
                type="text"
                value={secondaryCurrencyCode}
                onChange={(e) => setSecondaryCurrencyCode(e.target.value.toUpperCase().slice(0, 3))}
                maxLength={3}
                required
              />
            </div>

            <p className="text-xs text-muted-foreground">{t('auth.createFamily.hint')}</p>

            <Button type="submit" className="w-full" disabled={loading}>
              {loading ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" />
                  {t('auth.createFamily.loading')}
                </>
              ) : (
                t('auth.createFamily.submit')
              )}
            </Button>

            <div className="text-center">
              <Link
                to="/auth/select-family"
                className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
              >
                <ArrowLeft className="h-4 w-4" />
                {t('auth.createFamily.back')}
              </Link>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
