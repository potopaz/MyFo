import { useCallback, useEffect, useRef, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import { Checkbox } from '@/components/ui/checkbox'
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
import { ConfirmDialog } from '@/components/crud/ConfirmDialog'
import { AmountInput } from '@/components/ui/amount-input'
import { ArrowLeft, Lock, Unlock, Plus, Trash2 } from 'lucide-react'
import api from '@/lib/api'
import axios from 'axios'
import type {
  StatementPeriodDetailDto,
  StatementInstallmentDto,
} from '@/types/api'

function extractError(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data
    if (data?.message) return data.message
  }
  return 'Ocurrio un error inesperado'
}

function fmt(n: number) {
  return n.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatDateDisplay(dateISO: string): string {
  const [year, month, day] = dateISO.split('-')
  return `${day}/${month}/${year}`
}

function periodStatusBadge(p: { closedAt: string | null; paymentStatus: string }, t: (k: string) => string) {
  const open = p.closedAt == null
  if (open) {
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

export default function StatementPeriodFormPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { id } = useParams<{ id: string }>()

  const [detail, setDetail] = useState<StatementPeriodDetailDto | null>(null)
  const [loading, setLoading] = useState(true)

  const [editPeriodEnd, setEditPeriodEnd] = useState('')
  const [editDueDate, setEditDueDate] = useState('')

  const [lineItemOpen, setLineItemOpen] = useState(false)
  const [lineItemForm, setLineItemForm] = useState({ lineType: 'Charge', description: '', amount: 0 })
  const [savingLineItem, setSavingLineItem] = useState(false)

  const [deleteLineItemId, setDeleteLineItemId] = useState<string | null>(null)

  // Editing amounts inline
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editingType, setEditingType] = useState<'actual' | 'bonification'>('actual')
  const [editAmountValue, setEditAmountValue] = useState<number | null>(null)

  const togglingRef = useRef(false)

  const loadDetail = useCallback(async (periodId: string) => {
    try {
      const { data } = await api.get<StatementPeriodDetailDto>(`/statementperiods/${periodId}`)
      setDetail(data)
      setEditPeriodEnd(data.periodEnd)
      setEditDueDate(data.dueDate)
    } catch {
      toast.error('Error al cargar detalle')
      navigate('/statements')
    } finally {
      setLoading(false)
    }
  }, [navigate])

  useEffect(() => {
    if (id) loadDetail(id)
  }, [id, loadDetail])

  const periodIsOpen = detail ? detail.closedAt == null : false

  // ── Update dates ──
  const saveDates = async () => {
    if (!detail) return
    try {
      await api.patch(`/statementperiods/${detail.statementPeriodId}/dates`, {
        periodEnd: editPeriodEnd,
        dueDate: editDueDate,
      })
      toast.success(t('statements.toast.datesUpdated'))
      loadDetail(detail.statementPeriodId)
    } catch (err) {
      toast.error(extractError(err))
    }
  }

  // ── Close / Reopen ──
  const closePeriod = async () => {
    if (!detail) return
    try {
      await api.post(`/statementperiods/${detail.statementPeriodId}/close`)
      toast.success(t('statements.toast.periodClosed'))
      loadDetail(detail.statementPeriodId)
    } catch (err) {
      toast.error(extractError(err))
    }
  }

  const reopenPeriod = async () => {
    if (!detail) return
    try {
      await api.post(`/statementperiods/${detail.statementPeriodId}/reopen`)
      toast.success(t('statements.toast.periodReopened'))
      loadDetail(detail.statementPeriodId)
    } catch (err) {
      toast.error(extractError(err))
    }
  }

  // ── Toggle installment inclusion ──
  const toggleInclusion = async (inst: StatementInstallmentDto) => {
    if (!detail || togglingRef.current) return
    togglingRef.current = true
    try {
      await api.post(
        `/statementperiods/${detail.statementPeriodId}/installments/${inst.creditCardInstallmentId}/toggle-inclusion`,
        { include: !inst.isIncluded }
      )
      await loadDetail(detail.statementPeriodId)
    } catch (err) {
      toast.error(extractError(err))
    } finally {
      togglingRef.current = false
    }
  }

  // ── Toggle bonification inclusion ──
  const toggleBonification = async (inst: StatementInstallmentDto) => {
    if (!detail || togglingRef.current) return
    togglingRef.current = true
    try {
      await api.post(
        `/statementperiods/${detail.statementPeriodId}/installments/${inst.creditCardInstallmentId}/toggle-bonification`,
        { include: !inst.isBonificationIncluded }
      )
      await loadDetail(detail.statementPeriodId)
    } catch (err) {
      toast.error(extractError(err))
    } finally {
      togglingRef.current = false
    }
  }

  // ── Line items ──
  const addLineItem = async () => {
    if (!detail) return
    if (!lineItemForm.description.trim()) { toast.error(t('statements.toast.descriptionRequired')); return }
    if (lineItemForm.amount <= 0) { toast.error(t('statements.toast.amountRequired')); return }
    setSavingLineItem(true)
    try {
      await api.post(`/statementperiods/${detail.statementPeriodId}/line-items`, lineItemForm)
      toast.success(t('statements.toast.lineItemAdded'))
      setLineItemOpen(false)
      loadDetail(detail.statementPeriodId)
    } catch (err) {
      toast.error(extractError(err))
    } finally {
      setSavingLineItem(false)
    }
  }

  const handleDeleteLineItem = async () => {
    if (!deleteLineItemId || !detail) return
    try {
      await api.delete(`/statementperiods/line-items/${deleteLineItemId}`)
      toast.success(t('statements.toast.lineItemDeleted'))
      setDeleteLineItemId(null)
      loadDetail(detail.statementPeriodId)
    } catch (err) {
      toast.error(extractError(err))
      setDeleteLineItemId(null)
    }
  }

  // ── Save inline amount ──
  const saveInlineAmount = async () => {
    if (!editingId || !detail) return
    try {
      if (editingType === 'actual') {
        await api.patch(
          `/statementperiods/installments/${editingId}/actual-amount`,
          { actualAmount: editAmountValue }
        )
      } else {
        await api.patch(
          `/statementperiods/installments/${editingId}/bonification-amount`,
          { actualBonificationAmount: editAmountValue }
        )
      }
      toast.success(t('statements.toast.installmentUpdated'))
      setEditingId(null)
      loadDetail(detail.statementPeriodId)
    } catch (err) {
      toast.error(extractError(err))
    }
  }

  const startEditing = (instId: string, type: 'actual' | 'bonification', currentValue: number | null) => {
    if (!periodIsOpen) return
    setEditingId(instId)
    setEditingType(type)
    setEditAmountValue(currentValue)
  }

  if (loading) {
    return <p className="py-8 text-center text-muted-foreground">{t('common.loading')}</p>
  }

  if (!detail) return null

  // Build display rows: each installment gets one row, + bonification row if applicable
  type DisplayRow = {
    key: string
    type: 'installment' | 'bonification'
    inst: StatementInstallmentDto
  }
  const displayRows: DisplayRow[] = []
  for (const inst of detail.installments) {
    displayRows.push({ key: inst.creditCardInstallmentId, type: 'installment', inst })
    if (inst.bonificationApplied > 0) {
      displayRows.push({ key: `${inst.creditCardInstallmentId}-bonif`, type: 'bonification', inst })
    }
  }

  return (
    <div className="mx-auto max-w-4xl space-y-6 pb-10">
      {/* Header */}
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" onClick={() => navigate('/statements')}>
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <h1 className="text-2xl font-bold">
          {t('statements.detailTitle', { card: detail.creditCardName })}
        </h1>
        {periodStatusBadge(detail, t)}
      </div>

      {/* Period info card */}
      <Card>
        <CardContent className="p-4 space-y-4">
          <div className="grid grid-cols-3 gap-4">
            <div className="space-y-1">
              <Label className="text-xs text-muted-foreground">{t('statements.periodStart')}</Label>
              <div className="text-sm font-medium">{formatDateDisplay(detail.periodStart)}</div>
            </div>
            <div className="space-y-1">
              <Label className="text-xs text-muted-foreground">{t('statements.periodEnd')}</Label>
              {periodIsOpen ? (
                <Input
                  type="date"
                  className="h-8 text-sm"
                  value={editPeriodEnd}
                  onChange={(e) => setEditPeriodEnd(e.target.value)}
                  onBlur={() => { if (editPeriodEnd !== detail.periodEnd) saveDates() }}
                />
              ) : (
                <div className="text-sm font-medium">{formatDateDisplay(detail.periodEnd)}</div>
              )}
            </div>
            <div className="space-y-1">
              <Label className="text-xs text-muted-foreground">{t('statements.dueDate')}</Label>
              {periodIsOpen ? (
                <Input
                  type="date"
                  className="h-8 text-sm"
                  value={editDueDate}
                  onChange={(e) => setEditDueDate(e.target.value)}
                  onBlur={() => { if (editDueDate !== detail.dueDate) saveDates() }}
                />
              ) : (
                <div className="text-sm font-medium">{formatDateDisplay(detail.dueDate)}</div>
              )}
            </div>
          </div>

          <div className="grid grid-cols-3 gap-x-4 gap-y-1 text-sm border-t pt-3">
            <div>{t('statements.previousBalance')}: <strong>{fmt(detail.previousBalance)}</strong></div>
            <div>{t('statements.installmentsTotal')}: <strong>{fmt(detail.installmentsTotal)}</strong></div>
            <div>{t('statements.chargesTotal')}: <strong>{fmt(detail.chargesTotal)}</strong></div>
            <div>{t('statements.bonificationsTotal')}: <strong>-{fmt(detail.bonificationsTotal)}</strong></div>
            <div className="col-span-2">
              {t('statements.statementTotal')}: <strong className="text-lg">{fmt(detail.statementTotal)}</strong>
            </div>
          </div>

          <div className="flex gap-2 border-t pt-3">
            {periodIsOpen && (
              <Button onClick={closePeriod}>
                <Lock className="mr-2 h-4 w-4" /> {t('statements.closePeriod')}
              </Button>
            )}
            {!periodIsOpen && detail.paymentStatus === 'Unpaid' && (
              <Button variant="outline" onClick={reopenPeriod}>
                <Unlock className="mr-2 h-4 w-4" /> {t('statements.reopenPeriod')}
              </Button>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Installments */}
      <Card>
        <CardContent className="p-4">
          <h3 className="text-sm font-semibold mb-3">
            {t('statements.installments')} ({detail.installments.length})
          </h3>
          {detail.installments.length === 0 ? (
            <p className="text-sm text-muted-foreground">{t('statements.noInstallments')}</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b text-left">
                    {periodIsOpen && <th className="py-2 pr-2 w-8" />}
                    <th className="py-2 pr-3">{t('common.date')}</th>
                    <th className="py-2 pr-3">{t('statements.installmentCols.description')}</th>
                    <th className="py-2 pr-3">{t('statements.installmentCols.installment')}</th>
                    <th className="py-2 pr-3 text-right">{t('statements.installmentCols.projected')}</th>
                    <th className="py-2 pr-3 text-right">{t('statements.installmentCols.actual')}</th>
                  </tr>
                </thead>
                <tbody>
                  {displayRows.map((row) => {
                    if (row.type === 'installment') {
                      const inst = row.inst
                      const included = inst.isIncluded
                      const isEditingThis = editingId === inst.creditCardInstallmentId && editingType === 'actual'
                      return (
                        <tr
                          key={row.key}
                          className={`border-b hover:bg-muted/50 ${!included && periodIsOpen ? 'opacity-40' : ''}`}
                        >
                          {periodIsOpen && (
                            <td className="py-2 pr-2">
                              <Checkbox
                                checked={included}
                                onCheckedChange={() => toggleInclusion(inst)}
                              />
                            </td>
                          )}
                          <td className="py-2 pr-3 whitespace-nowrap">
                            {inst.movementDate ? formatDateDisplay(inst.movementDate) : ''}
                          </td>
                          <td className="py-2 pr-3">
                            {inst.movementDescription || t('common.noData')}
                          </td>
                          <td className="py-2 pr-3">{inst.installmentNumber}/{inst.totalInstallments}</td>
                          <td className="py-2 pr-3 text-right tabular-nums">{fmt(inst.projectedAmount)}</td>
                          <td className="py-2 pr-3 text-right tabular-nums">
                            {isEditingThis ? (
                              <div className="flex items-center gap-1 justify-end">
                                <Input
                                  className="w-24 h-7 text-right text-sm"
                                  type="number"
                                  value={editAmountValue ?? ''}
                                  onChange={(e) => setEditAmountValue(e.target.value ? Number(e.target.value) : null)}
                                />
                                <Button size="sm" className="h-7 text-xs" onClick={saveInlineAmount}>OK</Button>
                                <Button size="sm" variant="ghost" className="h-7 text-xs" onClick={() => setEditingId(null)}>X</Button>
                              </div>
                            ) : (
                              <span
                                className={periodIsOpen && included ? 'cursor-pointer underline decoration-dotted' : ''}
                                onClick={() => {
                                  if (periodIsOpen && included) startEditing(inst.creditCardInstallmentId, 'actual', inst.actualAmount)
                                }}
                              >
                                {inst.actualAmount != null ? fmt(inst.actualAmount) : '—'}
                              </span>
                            )}
                          </td>
                        </tr>
                      )
                    }

                    // Bonification row
                    const inst = row.inst
                    const bonifIncluded = inst.isBonificationIncluded
                    const parentIncluded = inst.isIncluded
                    return (
                      <tr
                        key={row.key}
                        className={`border-b hover:bg-muted/50 text-green-700 dark:text-green-400 ${!bonifIncluded && periodIsOpen ? 'opacity-40' : ''}`}
                      >
                        {periodIsOpen && (
                          <td className="py-1.5 pr-2 pl-4">
                            <Checkbox
                              checked={bonifIncluded}
                              disabled={!parentIncluded}
                              onCheckedChange={() => toggleBonification(inst)}
                            />
                          </td>
                        )}
                        <td className="py-1.5 pr-3" />
                        <td className="py-1.5 pr-3 text-xs italic pl-4">
                          {t('statements.bonification')}
                        </td>
                        <td className="py-1.5 pr-3" />
                        <td className="py-1.5 pr-3 text-right tabular-nums">-{fmt(inst.bonificationApplied)}</td>
                        <td className="py-1.5 pr-3 text-right tabular-nums">
                          {(() => {
                            const isEditingBonif = editingId === inst.creditCardInstallmentId && editingType === 'bonification'
                            if (isEditingBonif) {
                              return (
                                <div className="flex items-center gap-1 justify-end">
                                  <Input
                                    className="w-24 h-7 text-right text-sm"
                                    type="number"
                                    value={editAmountValue ?? ''}
                                    onChange={(e) => setEditAmountValue(e.target.value ? Number(e.target.value) : null)}
                                  />
                                  <Button size="sm" className="h-7 text-xs" onClick={saveInlineAmount}>OK</Button>
                                  <Button size="sm" variant="ghost" className="h-7 text-xs" onClick={() => setEditingId(null)}>X</Button>
                                </div>
                              )
                            }
                            if (!bonifIncluded) return '—'
                            const val = inst.actualBonificationAmount ?? inst.bonificationApplied
                            return (
                              <span
                                className={periodIsOpen ? 'cursor-pointer underline decoration-dotted' : ''}
                                onClick={() => {
                                  if (periodIsOpen) startEditing(inst.creditCardInstallmentId, 'bonification', inst.actualBonificationAmount)
                                }}
                              >
                                -{fmt(val)}
                              </span>
                            )
                          })()}
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Line Items */}
      <Card>
        <CardContent className="p-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="text-sm font-semibold">{t('statements.lineItems')}</h3>
            {periodIsOpen && (
              <Button size="sm" variant="outline" onClick={() => {
                setLineItemForm({ lineType: 'Charge', description: '', amount: 0 })
                setLineItemOpen(true)
              }}>
                <Plus className="mr-1 h-3 w-3" /> {t('statements.addLineItem')}
              </Button>
            )}
          </div>
          {detail.lineItems.length === 0 ? (
            <p className="text-sm text-muted-foreground">{t('statements.noLineItems')}</p>
          ) : (
            <div className="space-y-1">
              {detail.lineItems.map((li) => (
                <div key={li.statementLineItemId} className="flex items-center gap-2 rounded px-2 py-1.5 text-sm hover:bg-muted/50">
                  <Badge variant={li.lineType === 'Charge' ? 'destructive' : 'default'} className="text-xs">
                    {li.lineType === 'Charge' ? t('statements.charge') : t('statements.bonification')}
                  </Badge>
                  <span>{li.description}</span>
                  <div className="flex-1" />
                  <span className={li.lineType === 'Bonification' ? 'text-green-600' : 'text-destructive'}>
                    {li.lineType === 'Bonification' ? '-' : ''}{fmt(li.amount)}
                  </span>
                  {periodIsOpen && (
                    <Button
                      variant="ghost" size="icon" className="h-6 w-6 text-destructive hover:text-destructive"
                      onClick={() => setDeleteLineItemId(li.statementLineItemId)}
                    >
                      <Trash2 className="h-3 w-3" />
                    </Button>
                  )}
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Line item drawer */}
      <Sheet open={lineItemOpen} onOpenChange={setLineItemOpen}>
        <SheetContent side="right" className="flex flex-col sm:max-w-md">
          <SheetHeader>
            <SheetTitle>{t('statements.addLineItem')}</SheetTitle>
            <SheetDescription className="sr-only">Agregar cargo o bonificacion</SheetDescription>
          </SheetHeader>
          <div className="flex flex-1 flex-col gap-5 overflow-y-auto px-4">
            <div className="space-y-1.5">
              <Label>{t('statements.lineType')}</Label>
              <Select value={lineItemForm.lineType} onValueChange={(v) => setLineItemForm((p) => ({ ...p, lineType: v }))}>
                <SelectTrigger><SelectValue>
                  {lineItemForm.lineType === 'Charge' ? t('statements.charge') : t('statements.bonification')}
                </SelectValue></SelectTrigger>
                <SelectContent>
                  <SelectItem value="Charge">{t('statements.charge')}</SelectItem>
                  <SelectItem value="Bonification">{t('statements.bonification')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>{t('statements.description')}</Label>
              <Input
                value={lineItemForm.description}
                onChange={(e) => setLineItemForm((p) => ({ ...p, description: e.target.value }))}
                placeholder={t('statements.descriptionPlaceholder')}
                maxLength={200}
              />
            </div>
            <div className="space-y-1.5">
              <Label>{t('statements.amount')}</Label>
              <AmountInput
                value={lineItemForm.amount}
                onChange={(v) => setLineItemForm((p) => ({ ...p, amount: v }))}
                decimalPlaces={2}
              />
            </div>
          </div>
          <SheetFooter className="flex-row gap-2 border-t pt-4">
            <SheetClose render={<Button variant="outline" className="flex-1" />}>
              {t('common.cancel')}
            </SheetClose>
            <Button className="flex-1" disabled={savingLineItem} onClick={addLineItem}>
              {savingLineItem ? t('common.saving') : t('common.save')}
            </Button>
          </SheetFooter>
        </SheetContent>
      </Sheet>

      <ConfirmDialog
        open={deleteLineItemId !== null}
        onOpenChange={(open) => { if (!open) setDeleteLineItemId(null) }}
        onConfirm={handleDeleteLineItem}
      />
    </div>
  )
}
