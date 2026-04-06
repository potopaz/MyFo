import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
  SheetFooter,
  SheetClose,
} from '@/components/ui/sheet'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { ConfirmDialog } from '@/components/crud/ConfirmDialog'
import { Plus, Trash2, Pencil } from 'lucide-react'
import api from '@/lib/api'
import axios from 'axios'
import type { StatementPeriodDto, CreditCardDto } from '@/types/api'

function extractError(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data
    if (data?.message) return data.message
  }
  return 'Ocurrio un error inesperado'
}

function isOpen(p: { closedAt: string | null }) {
  return p.closedAt == null
}

function periodStatusBadge(p: { closedAt: string | null; paymentStatus: string }, t: (k: string) => string) {
  if (isOpen(p)) {
    if (p.paymentStatus === 'PartiallyPaid') {
      return (
        <span className="flex gap-1">
          <Badge variant="outline">{t('statements.status.open')}</Badge>
          <Badge variant="secondary">{t('statements.status.partiallyPaid')}</Badge>
        </span>
      )
    }
    return <Badge variant="outline">{t('statements.status.open')}</Badge>
  }
  if (p.paymentStatus === 'FullyPaid') return <Badge variant="default">{t('statements.status.fullyPaid')}</Badge>
  if (p.paymentStatus === 'PartiallyPaid') return <Badge variant="secondary">{t('statements.status.partiallyPaid')}</Badge>
  return <Badge variant="default">{t('statements.status.closed')}</Badge>
}

