import { useCallback, useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Switch } from '@/components/ui/switch'
import { Plus, Trash2 } from 'lucide-react'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { SideDrawer } from '@/components/crud/SideDrawer'
import { ConfirmDialog } from '@/components/crud/ConfirmDialog'
import { SearchBar } from '@/components/crud/SearchBar'
import { HelpCircle } from 'lucide-react'
import api from '@/lib/api'
import axios from 'axios'
import { loadCurrencyOptions, clearFamilyCurrencyCache } from '@/lib/currency-options'
import type { FamilyCurrencyDto } from '@/types/api'
import type { FieldDef } from '@/components/crud/types'

function extractError(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data
    if (data?.message) return data.message
  }
  return 'Ocurrió un error inesperado'
}

export default function CurrenciesPage() {
  const { t } = useTranslation()
  const [items, setItems] = useState<FamilyCurrencyDto[]>([])
  const [search, setSearch] = useState('')
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [formValues, setFormValues] = useState<Record<string, unknown>>({ currencyCode: '', isActive: true })
  const [saving, setSaving] = useState(false)
  const [deleteItem, setDeleteItem] = useState<FamilyCurrencyDto | null>(null)
  const [toggling, setToggling] = useState<string | null>(null)

  const load = useCallback(() => {
    api.get<FamilyCurrencyDto[]>('/familycurrencies').then((r) => setItems(r.data))
  }, [])

  useEffect(() => { load() }, [load])

  const filtered = useMemo(() => {
    if (!search.trim()) return items
    const q = search.toLowerCase()
    return items.filter((i) =>
      i.code.toLowerCase().includes(q) || i.name.toLowerCase().includes(q)
    )
  }, [items, search])

  const fields: FieldDef<Record<string, unknown>>[] = useMemo(() => [
    { key: 'currencyCode', label: t('currencies.fields.currency'), type: 'combobox', required: true, placeholder: t('currencies.fields.searchCurrency'), loadOptions: loadCurrencyOptions },
  ], [t])

  const handleCreate = async () => {
    if (!formValues.currencyCode) {
      toast.error(t('errors.fieldRequired', { field: t('currencies.fields.currency') }))
      return
    }
    setSaving(true)
    try {
      await api.post('/familycurrencies', { currencyCode: formValues.currencyCode })
      clearFamilyCurrencyCache()
      toast.success(t('crud.created'))
      setDrawerOpen(false)
      load()
    } catch (err) {
      toast.error(extractError(err))
    } finally {
      setSaving(false)
    }
  }

  const handleToggle = async (item: FamilyCurrencyDto) => {
    setToggling(item.familyCurrencyId)
    try {
      await api.put(`/familycurrencies/${item.familyCurrencyId}`, { isActive: !item.isActive })
      clearFamilyCurrencyCache()
      setItems((prev) => prev.map((i) => i.familyCurrencyId === item.familyCurrencyId ? { ...i, isActive: !i.isActive } : i))
    } catch (err) {
      toast.error(extractError(err))
    } finally {
      setToggling(null)
    }
  }

  const handleDelete = async () => {
    if (!deleteItem) return
    try {
      await api.delete(`/familycurrencies/${deleteItem.familyCurrencyId}`)
      clearFamilyCurrencyCache()
      toast.success(t('crud.deleted'))
      setDeleteItem(null)
      load()
    } catch (err) {
      toast.error(extractError(err))
      setDeleteItem(null)
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-bold">
          {t('currencies.title')}
          <Tooltip>
            <TooltipTrigger>
              <HelpCircle className="inline h-3.5 w-3.5 ml-1.5 align-super text-muted-foreground cursor-help hover:text-foreground transition-colors" />
            </TooltipTrigger>
            <TooltipContent>{t('currencies.tooltip')}</TooltipContent>
          </Tooltip>
        </h1>
        <Button onClick={() => { setFormValues({ currencyCode: '', isActive: true }); setDrawerOpen(true) }}>
          <Plus className="mr-2 h-4 w-4" />
          {t('currencies.new')}
        </Button>
      </div>

      <SearchBar value={search} onChange={setSearch} />

      <Card>
        <CardContent className="p-0 sm:p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('currencies.columns.code')}</TableHead>
                <TableHead>{t('currencies.columns.name')}</TableHead>
                <TableHead>{t('currencies.columns.symbol')}</TableHead>
                <TableHead>{t('currencies.columns.decimals')}</TableHead>
                <TableHead>{t('currencies.columns.status')}</TableHead>
                <TableHead className="w-[80px] text-right">{t('common.actions')}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filtered.map((item) => (
                <TableRow key={item.familyCurrencyId}>
                  <TableCell className="font-medium">{item.code}</TableCell>
                  <TableCell>{item.name}</TableCell>
                  <TableCell>{item.symbol}</TableCell>
                  <TableCell>{item.decimalPlaces}</TableCell>
                  <TableCell>
                    <Switch
                      checked={item.isActive}
                      disabled={toggling === item.familyCurrencyId}
                      onCheckedChange={() => handleToggle(item)}
                    />
                  </TableCell>
                  <TableCell className="text-right">
                    <Button
                      variant="ghost"
                      size="icon-sm"
                      className="text-destructive hover:text-destructive"
                      onClick={() => setDeleteItem(item)}
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
              {filtered.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} className="text-center text-muted-foreground">
                    {t('common.noData')}
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      <SideDrawer
        open={drawerOpen}
        onOpenChange={setDrawerOpen}
        title={t('currencies.createTitle')}
        fields={fields}
        values={formValues}
        onChange={(key, value) => setFormValues((prev) => ({ ...prev, [key]: value }))}
        onSubmit={handleCreate}
        loading={saving}
      />

      <ConfirmDialog
        open={deleteItem !== null}
        onOpenChange={(open) => { if (!open) setDeleteItem(null) }}
        onConfirm={handleDelete}
      />
    </div>
  )
}
