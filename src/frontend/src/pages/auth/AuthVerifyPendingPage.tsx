import { useState, useEffect, useCallback } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import api from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Mail, ArrowLeft, Loader2 } from 'lucide-react'

export default function AuthVerifyPendingPage() {
  const { t } = useTranslation()
  const location = useLocation()
  const navigate = useNavigate()
  const email = (location.state as { email?: string })?.email

  const [cooldown, setCooldown] = useState(0)
  const [resending, setResending] = useState(false)
  const [resendSuccess, setResendSuccess] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!email) {
      navigate('/auth', { replace: true })
    }
  }, [email, navigate])

  useEffect(() => {
    if (cooldown <= 0) return
    const timer = setInterval(() => {
      setCooldown((prev) => prev - 1)
    }, 1000)
    return () => clearInterval(timer)
  }, [cooldown])

  const handleResend = useCallback(async () => {
    setError('')
    setResendSuccess(false)

    const stored = sessionStorage.getItem('pending_registration')
    if (!stored) {
      setError(t('auth.verifyPending.noData', 'No se encontraron los datos de registro. Volve a registrarte.'))
      return
    }

    setResending(true)
    try {
      const data = JSON.parse(stored)
      await api.post('/auth/initiate-registration', data)
      setResendSuccess(true)
      setCooldown(60)
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail
      setError(msg || t('auth.verifyPending.resendError', 'No se pudo reenviar el email.'))
    } finally {
      setResending(false)
    }
  }, [t])

  if (!email) return null

  return (
    <div
      className="min-h-screen flex items-center justify-center p-4 bg-cover bg-center bg-no-repeat"
      style={{ backgroundImage: 'url(/fondo_login.png)' }}
    >
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <div className="mx-auto mb-2 flex h-12 w-12 items-center justify-center rounded-full bg-primary/10">
            <Mail className="h-6 w-6 text-primary" />
          </div>
          <CardTitle className="text-2xl">
            {t('auth.verifyPending.title', 'Verifica tu email')}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4 text-center">
          <p className="text-sm text-muted-foreground">
            {t('auth.verifyPending.message', 'Te enviamos un email de verificacion a:')}
          </p>
          <p className="text-sm font-medium">{email}</p>
          <p className="text-sm text-muted-foreground">
            {t('auth.verifyPending.checkSpam', 'Revisa tu bandeja de entrada y la carpeta de spam.')}
          </p>

          {error && (
            <div className="bg-destructive/10 text-destructive text-sm p-3 rounded-md">
              {error}
            </div>
          )}

          {resendSuccess && (
            <div className="bg-green-500/10 text-green-700 dark:text-green-400 text-sm p-3 rounded-md">
              {t('auth.verifyPending.resendSuccess', 'Email reenviado correctamente.')}
            </div>
          )}

          <Button
            type="button"
            variant="outline"
            className="w-full"
            disabled={resending || cooldown > 0}
            onClick={handleResend}
          >
            {resending ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin" />
                {t('auth.verifyPending.resending', 'Reenviando...')}
              </>
            ) : cooldown > 0 ? (
              t('auth.verifyPending.resendCooldown', 'Reenviar email ({{seconds}}s)', { seconds: cooldown })
            ) : (
              t('auth.verifyPending.resend', 'Reenviar email')
            )}
          </Button>

          <div>
            <Link
              to="/auth"
              className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
            >
              <ArrowLeft className="h-4 w-4" />
              {t('auth.verifyPending.useAnotherEmail', 'Usar otro email')}
            </Link>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
