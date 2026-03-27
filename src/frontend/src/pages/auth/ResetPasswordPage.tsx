import { useState, type FormEvent } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import api from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Eye, EyeOff, Check, Loader2 } from 'lucide-react'

export default function ResetPasswordPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') || ''
  const email = searchParams.get('email') || ''

  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState(false)

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError('')

    if (!newPassword) {
      setError(t('auth.resetPassword.passwordRequired', 'La contrasena es requerida.'))
      return
    }
    if (newPassword !== confirmPassword) {
      setError(t('auth.resetPassword.passwordMismatch', 'Las contrasenas no coinciden.'))
      return
    }

    setLoading(true)
    try {
      await api.post('/auth/reset-password', { token, email, newPassword })
      setSuccess(true)
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail
      setError(msg || t('auth.resetPassword.error', 'No se pudo restablecer la contrasena. El enlace puede haber expirado.'))
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
          {success ? (
            <>
              <div className="mx-auto mb-2 flex h-12 w-12 items-center justify-center rounded-full bg-green-500/10">
                <Check className="h-6 w-6 text-green-600 dark:text-green-400" />
              </div>
              <CardTitle className="text-2xl">
                {t('auth.resetPassword.successTitle', 'Contrasena actualizada')}
              </CardTitle>
            </>
          ) : (
            <>
              <CardTitle className="text-2xl">MyFO</CardTitle>
              <CardDescription>
                {t('auth.resetPassword.subtitle', 'Nueva contrasena')}
              </CardDescription>
            </>
          )}
        </CardHeader>
        <CardContent>
          {success ? (
            <div className="space-y-4 text-center">
              <p className="text-sm text-muted-foreground">
                {t('auth.resetPassword.successMessage', 'Tu contrasena fue actualizada correctamente.')}
              </p>
              <Button className="w-full" onClick={() => navigate('/auth')}>
                {t('auth.resetPassword.login', 'Iniciar sesion')}
              </Button>
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="space-y-4">
              {error && (
                <div className="bg-destructive/10 text-destructive text-sm p-3 rounded-md">
                  {error}
                </div>
              )}

              <div className="space-y-2">
                <Label htmlFor="newPassword">
                  {t('auth.resetPassword.newPassword', 'Nueva contrasena')}
                </Label>
                <div className="relative">
                  <Input
                    id="newPassword"
                    type={showPassword ? 'text' : 'password'}
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    required
                    autoFocus
                    className="pr-10"
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword(!showPassword)}
                    className="absolute right-2.5 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                    tabIndex={-1}
                  >
                    {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                  </button>
                </div>
                <p className="text-xs text-muted-foreground">
                  {t('auth.resetPassword.passwordHint', 'Minimo 8 caracteres, una mayuscula y un numero.')}
                </p>
              </div>

              <div className="space-y-2">
                <Label htmlFor="confirmPassword">
                  {t('auth.resetPassword.confirmPassword', 'Confirmar contrasena')}
                </Label>
                <div className="relative">
                  <Input
                    id="confirmPassword"
                    type={showConfirmPassword ? 'text' : 'password'}
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    required
                    className="pr-10"
                  />
                  <button
                    type="button"
                    onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                    className="absolute right-2.5 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                    tabIndex={-1}
                  >
                    {showConfirmPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                  </button>
                </div>
              </div>

              <Button type="submit" className="w-full" disabled={loading}>
                {loading ? (
                  <>
                    <Loader2 className="h-4 w-4 animate-spin" />
                    {t('auth.resetPassword.loading', 'Actualizando...')}
                  </>
                ) : (
                  t('auth.resetPassword.submit', 'Restablecer contrasena')
                )}
              </Button>
            </form>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
