import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { Eye } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import api from '@/lib/api'
import type { AdminFamilyListItemDto } from '@/types/api'

export default function AdminFamiliesPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [families, setFamilies] = useState<AdminFamilyListItemDto[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    api.get<AdminFamilyListItemDto[]>('/admin/families')
      .then(({ data }) => setFamilies(data))
      .catch(() => toast.error(t('errors.serverError')))
      .finally(() => setLoading(false))
  }, [t])

  if (loading) return <p className="text-muted-foreground">{t('common.loading')}</p>

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-semibold">{t('admin.familiesTitle')}</h1>

      <Card>
        <CardHeader>
          <CardTitle className="text-base font-medium">
            {families.length} {families.length === 1 ? 'familia' : 'familias'}
          </CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <table className="w-full text-sm">
            <thead className="border-b bg-muted/40">
              <tr>
                <th className="px-2 h-10 text-left align-middle font-medium whitespace-nowrap">{t('admin.columns.name')}</th>
                <th className="px-2 h-10 text-left align-middle font-medium whitespace-nowrap">{t('admin.columns.members')}</th>
                <th className="px-2 h-10 text-left align-middle font-medium whitespace-nowrap">{t('admin.columns.status')}</th>
                <th className="px-2 h-10 text-left align-middle font-medium whitespace-nowrap">{t('admin.columns.maxMembers')}</th>
                <th className="px-2 h-10 text-left align-middle font-medium whitespace-nowrap">{t('admin.columns.notes')}</th>
                <th className="px-2 h-10 text-right align-middle font-medium whitespace-nowrap">{t('admin.columns.actions')}</th>
              </tr>
            </thead>
            <tbody>
              {families.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-2 py-6 text-center text-muted-foreground">
                    {t('common.noData')}
                  </td>
                </tr>
              ) : (
                families.map((f) => (
                  <tr key={f.familyId} className="border-b last:border-0 hover:bg-muted/50 transition-colors">
                    <td className="px-2 h-10 align-middle font-medium">{f.name}</td>
                    <td className="px-2 h-10 align-middle text-muted-foreground">
                      {f.memberCount}
                      {f.maxMembers != null && ` / ${f.maxMembers}`}
                    </td>
                    <td className="px-2 h-10 align-middle">
                      <Badge variant={f.isEnabled ? 'default' : 'destructive'}>
                        {f.isEnabled ? t('admin.enabled') : t('admin.disabled')}
                      </Badge>
                    </td>
                    <td className="px-2 h-10 align-middle text-muted-foreground">
                      {f.maxMembers ?? '—'}
                    </td>
                    <td className="px-2 h-10 align-middle text-muted-foreground max-w-xs truncate">
                      {f.notes ?? '—'}
                    </td>
                    <td className="px-2 h-10 align-middle text-right">
                      <Button
                        variant="ghost"
                        size="icon-sm"
                        title={t('admin.columns.actions')}
                        onClick={() => navigate(`/admin/families/${f.familyId}`)}
                      >
                        <Eye className="h-3.5 w-3.5" />
                      </Button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </CardContent>
      </Card>
    </div>
  )
}