function fmt(n: number) {
  return n.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatDateDisplay(dateISO: string): string {
  const [year, month, day] = dateISO.split('-')
  return `${day}/${month}/${year}`
}

export default function StatementPeriodsPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()

  const [cards, setCards] = useState<CreditCardDto[]>([])
  const [periods, setPeriods] = useState<StatementPeriodDto[]>([])
  const [loading, setLoading] = useState(false)

  const [filterCardId, setFilterCardId] = useState('_all_')
  const [filterStatus, setFilterStatus] = useState('_all_')

  const [createOpen, setCreateOpen] = useState(false)
  const [createCardId, setCreateCardId] = useState('')
  const [createForm, setCreateForm] = useState({ periodEnd: '', dueDate: '' })
  const [saving, setSaving] = useState(false)

  const [deletePeriodId, setDeletePeriodId] = useState<string | null>(null)

  const loadPeriods = useCallback(async () => {
    setLoading(true)
    try {
      const params = new URLSearchParams()
      if (filterCardId && filterCardId !== '_all_') params.set('creditCardId', filterCardId)
      if (filterStatus && filterStatus !== '_all_') params.set('status', filterStatus)
      const { data } = await api.get<StatementPeriodDto[]>(`/statementperiods?${params}`)
      setPeriods(data)
    } catch {
      toast.error('Error al cargar resumenes')
    } finally {
      setLoading(false)
    }
  }, [filterCardId, filterStatus])

  useEffect(() => {
    api.get<CreditCardDto[]>('/creditcards').then((r) => setCards(r.data))
  }, [])

  useEffect(() => { loadPeriods() }, [loadPeriods])

  const createPeriod = async () => {
    if (!createCardId) { toast.error(t('statements.selectCardFirst')); return }
    if (!createForm.periodEnd || !createForm.dueDate) {
      toast.error(t('statements.toast.datesRequired'))
      return
    }
    setSaving(true)
    try {
      const { data } = await api.post<StatementPeriodDto>('/statementperiods', {
        creditCardId: createCardId,
        periodEnd: createForm.periodEnd,
        dueDate: createForm.dueDate,
      })
      toast.success(t('statements.toast.periodCreated'))
      setCreateOpen(false)
      setCreateForm({ periodEnd: '', dueDate: '' })
      setCreateCardId('')
      navigate(`/statements/${data.statementPeriodId}/edit`)
    } catch (err) {
      toast.error(extractError(err))
    } finally {
      setSaving(false)
    }
  }

  const handleDeletePeriod = async () => {
    if (!deletePeriodId) return
    try {
      await api.delete(`/statementperiods/${deletePeriodId}`)
      toast.success(t('statements.toast.periodDeleted'))
      setDeletePeriodId(null)
      loadPeriods()
    } catch (err) {
      toast.error(extractError(err))
      setDeletePeriodId(null)
    }
  }

  const cardLabel = (id: string) => {
    const c = cards.find((x) => x.creditCardId === id)
    return c ? `${c.name} (${c.currencyCode})` : ''
  }

  const statusLabel = (s: string) => {
    const labels: Record<string, string> = {
      _all_: t('statements.allStatuses'),
      Open: t('statements.status.open'),
      Closed: t('statements.status.closed'),
    }
    return labels[s] ?? t('statements.allStatuses')
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-bold">{t('statements.pageTitle')}</h1>
        <Button onClick={() => {
          setCreateCardId(filterCardId !== '_all_' ? filterCardId : '')
          setCreateForm({ periodEnd: '', dueDate: '' })
          setCreateOpen(true)
        }}>
          <Plus className="mr-2 h-4 w-4" /> {t('statements.newPeriod')}
        </Button>
      </div>

      <Card>
        <CardContent className="flex flex-wrap items-end gap-3 p-3">
          <div className="flex flex-col gap-1">
            <Label className="text-xs">{t('statements.selectCard')}</Label>
            <Select value={filterCardId} onValueChange={setFilterCardId}>
              <SelectTrigger className="h-8 w-52">
                <SelectValue>
                  {filterCardId === '_all_' ? t('statements.allCards') : cardLabel(filterCardId)}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="_all_">{t('statements.allCards')}</SelectItem>
                {cards.map((c) => (
                  <SelectItem key={c.creditCardId} value={c.creditCardId}>
                    {c.name} ({c.currencyCode})
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="flex flex-col gap-1">
            <Label className="text-xs">{t('statements.filterStatus')}</Label>
            <Select value={filterStatus} onValueChange={setFilterStatus}>
              <SelectTrigger className="h-8 w-40">
                <SelectValue>{statusLabel(filterStatus)}</SelectValue>
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="_all_">{t('statements.allStatuses')}</SelectItem>
                <SelectItem value="Open">{t('statements.status.open')}</SelectItem>
                <SelectItem value="Closed">{t('statements.status.closed')}</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {loading ? (
        <p className="py-8 text-center text-muted-foreground">{t('common.loading')}</p>
      ) : periods.length === 0 ? (
        <p className="py-8 text-center text-muted-foreground">{t('statements.empty')}</p>
      ) : (
        <div className="rounded-md border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('statements.columns.card')}</TableHead>
                <TableHead>{t('statements.columns.period')}</TableHead>
                <TableHead>{t('statements.columns.dueDate')}</TableHead>
                <TableHead>{t('statements.columns.status')}</TableHead>
                <TableHead className="text-right">{t('statements.columns.total')}</TableHead>
                <TableHead className="w-24" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {periods.map((p) => (
                <TableRow key={p.statementPeriodId}>
                  <TableCell className="font-medium">{p.creditCardName}</TableCell>
                  <TableCell>{formatDateDisplay(p.periodEnd)}</TableCell>
                  <TableCell>{formatDateDisplay(p.dueDate)}</TableCell>
                  <TableCell>{periodStatusBadge(p, t)}</TableCell>
                  <TableCell className="text-right tabular-nums">
                    {fmt(p.statementTotal)}
                  </TableCell>
                  <TableCell>
                    <div className="flex gap-1">
                      <Button
                        variant="ghost" size="icon" className="h-7 w-7"
                        title={t('common.edit')}
                        onClick={() => navigate(`/statements/${p.statementPeriodId}/edit`)}
                      >
                        <Pencil className="h-3.5 w-3.5" />
                      </Button>
                      {isOpen(p) && (
                        <Button
                          variant="ghost" size="icon" className="h-7 w-7 text-destructive hover:text-destructive"
                          title={t('common.delete')}
                          onClick={() => setDeletePeriodId(p.statementPeriodId)}
                        >
                          <Trash2 className="h-3.5 w-3.5" />
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <Sheet open={createOpen} onOpenChange={setCreateOpen}>
        <SheetContent side="right" className="flex flex-col sm:max-w-md">
          <SheetHeader>
            <SheetTitle>{t('statements.newPeriod')}</SheetTitle>
            <SheetDescription className="sr-only">Crear periodo de resumen</SheetDescription>
          </SheetHeader>
          <div className="flex flex-1 flex-col gap-5 overflow-y-auto px-4">
            <div className="space-y-1.5">
              <Label>{t('statements.selectCard')}</Label>
              <Select value={createCardId} onValueChange={setCreateCardId}>
                <SelectTrigger><SelectValue>
                  {createCardId ? cardLabel(createCardId) : t('statements.selectCardPlaceholder')}
                </SelectValue></SelectTrigger>
                <SelectContent>
                  {cards.filter((c) => c.isActive).map((c) => (
                    <SelectItem key={c.creditCardId} value={c.creditCardId}>
                      {c.name} ({c.currencyCode})
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>{t('statements.periodEnd')}</Label>
              <Input
                type="date"
                value={createForm.periodEnd}
                onChange={(e) => setCreateForm((p) => ({ ...p, periodEnd: e.target.value }))}
              />
            </div>
            <div className="space-y-1.5">
              <Label>{t('statements.dueDate')}</Label>
              <Input
                type="date"
                value={createForm.dueDate}
                onChange={(e) => setCreateForm((p) => ({ ...p, dueDate: e.target.value }))}
              />
            </div>
          </div>
          <SheetFooter className="flex-row gap-2 border-t pt-4">
            <SheetClose render={<Button variant="outline" className="flex-1" />}>
              {t('common.cancel')}
            </SheetClose>
            <Button className="flex-1" disabled={saving} onClick={createPeriod}>
              {saving ? t('common.saving') : t('common.save')}
            </Button>
          </SheetFooter>
        </SheetContent>
      </Sheet>

      <ConfirmDialog
        open={deletePeriodId !== null}
        onOpenChange={(open) => { if (!open) setDeletePeriodId(null) }}
        onConfirm={handleDeletePeriod}
      />
    </div>
  )
}
