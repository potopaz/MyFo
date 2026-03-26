import { useCallback, useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { ConfigPage } from '@/components/crud'
import type { ColumnDef, FieldDef } from '@/components/crud'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Checkbox } from '@/components/ui/checkbox'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import api from '@/lib/api'
import { loadFamilyCurrencyOptions } from '@/lib/currency-options'
import { HelpCircle, ShieldCheck } from 'lucide-react'
import { toast } from 'sonner'
import type { CashBoxDto, CashBoxMemberPermissionDto } from '@/types/api'

function PermissionsDialog({ cashBox, onClose }: { cashBox: CashBoxDto; onClose: () => void }) {
  const { t } = useTranslation()
  const [original, setOriginal] = useState<CashBoxMemberPermissionDto[]>([])
  const [draft, setDraft] = useState<CashBoxMemberPermissionDto[]>([])
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)

  const loadPermissions = useCallback(async () => {
    setLoading(true)
    try {
      const { data } = await api.get<CashBoxMemberPermissionDto[]>(`/cashboxes/${cashBox.cashBoxId}/permissions`)
      const sorted = [...data].sort((a, b) => a.displayName.localeCompare(b.displayName))
      setOriginal(sorted)
      setDraft(sorted.map((p) => ({ ...p })))
    } catch {
      toast.error(t('cashBoxes.permissions.loadError'))
    } finally {
      setLoading(false)
    }
  }, [cashBox.cashBoxId, t])

  useEffect(() => { loadPermissions() }, [loadPermissions])

  const handleDraftChange = (memberId: string, isGranted: boolean) => {
    setDraft((prev) =>
      prev.map((p) => p.memberId === memberId
        ? { ...p, permission: isGranted ? 'Operate' as const : null }
        : p
      )
    )
  }

  const handleSave = async () => {
    setSaving(true)
    try {
      const changes = draft.filter((d) => {
        const orig = original.find((o) => o.memberId === d.memberId)
        return orig?.permission !== d.permission
      })

      await Promise.all(changes.map((d) => {
        if (d.permission === null) {
          return api.delete(`/cashboxes/${cashBox.cashBoxId}/permissions/${d.memberId}`)
        }
        return api.put(`/cashboxes/${cashBox.cashBoxId}/permissions/${d.memberId}`, {})
      }))

      toast.success(t('cashBoxes.permissions.updated'))
      onClose()
    } catch {
      toast.error(t('cashBoxes.permissions.saveError'))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Dialog open onOpenChange={(open) => { if (!open) onClose() }}>
      <DialogContent className="max-w-md" showCloseButton={false}>
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <ShieldCheck className="h-5 w-5 text-primary" />
            <span>{t('cashBoxes.permissions.title', { name: cashBox.name })}</span>
          </DialogTitle>
        </DialogHeader>
        {loading ? (
          <p className="py-4 text-center text-sm text-muted-foreground">{t('cashBoxes.permissions.loading')}</p>
        ) : draft.length === 0 ? (
          <p className="py-4 text-center text-sm text-muted-foreground">{t('cashBoxes.permissions.noMembers')}</p>
        ) : (
          <>
            <div className="space-y-2 py-2">
              {draft.map((p) => (
                <div key={p.memberId} className="flex items-center justify-between gap-4 rounded-md px-1 py-1.5">
                  <span className="text-sm font-medium">{p.displayName}</span>
                  <Checkbox
                    checked={p.permission === 'Operate'}
                    onCheckedChange={(checked) => handleDraftChange(p.memberId, checked === true)}
                    disabled={saving}
                  />
                </div>
              ))}
            </div>
            <div className="flex justify-end gap-2 pt-2 border-t">
              <Button variant="outline" onClick={onClose} disabled={saving}>{t('cashBoxes.permissions.cancel')}</Button>
              <Button onClick={handleSave} disabled={saving}>
                {saving ? t('cashBoxes.permissions.saving') : t('cashBoxes.permissions.save')}
              </Button>
            </div>
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}

export default function CashBoxesPage() {
  const { t } = useTranslation()
  const fetchItems = useCallback(() => api.get<CashBoxDto[]>('/cashboxes').then((r) => r.data), [])
  const [permissionsCashBox, setPermissionsCashBox] = useState<CashBoxDto | null>(null)

  const columns: ColumnDef<CashBoxDto>[] = useMemo(() => [
    { key: 'name', header: t('cashBoxes.columns.name'), render: (item) => <span className="font-medium">{item.name}</span> },
    { key: 'currencyCode', header: t('cashBoxes.columns.currency'), render: (item) => item.currencyCode },
    { key: 'initialBalance', header: t('cashBoxes.columns.initialBalance'), className: 'text-right', render: (item) => <span className="text-muted-foreground">{item.initialBalance.toLocaleString()}</span> },
    { key: 'balance', header: t('cashBoxes.columns.balance'), className: 'text-right', render: (item) => <span className="font-medium">{item.balance.toLocaleString()}</span> },
    {
      key: 'isActive',
      header: t('cashBoxes.columns.status'),
      render: (item) => (
        <Badge variant={item.isActive ? 'default' : 'secondary'}>
          {item.isActive ? t('cashBoxes.active') : t('cashBoxes.inactive')}
        </Badge>
      ),
    },
  ], [t])

  const fields: FieldDef<Record<string, unknown>>[] = useMemo(() => [
    { key: 'name', label: t('cashBoxes.fields.name'), type: 'text', required: true, placeholder: t('cashBoxes.fields.namePlaceholder'), maxLength: 100 },
    { key: 'currencyCode', label: t('cashBoxes.fields.currency'), type: 'combobox', required: true, placeholder: t('cashBoxes.fields.searchCurrency'), loadOptions: loadFamilyCurrencyOptions },
    { key: 'initialBalance', label: t('cashBoxes.fields.initialBalance'), type: 'amount', decimalPlaces: 2 },
    { key: 'isActive', label: t('cashBoxes.fields.isActive'), type: 'switch' },
  ], [t])

  const title = (
    <div className="flex items-center">
      <span>{t('cashBoxes.title')}
        <Tooltip>
          <TooltipTrigger>
            <HelpCircle className="inline h-3.5 w-3.5 ml-1.5 align-super text-muted-foreground cursor-help hover:text-foreground transition-colors" />
          </TooltipTrigger>
          <TooltipContent>
            {t('cashBoxes.tooltip')}
          </TooltipContent>
        </Tooltip>
      </span>
    </div>
  )

  return (
    <>
      <ConfigPage<CashBoxDto>
        title={title}
        columns={columns}
        fields={fields}
        rowKey="cashBoxId"
        fetchItems={fetchItems}
        mapItemToForm={(item) => ({
          name: item.name,
          currencyCode: item.currencyCode,
          initialBalance: item.initialBalance,
          isActive: item.isActive,
        })}
        onCreate={(data) => api.post('/cashboxes', { name: data.name, currencyCode: data.currencyCode, initialBalance: Number(data.initialBalance) || 0 })}
        onUpdate={(id, data) => api.put(`/cashboxes/${id}`, {
          name: data.name,
          currencyCode: data.currencyCode,
          initialBalance: Number(data.initialBalance) || 0,
          isActive: data.isActive,
        })}
        onDelete={(id) => api.delete(`/cashboxes/${id}`)}
        defaultValues={{ name: '', currencyCode: 'ARS', initialBalance: 0, isActive: true }}
        newItemLabel={t('cashBoxes.new')}
        createTitle={t('cashBoxes.createTitle')}
        editTitle={t('cashBoxes.editTitle')}
        extraRowActions={(item) => (
          <Button
            variant="ghost"
            size="icon-sm"
            title="Gestionar permisos"
            onClick={() => setPermissionsCashBox(item)}
          >
            <ShieldCheck className="h-3.5 w-3.5" />
          </Button>
        )}
      />

      {permissionsCashBox && (
        <PermissionsDialog
          cashBox={permissionsCashBox}
          onClose={() => setPermissionsCashBox(null)}
        />
      )}
    </>
  )
}
