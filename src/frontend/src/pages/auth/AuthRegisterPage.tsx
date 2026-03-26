import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import api from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Eye, EyeOff, ArrowLeft, Loader2 } from 'lucide-react'

export default function AuthRegisterPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const email = searchParams.get('email') || ''

  const [fullName, setFullName] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError('')

    if (!fullName.trim()) {
      setError(t('auth.register.nameRequired', 'El nombre es requerido.'))
      return
    }
    if (!password) {
      setError(t('auth.register.passwordRequired', 'La contrasena es requerida.'))
      return
    }
    if (password !== confirmPassword) {
      setError(t('auth.register.passwordMismatch', 'Las contrasenas no coinciden.'))
      return
    }

    const language = localStorage.getItem('myfo_language') || 'es'

    setLoading(true)
    try {
      await api.post('/auth/initiate-registration', {
        email,
        password,
        fullName: fullName.trim(),
        language,
      })

      sessionStorage.setItem(
        'pending_registration',
        JSON.stringify({ email, password, fullName: fullName.trim(), language })
      )

      navigate('/auth/verify-pending', { state: { email } })
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail
      setError(msg || t('auth.register.error', 'Ocurrio un error. Intenta de nuevo.'))
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
          <CardDescription>{t('auth.register.subtitle', 'Crear cuenta')}</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            {error && (
              <div className="bg-destructive/10 text-destructive text-sm p-3 rounded-md">
                {error}
              </div>
            )}

            <div className="space-y-2">
              <Label>{t('auth.register.email', 'Email')}</Label>
              <p className="text-sm text-muted-foreground bg-muted rounded-lg px-2.5 py-1.5">
                {email}
              </p>
            </div>

            <div className="space-y-2">
              <Label htmlFor="fullName">{t('auth.register.fullName', 'Nombre completo')}</Label>
              <Input
                id="fullName"
                type="text"
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                required
                autoFocus
                maxLength={100}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="password">{t('auth.register.password', 'Contrasena')}</Label>
              <div className="relative">
                <Input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
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
                {t('auth.register.passwordHint', 'Minimo 8 caracteres, una mayuscula y un numero.')}
              </p>
            </div>

            <div className="space-y-2">
              <Label htmlFor="confirmPassword">{t('auth.register.confirmPassword', 'Confirmar contrasena')}</Label>
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
                  {t('auth.register.loading', 'Creando cuenta...')}
                </>
              ) : (
                t('auth.register.submit', 'Crear cuenta')
              )}
            </Button>

            <div className="text-center">
              <Link
                to="/auth"
                className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
              >
                <ArrowLeft className="h-4 w-4" />
                {t('auth.register.back', 'Volver')}
              </Link>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
