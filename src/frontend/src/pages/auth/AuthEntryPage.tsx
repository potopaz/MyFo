import { useState, useRef, useEffect, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import api from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Loader2, Globe, ChevronDown } from 'lucide-react'

function FlagES() {
  return (
    <svg viewBox="0 0 640 480" className="h-4 w-6 rounded-sm shadow-sm">
      <path fill="#AA151B" d="M0 0h640v480H0z" />
      <path fill="#F1BF00" d="M0 120h640v240H0z" />
    </svg>
  )
}

function FlagUS() {
  return (
    <svg viewBox="0 0 640 480" className="h-4 w-6 rounded-sm shadow-sm">
      <path fill="#bd3d44" d="M0 0h640v37h-640zm0 74h640v37h-640zm0 74h640v37h-640zm0 74h640v37h-640zm0 74h640v37h-640zm0 74h640v37h-640zm0 74h640v37h-640z" />
      <path fill="#fff" d="M0 37h640v37h-640zm0 74h640v37h-640zm0 74h640v37h-640zm0 74h640v37h-640zm0 74h640v37h-640zm0 74h640v37h-640z" />
      <path fill="#192f5d" d="M0 0h260v259H0z" />
    </svg>
  )
}

const LANGUAGES = [
  { code: 'es', label: 'Español', Flag: FlagES },
  { code: 'en', label: 'English', Flag: FlagUS },
] as const

function GoogleIcon() {
  return (
    <svg viewBox="0 0 24 24" className="h-5 w-5">
      <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92a5.06 5.06 0 01-2.2 3.32v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.1z" />
      <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" />
      <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18A10.96 10.96 0 001 12c0 1.77.42 3.45 1.18 4.93l2.85-2.22.81-.62z" />
      <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" />
    </svg>
  )
}


export default function AuthEntryPage() {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [langOpen, setLangOpen] = useState(false)
  const langRef = useRef<HTMLDivElement>(null)
  const [language, setLanguage] = useState(
    () => localStorage.getItem('myfo_language') || 'es'
  )

  const currentLang = LANGUAGES.find(l => l.code === language) || LANGUAGES[0]
  const CurrentFlag = currentLang.Flag

  const handleLanguageChange = (lang: string) => {
    setLanguage(lang)
    setLangOpen(false)
    localStorage.setItem('myfo_language', lang)
    i18n.changeLanguage(lang)
  }

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (langRef.current && !langRef.current.contains(e.target as Node)) {
        setLangOpen(false)
      }
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [])

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!email.trim()) return
    setError('')
    setLoading(true)
    try {
      const { data } = await api.post<{ exists: boolean }>('/auth/check-email', { email: email.trim() })
      if (data.exists) {
        navigate(`/auth/login?email=${encodeURIComponent(email.trim())}`)
      } else {
        navigate(`/auth/register?email=${encodeURIComponent(email.trim())}`)
      }
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail
      setError(msg || t('auth.entry.error'))
    } finally {
      setLoading(false)
    }
  }

  const handleSocialLogin = (provider: string) => {
    window.location.href = `/api/auth/external-login?provider=${provider}`
  }

  return (
    <div
      className="min-h-screen flex items-center justify-center p-4 bg-cover bg-center bg-no-repeat"
      style={{ backgroundImage: 'url(/fondo_login.png)' }}
    >
      {/* Language selector */}
      <div className="absolute top-4 right-4" ref={langRef}>
        <button
          onClick={() => setLangOpen(!langOpen)}
          className="flex items-center gap-2 rounded-lg border border-input bg-background/80 backdrop-blur-sm px-3 py-2 text-sm shadow-sm transition-colors hover:bg-background"
        >
          <Globe className="h-4 w-4 text-muted-foreground" />
          <CurrentFlag />
          <span className="hidden sm:inline">{currentLang.label}</span>
          <ChevronDown className="h-3.5 w-3.5 text-muted-foreground" />
        </button>
        {langOpen && (
          <div className="absolute right-0 mt-1 w-44 rounded-lg border bg-popover shadow-lg z-50 overflow-hidden">
            {LANGUAGES.map((lang) => (
              <button
                key={lang.code}
                onClick={() => handleLanguageChange(lang.code)}
                className={`flex w-full items-center gap-3 px-3 py-2.5 text-sm transition-colors hover:bg-muted ${
                  language === lang.code ? 'bg-muted font-medium' : ''
                }`}
              >
                <lang.Flag />
                <span>{lang.label}</span>
              </button>
            ))}
          </div>
        )}
      </div>

      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl">{t('auth.entry.title')}</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            {error && (
              <div className="bg-destructive/10 text-destructive text-sm p-3 rounded-md">
                {error}
              </div>
            )}
            <div className="space-y-2">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder={t('auth.entry.emailPlaceholder')}
                required
                autoFocus
              />
            </div>
            <Button type="submit" className="w-full" disabled={loading}>
              {loading ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" />
                  {t('auth.entry.loading')}
                </>
              ) : (
                t('auth.entry.continue')
              )}
            </Button>

            <div className="relative my-4">
              <div className="absolute inset-0 flex items-center">
                <span className="w-full border-t" />
              </div>
              <div className="relative flex justify-center text-xs uppercase">
                <span className="bg-card px-2 text-muted-foreground">
                  {t('auth.entry.orContinueWith')}
                </span>
              </div>
            </div>

            <div className="space-y-2" translate="no">
              <Button
                type="button"
                variant="outline"
                className="w-full"
                onClick={() => handleSocialLogin('Google')}
              >
                <GoogleIcon />
                Google
              </Button>
              {/* TODO: Enable Apple and Microsoft login when configured */}
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
