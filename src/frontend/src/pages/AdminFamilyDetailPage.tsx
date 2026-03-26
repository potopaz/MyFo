import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams, Link } from 'react-router-dom'
import { toast } from 'sonner'
import { ArrowLeft } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Checkbox } from '@/components/ui/checkbox'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import api from '@/lib/api'
import type { AdminFamilyDetailDto } from '@/types/api'

interface ConfigForm {
  isEnabled: boolean
  maxMembers: string
  notes: string
  disabledReason: string
}

const LANGUAGE_LABELS: Record<string, string> = { es: 'Español', en: 'English' }

function fmtDate(iso: string) {
  const d = new Date(iso)
  return `${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}/${d.getFullYear()}`
}

export default function AdminFamilyDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { t } = useTranslation()
  const [family, setFamily] = useState<AdminFamilyDetailDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [form, setForm] = useState<ConfigForm>({ isEnabled: true, maxMembers: '', notes: '', disabledReason: '' })

  useEffect(() => {
    if (!id) return
    api.get<AdminFamilyDetailDto>(`/admin/families/${id}`)
      .then(({ data }) => {
        setFamily(data)
        setForm({
          isEnabled: data.isEnabled,
          maxMembers: data.maxMembers != null ? String(data.maxMembers) : '',
          notes: data.notes ?? '',
          disabledReason: data.disabledReason ?? '',
        })
      })
      .catch(() => toast.error(t('errors.serverError')))
      .finally(() => setLoading(false))
  }, [id, t])

  const handleSave = async () => {
    if (!id) return
    setSaving(true)
    try {
      const maxMembersParsed = form.maxMembers.trim() !== '' ? parseInt(form.maxMembers, 10) : null
      const { data } = await api.put<AdminFamilyDetailDto>(`/admin/families/${id}/config`, {
        isEnabled: form.isEnabled,
        maxMembers: maxMembersParsed,
        notes: form.notes.trim() || null,
        disabledReason: !form.isEnabled && form.disabledReason.trim() ? form.disabledReason.trim() : null,
      })
      setFamily(data)
      toast.success(t('admin.saved'))
    } catch {
      toast.error(t('errors.serverError'))
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <p className="text-muted-foreground">{t('common.loading')}</p>
  if (!family) return null

  return (
    <div className="space-y-6 max-w-2xl">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon-sm" asChild>
          <Link to="/admin/families"><ArrowLeft className="size-4" /></Link>
        </Button>
        <h1 className="text-2xl font-semibold">{family.name}</h1>
        <Badge variant={form.isEnabled ? 'default' : 'destructive'}>
          {form.isEnabled ? t('admin.enabled') : t('admin.disabled')}
        </Badge>
      </div>

      {/* Detalle + Configuración unificados */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">{t('admin.familyDetail')}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Info de la familia (solo lectura) */}
          <div className="grid grid-cols-2 gap-x-8 gap-y-2 text-sm">
            <div>
              <span className="text-muted-foreground">Moneda principal</span>
              <p className="font-medium">{family.primaryCurrencyCode}</p>
            </div>
            <div>
              <span className="text-muted-foreground">Moneda secundaria</span>
              <p className="font-medium">{family.secondaryCurrencyCode}</p>
            </div>
            <div>
              <span className="text-muted-foreground">Idioma</span>
              <p className="font-medium">{LANGUAGE_LABELS[family.language] ?? family.language}</p>
            </div>
            <div>
              <span className="text-muted-foreground">Creada el</span>
              <p className="font-medium">{fmtDate(family.createdAt)}</p>
            </div>
            {family.disabledAt && (
              <div>
                <span className="text-muted-foreground">{t('admin.disabledAt')}</span>
                <p className="font-medium">{fmtDate(family.disabledAt)}</p>
              </div>
            )}
            {family.disabledReason && (
              <div className="col-span-2">
                <span className="text-muted-foreground">{t('admin.disabledReason')}</span>
                <p className="font-medium">{family.disabledReason}</p>
              </div>
            )}
          </div>

          <div className="border-t pt-4 space-y-4">
            {/* Habilitación + Máx. miembros en la misma fila */}
            <div className="flex items-center gap-6">
              <div className="flex items-center gap-2">
                <Checkbox
                  id="isEnabled"
                  checked={form.isEnabled}
                  onCheckedChange={(v) => setForm((p) => ({ ...p, isEnabled: !!v }))}
                />
                <Label htmlFor="isEnabled">{t('admin.isEnabled')}</Label>
              </div>
              <div className="h-4 w-px bg-border" />
              <div className="flex items-center gap-2">
                <Label htmlFor="maxMembers" className="text-muted-foreground whitespace-nowrap">
                  {t('admin.maxMembers')}
                </Label>
                <div className="w-14 shrink-0">
                  <Input
                    id="maxMembers"
                    value={form.maxMembers}
                    onChange={(e) => {
                      const v = e.target.value
                      if (/^\d*$/.test(v)) setForm((p) => ({ ...p, maxMembers: v }))
                    }}
                    inputMode="numeric"
                    placeholder="—"
                    className="h-7 text-sm"
                  />
                </div>
              </div>
            </div>

            {!form.isEnabled && (
              <div className="space-y-1">
                <Label htmlFor="disabledReason">{t('admin.disabledReasonLabel')}</Label>
                <Input
                  id="disabledReason"
                  value={form.disabledReason}
                  onChange={(e) => setForm((p) => ({ ...p, disabledReason: e.target.value }))}
                  placeholder={t('admin.disabledReasonPlaceholder')}
                  maxLength={200}
                />
              </div>
            )}

            <div className="space-y-1">
              <Label htmlFor="notes">{t('admin.notes')}</Label>
              <textarea
                id="notes"
                value={form.notes}
                onChange={(e) => setForm((p) => ({ ...p, notes: e.target.value }))}
                placeholder={t('admin.notesPlaceholder')}
                maxLength={500}
                rows={3}
                className="flex min-h-[80px] w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
              />
            </div>

            <div className="flex justify-end">
              <Button onClick={handleSave} disabled={saving}>
                {saving ? t('admin.saving') : t('admin.save')}
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Miembros */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">
            {t('admin.membersSection')}
            <span className="ml-2 text-sm font-normal text-muted-foreground">
              {family.memberCount} {family.memberCount === 1 ? 'activo' : 'activos'}
            </span>
          </CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <table className="w-full text-sm">
            <thead className="border-b bg-muted/40">
              <tr>
                <th className="px-2 h-10 text-left align-middle font-medium">{t('admin.memberName')}</th>
                <th className="px-2 h-10 text-left align-middle font-medium">{t('admin.memberRole')}</th>
                <th className="px-2 h-10 text-left align-middle font-medium">{t('admin.memberStatus')}</th>
              </tr>
            </thead>
            <tbody>
              {family.members.map((m) => (
                <tr key={m.memberId} className="border-b last:border-0 hover:bg-muted/50 transition-colors">
                  <td className="px-2 h-10 align-middle font-medium">{m.displayName}</td>
                  <td className="px-2 h-10 align-middle text-muted-foreground">{m.role}</td>
                  <td className="px-2 h-10 align-middle">
                    <Badge variant={m.isActive ? 'default' : 'secondary'}>
                      {m.isActive ? t('common.active') : t('common.inactive')}
                    </Badge>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </CardContent>
      </Card>
    </div>
  )
}
