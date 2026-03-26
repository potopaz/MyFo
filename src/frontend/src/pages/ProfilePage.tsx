import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAuth } from '@/contexts/AuthContext'
import api from '@/lib/api'
import axios from 'axios'

function extractErrorMessage(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data
    if (data?.message) return data.message
    if (data?.title) return data.title
  }
  return 'Ocurrió un error inesperado'
}

export default function ProfilePage() {
  const { t } = useTranslation()
  const { fullName: authName, email: authEmail, updateFullName } = useAuth()
  const [fullName, setFullName] = useState(authName ?? '')
  const [email] = useState(authEmail ?? '')
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [saving, setSaving] = useState(false)
  const [changingPassword, setChangingPassword] = useState(false)

  useEffect(() => {
    setFullName(authName ?? '')
  }, [authName])

  const handleSaveProfile = async () => {
    if (!fullName.trim()) {
      toast.error(t('profile.errors.nameRequired'))
      return
    }
    setSaving(true)
    try {
      await api.put('/auth/profile', { fullName: fullName.trim() })
      updateFullName(fullName.trim())
      toast.success(t('profile.saved'))
    } catch (err) {
      toast.error(extractErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  const handleChangePassword = async () => {
    if (!currentPassword) {
      toast.error(t('profile.errors.currentPasswordRequired'))
      return
    }
    if (!newPassword || newPassword.length < 6) {
      toast.error(t('profile.errors.passwordMinLength'))
      return
    }
    if (newPassword !== confirmPassword) {
      toast.error(t('profile.errors.passwordMismatch'))
      return
    }
    setChangingPassword(true)
    try {
      await api.put('/auth/change-password', { currentPassword, newPassword })
      toast.success(t('profile.passwordChanged'))
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
    } catch (err) {
      toast.error(extractErrorMessage(err))
    } finally {
      setChangingPassword(false)
    }
  }

  return (
    <div className="space-y-6 max-w-xl">
      <h1 className="text-2xl font-bold">{t('profile.title')}</h1>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">{t('profile.personalData')}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="email">{t('profile.email')}</Label>
            <Input id="email" value={email} disabled className="bg-muted" />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="fullName">{t('profile.fullName')}</Label>
            <Input
              id="fullName"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              maxLength={100}
            />
          </div>
          <Button onClick={handleSaveProfile} disabled={saving}>
            {saving ? t('profile.saving') : t('profile.save')}
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">{t('profile.changePassword')}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="currentPassword">{t('profile.currentPassword')}</Label>
            <Input
              id="currentPassword"
              type="password"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="newPassword">{t('profile.newPassword')}</Label>
            <Input
              id="newPassword"
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="confirmPassword">{t('profile.confirmPassword')}</Label>
            <Input
              id="confirmPassword"
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
            />
          </div>
          <Button onClick={handleChangePassword} disabled={changingPassword}>
            {changingPassword ? t('profile.changing') : t('profile.changePasswordBtn')}
          </Button>
        </CardContent>
      </Card>
    </div>
  )
}
