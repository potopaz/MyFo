import { useState, useEffect, type FormEvent } from 'react'
import { useSearchParams, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/contexts/AuthContext'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Eye, EyeOff } from 'lucide-react'
import api from '@/lib/api'
import type { InvitationInfoDto } from '@/types/api'

type Tab = 'register' | 'login'

export default function JoinPage() {
  const { t } = useTranslation()
  const { isAuthenticated, login, registerWithInvitation, acceptInvitation } = useAuth()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''

  const [info, setInfo] = useState<InvitationInfoDto | null>(null)
  const [loadingInfo, setLoadingInfo] = useState(true)
  const [tab, setTab] = useState<Tab>('register')
  const [showPassword, setShowPassword] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const [loginForm, setLoginForm] = useState({ email: '', password: '' })
  const [registerForm, setRegisterForm] = useState({ email: '', password: '', fullName: '' })

  useEffect(() => {
    if (!token) {
      setLoadingInfo(false)
      return
    }
    api.get<InvitationInfoDto>(`/invitations/${token}`)
      .then(({ data }) => setInfo(data))
      .catch(() => setInfo({ isValid: false, errorCode: 'NOT_FOUND', familyName: '', invitedByDisplayName: '', expiresAt: '' }))
      .finally(() => setLoadingInfo(false))
  }, [token])

  const handleAccept = async () => {
    setError('')
    setLoading(true)
    try {
      await acceptInvitation(token)
      navigate('/')
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail
      setError(msg || t('join.errors.acceptError'))
    } finally {
      setLoading(false)
    }
  }

  const handleLogin = async (e: FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      await login(loginForm.email, loginForm.password)
      await acceptInvitation(token)
      navigate('/')
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail
      setError(msg || t('join.errors.loginError'))
    } finally {
      setLoading(false)
    }
  }

  const handleRegister = async (e: FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      await registerWithInvitation({ ...registerForm, invitationToken: token })
      navigate('/')
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail
      setError(msg || t('join.errors.registerError'))
    } finally {
      setLoading(false)
    }
  }

  if (loadingInfo) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-cover bg-center bg-no-repeat" style={{ backgroundImage: 'url(/fondo_login.png)' }}>
        <p className="text-muted-foreground">{t('common.loading')}</p>
      </div>
    )
  }

  if (!token || !info?.isValid) {
    const code = info?.errorCode
    const msg = code === 'ALREADY_USED' ? t('join.errors.alreadyUsed')
      : code === 'EXPIRED' ? t('join.errors.expired')
      : t('join.errors.invalid')
    return (
      <div className="min-h-screen flex items-center justify-center p-4 bg-cover bg-center bg-no-repeat" style={{ backgroundImage: 'url(/fondo_login.png)' }}>
        <Card className="w-full max-w-md text-center">
          <CardHeader>
            <CardTitle className="text-xl">{t('join.invalidTitle')}</CardTitle>
            <CardDescription>{msg}</CardDescription>
          </CardHeader>
        </Card>
      </div>
    )
  }

  return (
    <div className="min-h-screen flex items-center justify-center p-4 bg-cover bg-center bg-no-repeat" style={{ backgroundImage: 'url(/fondo_login.png)' }}>
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl">MyFO</CardTitle>
          <CardDescription>
            {t('join.subtitle', { family: info.familyName, inviter: info.invitedByDisplayName })}
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {error && (
            <div className="bg-destructive/10 text-destructive text-sm p-3 rounded-md">{error}</div>
          )}

          {/* Caso: ya autenticado */}
          {isAuthenticated ? (
            <div className="space-y-4">
              <p className="text-sm text-muted-foreground text-center">{t('join.alreadyLoggedIn')}</p>
              <Button className="w-full" onClick={handleAccept} disabled={loading}>
                {loading ? t('join.accepting') : t('join.accept', { family: info.familyName })}
              </Button>
            </div>
          ) : (
            <>
              {/* Tabs */}
              <div className="flex rounded-lg border overflow-hidden text-sm">
                <button
                  type="button"
                  onClick={() => { setTab('register'); setError('') }}
                  className={`flex-1 py-2 transition-colors ${tab === 'register' ? 'bg-primary text-primary-foreground' : 'bg-background hover:bg-muted'}`}
                >
                  {t('join.tabRegister')}
                </button>
                <button
                  type="button"
                  onClick={() => { setTab('login'); setError('') }}
                  className={`flex-1 py-2 transition-colors ${tab === 'login' ? 'bg-primary text-primary-foreground' : 'bg-background hover:bg-muted'}`}
                >
                  {t('join.tabLogin')}
                </button>
              </div>

              {/* Registro */}
              {tab === 'register' && (
                <form onSubmit={handleRegister} className="space-y-3">
                  <div className="space-y-1.5">
                    <Label htmlFor="fullName">{t('auth.register.fullName')}</Label>
                    <Input
                      id="fullName"
                      value={registerForm.fullName}
                      onChange={(e) => setRegisterForm((p) => ({ ...p, fullName: e.target.value }))}
                      required
                      autoFocus
                      maxLength={100}
                    />
                  </div>
                  <div className="space-y-1.5">
                    <Label htmlFor="reg-email">{t('auth.register.email')}</Label>
                    <Input
                      id="reg-email"
                      type="email"
                      value={registerForm.email}
                      onChange={(e) => setRegisterForm((p) => ({ ...p, email: e.target.value }))}
                      required
                    />
                  </div>
                  <div className="space-y-1.5">
                    <Label htmlFor="reg-password">{t('auth.register.password')}</Label>
                    <div className="relative">
                      <Input
                        id="reg-password"
                        type={showPassword ? 'text' : 'password'}
                        value={registerForm.password}
                        onChange={(e) => setRegisterForm((p) => ({ ...p, password: e.target.value }))}
                        required
                        minLength={8}
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
                  </div>
                  <Button type="submit" className="w-full" disabled={loading}>
                    {loading ? t('join.registering') : t('join.registerAndJoin')}
                  </Button>
                </form>
              )}

              {/* Login */}
              {tab === 'login' && (
                <form onSubmit={handleLogin} className="space-y-3">
                  <div className="space-y-1.5">
                    <Label htmlFor="login-email">{t('auth.login.email')}</Label>
                    <Input
                      id="login-email"
                      type="email"
                      value={loginForm.email}
                      onChange={(e) => setLoginForm((p) => ({ ...p, email: e.target.value }))}
                      required
                      autoFocus
                    />
                  </div>
                  <div className="space-y-1.5">
                    <Label htmlFor="login-password">{t('auth.login.password')}</Label>
                    <div className="relative">
                      <Input
                        id="login-password"
                        type={showPassword ? 'text' : 'password'}
                        value={loginForm.password}
                        onChange={(e) => setLoginForm((p) => ({ ...p, password: e.target.value }))}
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
                  </div>
                  <Button type="submit" className="w-full" disabled={loading}>
                    {loading ? t('join.loggingIn') : t('join.loginAndJoin')}
                  </Button>
                </form>
              )}
            </>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
