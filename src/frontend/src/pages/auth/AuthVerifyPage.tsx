import { useState, useEffect } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import api from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Check, Loader2 } from 'lucide-react'

export default function AuthVerifyPage() {
  const { t } = useTranslation()
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token')

  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading')
  const [errorMessage, setErrorMessage] = useState('')

  useEffect(() => {
    if (!token) {
      setStatus('error')
      setErrorMessage(t('auth.verify.noToken', 'Token de verificacion no encontrado.'))
      return
    }

    const verify = async () => {
      try {
        await api.post('/auth/verify-email', { token })
        setStatus('success')
        sessionStorage.removeItem('pending_registration')
      } catch (err: unknown) {
        setStatus('error')
        const msg = (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail
        setErrorMessage(msg || t('auth.verify.error', 'No se pudo verificar el email. El enlace puede haber expirado.'))
      }
    }

    verify()
  }, [token, t])

  return (
    <div
      className="min-h-screen flex items-center justify-center p-4 bg-cover bg-center bg-no-repeat"
      style={{ backgroundImage: 'url(/fondo_login.png)' }}
    >
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          {status === 'loading' && (
            <div className="mx-auto mb-2 flex h-12 w-12 items-center justify-center">
              <Loader2 className="h-8 w-8 animate-spin text-primary" />
            </div>
          )}
          {status === 'success' && (
            <div className="mx-auto mb-2 flex h-12 w-12 items-center justify-center rounded-full bg-green-500/10">
              <Check className="h-6 w-6 text-green-600 dark:text-green-400" />
            </div>
          )}
          {status === 'error' && (
            <div className="mx-auto mb-2 flex h-12 w-12 items-center justify-center rounded-full bg-destructive/10">
              <span className="text-destructive text-xl font-bold">X</span>
            </div>
          )}
          <CardTitle className="text-2xl">
            {status === 'loading' && t('auth.verify.verifying', 'Verificando...')}
            {status === 'success' && t('auth.verify.successTitle', 'Email verificado!')}
            {status === 'error' && t('auth.verify.errorTitle', 'Error de verificacion')}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4 text-center">
          {status === 'success' && (
            <>
              <p className="text-sm text-muted-foreground">
                {t('auth.verify.successMessage', 'Tu cuenta fue creada exitosamente.')}
              </p>
              <Button asChild className="w-full">
                <Link to="/auth">
                  {t('auth.verify.login', 'Iniciar sesion')}
                </Link>
              </Button>
            </>
          )}

          {status === 'error' && (
            <>
              <div className="bg-destructive/10 text-destructive text-sm p-3 rounded-md">
                {errorMessage}
              </div>
              <Link
                to="/auth"
                className="inline-block text-sm text-muted-foreground hover:text-foreground"
              >
                {t('auth.verify.backToEntry', 'Volver al inicio')}
              </Link>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
