import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
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
import { AmountInput } from '@/components/ui/amount-input'
import { Checkbox } from '@/components/ui/checkbox'
import { Pencil, Plus, Trash2 } from 'lucide-react'
import api from '@/lib/api'
import axios from 'axios'
import { loadCashBoxOptions, loadBankAccountOptions, type PaymentEntityOption } from '@/lib/payment-entities'
import type { CreditCardPaymentDto, CreditCardDto, FamilySettingsDto, StatementPeriodDto } from '@/types/api'

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

export default function CreditCardPaymentsPage() {
  const { t } = useTranslation()

  const [cards, setCards] = useState<CreditCardDto[]>([])
  const [payments, setPayments] = useState<CreditCardPaymentDto[]>([])
  const [loading, setLoading] = useState(false)

  // Filters
  const [filterCardId, setFilterCardId] = useState('_all_')

  // Create form
  const [createOpen, setCreateOpen] = useState(false)
  const [saving, setSaving] = useState(false)
  const [form, setForm] = useState({
    creditCardId: '',
    paymentDate: '',
    amount: '',
    description: '',
    paymentSource: 'CashBox' as 'CashBox' | 'BankAccount',
    cashBoxId: '',
    bankAccountId: '',
    isTotalPayment: false,
    statementPeriodId: '',
    primaryExchangeRate: '1',
    secondaryExchangeRate: '1',
  })
  const [cashBoxes, setCashBoxes] = useState<PaymentEntityOption[]>([])
  const [bankAccounts, setBankAccounts] = useState<PaymentEntityOption[]>([])
  const [familySettings, setFamilySettings] = useState<FamilySettingsDto | null>(null)
  const [periods, setPeriods] = useState<StatementPeriodDto[]>([])

  // Edit
  const [editingPaymentId, setEditingPaymentId] = useState<string | null>(null)

  // Delete
  const [deleteId, setDeleteId] = useState<string | null>(null)

  const loadPayments = useCallback(async () => {
    setLoading(true)
    try {
      const params = new URLSearchParams()
      if (filterCardId && filterCardId !== '_all_') params.set('creditCardId', filterCardId)
      const { data } = await api.get<CreditCardPaymentDto[]>(`/creditcardpayments?${params}`)
      setPayments(data)
    } catch {
      toast.error('Error al cargar pagos')
    } finally {
      setLoading(false)
    }
  }, [filterCardId])

  useEffect(() => {
    api.get<CreditCardDto[]>('/creditcards').then((r) => setCards(r.data))
  }, [])

  useEffect(() => { loadPayments() }, [loadPayments])

  // Load family settings + payment sources when drawer opens
  useEffect(() => {
    if (!createOpen) return
    api.get<FamilySettingsDto>('/family-settings').then((r) => setFamilySettings(r.data))
    loadCashBoxOptions().then(setCashBoxes)
    loadBankAccountOptions().then(setBankAccounts)
  }, [createOpen])

  // Load eligible periods when card changes
  useEffect(() => {
    if (!createOpen || !form.creditCardId) {
      setPeriods([])
      return
    }
    api.get<StatementPeriodDto[]>('/statementperiods', {
      params: { creditCardId: form.creditCardId },
    }).then((r) => {
      // Show closed periods that are not fully paid
      setPeriods(r.data.filter((p) => p.closedAt != null && p.paymentStatus !== 'FullyPaid'))
    }).catch(() => setPeriods([]))
  }, [createOpen, form.creditCardId])

  // Auto-fetch exchange rates when card or date changes
  useEffect(() => {
    if (!createOpen || !form.creditCardId || !form.paymentDate || !familySettings) return
    const card = cards.find((c) => c.creditCardId === form.creditCardId)
    if (!card) return
    const cur = card.currencyCode
    const pCurr = familySettings.primaryCurrencyCode
    const sCurr = familySettings.secondaryCurrencyCode
    if (!pCurr || !sCurr || pCurr === sCurr) return

    const fetchRates = async () => {
      const updates: Record<string, string> = {}
      if (cur !== pCurr) {
        try {
          const { data } = await api.get<{ rate: number }>('/exchange-rates', {
            params: { base_currency: cur, target_currency: pCurr, date: form.paymentDate },
          })
          updates.primaryExchangeRate = String(data.rate)
        } catch { /* silent */ }
      }
      if (cur !== sCurr) {
        try {
          const { data } = await api.get<{ rate: number }>('/exchange-rates', {
            params: { base_currency: cur, target_currency: sCurr, date: form.paymentDate },
          })
          updates.secondaryExchangeRate = String(data.rate)
        } catch { /* silent */ }
      }
      if (Object.keys(updates).length > 0) {
        setForm((p) => ({ ...p, ...updates }))
      }
    }
    fetchRates()
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [createOpen, form.creditCardId, form.paymentDate, familySettings])

  const openEdit = (p: CreditCardPaymentDto) => {
    setEditingPaymentId(p.creditCardPaymentId)
    setForm({
      creditCardId: p.creditCardId,
      paymentDate: p.paymentDate,
      amount: String(p.amount),
      description: p.description ?? '',
      paymentSource: p.cashBoxId ? 'CashBox' : 'BankAccount',
      cashBoxId: p.cashBoxId ?? '',
      bankAccountId: p.bankAccountId ?? '',
      isTotalPayment: p.isTotalPayment,
      statementPeriodId: p.statementPeriodId ?? '',
      primaryExchangeRate: String(p.primaryExchangeRate),
      secondaryExchangeRate: String(p.secondaryExchangeRate),
    })
    setCreateOpen(true)
  }

  const savePayment = async () => {
    if (!form.creditCardId) { toast.error(t('ccPayments.toast.cardRequired')); return }
    if (!form.paymentDate) { toast.error(t('ccPayments.toast.dateRequired')); return }
    const amount = parseFloat(form.amount)
    if (!amount || amount <= 0) { toast.error(t('ccPayments.toast.amountRequired')); return }
    const sourceId = form.paymentSource === 'CashBox' ? form.cashBoxId : form.bankAccountId
    if (!sourceId) { toast.error(t('ccPayments.toast.sourceRequired')); return }
    if (form.isTotalPayment && !form.statementPeriodId) { toast.error(t('ccPayments.toast.periodRequiredForTotal')); return }

    const body = {
      creditCardId: form.creditCardId,
      paymentDate: form.paymentDate,
      amount,
      description: form.description.trim() || null,
      cashBoxId: form.paymentSource === 'CashBox' ? form.cashBoxId : null,
      bankAccountId: form.paymentSource === 'BankAccount' ? form.bankAccountId : null,
      isTotalPayment: form.isTotalPayment,
      statementPeriodId: form.statementPeriodId || null,
      primaryExchangeRate: parseFloat(form.primaryExchangeRate) || 1,
      secondaryExchangeRate: parseFloat(form.secondaryExchangeRate) || 1,
    }

    setSaving(true)
    try {
      if (editingPaymentId) {
        await api.put(`/creditcardpayments/${editingPaymentId}`, body)
        toast.success(t('ccPayments.toast.updated'))
      } else {
        await api.post('/creditcardpayments', body)
        toast.success(t('ccPayments.toast.created'))
      }
      setCreateOpen(false)
      setEditingPaymentId(null)
      loadPayments()
    } catch (err) {
      toast.error(extractError(err))
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async () => {
    if (!deleteId) return
    try {
      await api.delete(`/creditcardpayments/${deleteId}`)
      toast.success(t('ccPayments.toast.deleted'))
      setDeleteId(null)
      loadPayments()
    } catch (err) {
      toast.error(extractError(err))
      setDeleteId(null)
    }
  }

  const cardLabel = (id: string) => {
    const c = cards.find((x) => x.creditCardId === id)
    return c ? `${c.name} (${c.currencyCode})` : ''
  }

  // Derived: selected card currency
  const selectedCard = cards.find((c) => c.creditCardId === form.creditCardId)
  const cardCurrency = selectedCard?.currencyCode ?? ''

  // Filter sources by card currency + canOperate (for cash boxes)
  const filteredCashBoxes = cashBoxes.filter((cb) => cb.currencyCode === cardCurrency && (cb.isActive !== false || cb.value === form.cashBoxId))
  const filteredBankAccounts = bankAccounts.filter((ba) => ba.currencyCode === cardCurrency && (ba.isActive !== false || ba.value === form.bankAccountId))

  // Exchange rate logic (same as MovementFormPage)
  const primaryCurrency = familySettings?.primaryCurrencyCode ?? ''
  const secondaryCurrency = familySettings?.secondaryCurrencyCode ?? ''
  const isBimonetary = !!primaryCurrency && !!secondaryCurrency && primaryCurrency !== secondaryCurrency

  const getExchangeRateState = () => {
    if (!isBimonetary || !cardCurrency) return { show: false, lockPrimary: false, lockSecondary: false }
    if (cardCurrency === primaryCurrency) return { show: true, lockPrimary: true, lockSecondary: false }
    if (cardCurrency === secondaryCurrency) return { show: true, lockPrimary: false, lockSecondary: true }
    return { show: true, lockPrimary: false, lockSecondary: false }
  }
  const erState = getExchangeRateState()

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-bold">{t('ccPayments.pageTitle')}</h1>
        <Button onClick={() => {
          setEditingPaymentId(null)
          setForm({
            creditCardId: filterCardId !== '_all_' ? filterCardId : '',
            paymentDate: new Date().toISOString().slice(0, 10), amount: '', description: '',
            paymentSource: 'CashBox', cashBoxId: '', bankAccountId: '',
            isTotalPayment: false, statementPeriodId: '',
            primaryExchangeRate: '1', secondaryExchangeRate: '1',
          })
          setCreateOpen(true)
        }}>
          <Plus className="mr-2 h-4 w-4" /> {t('ccPayments.new')}
        </Button>
      </div>

      {/* Filters */}
      <Card>
        <CardContent className="flex flex-wrap items-end gap-3 p-3">
          <div className="flex flex-col gap-1">
            <Label className="text-xs">{t('ccPayments.selectCard')}</Label>
            <Select value={filterCardId} onValueChange={setFilterCardId}>
              <SelectTrigger className="h-8 w-52">
                <SelectValue>
                  {filterCardId === '_all_' ? t('ccPayments.allCards') : cardLabel(filterCardId)}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="_all_">{t('ccPayments.allCards')}</SelectItem>
                {cards.map((c) => (
                  <SelectItem key={c.creditCardId} value={c.creditCardId}>
                    {c.name} ({c.currencyCode})
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Table */}
      {loading ? (
        <p className="py-8 text-center text-muted-foreground">{t('common.loading')}</p>
      ) : payments.length === 0 ? (
        <p className="py-8 text-center text-muted-foreground">{t('ccPayments.empty')}</p>
      ) : (
        <div className="rounded-md border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('ccPayments.columns.date')}</TableHead>
                <TableHead>{t('ccPayments.columns.card')}</TableHead>
                <TableHead>{t('ccPayments.columns.description')}</TableHead>
                <TableHead>{t('ccPayments.columns.source')}</TableHead>
                <TableHead>{t('ccPayments.columns.type')}</TableHead>
                <TableHead className="text-right">{t('ccPayments.columns.amount')}</TableHead>
                <TableHead className="w-16" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {payments.map((p) => (
                <TableRow key={p.creditCardPaymentId}>
                  <TableCell>{formatDateDisplay(p.paymentDate)}</TableCell>
                  <TableCell className="font-medium">{p.creditCardName}</TableCell>
                  <TableCell className="max-w-[200px] truncate text-muted-foreground">{p.description ?? '—'}</TableCell>
                  <TableCell>{p.cashBoxName ?? p.bankAccountName ?? '—'}</TableCell>
                  <TableCell>{p.isTotalPayment ? t('ccPayments.totalPayment') : t('ccPayments.partialPayment')}</TableCell>
                  <TableCell className="text-right tabular-nums font-medium">{fmt(p.amount)}</TableCell>
                  <TableCell>
                    <div className="flex gap-0.5">
                      <Button
                        variant="ghost" size="icon" className="h-7 w-7"
                        onClick={() => openEdit(p)}
                      >
                        <Pencil className="h-3.5 w-3.5" />
                      </Button>
                      <Button
                        variant="ghost" size="icon" className="h-7 w-7 text-destructive hover:text-destructive"
                        onClick={() => setDeleteId(p.creditCardPaymentId)}
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {/* Create drawer */}
      <Sheet open={createOpen} onOpenChange={(open) => {
        setCreateOpen(open)
        if (!open) setEditingPaymentId(null)
      }}>
        <SheetContent side="right" className="flex flex-col sm:max-w-md">
          <SheetHeader>
            <SheetTitle>{editingPaymentId ? t('ccPayments.edit') : t('ccPayments.new')}</SheetTitle>
            <SheetDescription className="sr-only">{editingPaymentId ? 'Editar' : 'Registrar'} pago de tarjeta de credito</SheetDescription>
          </SheetHeader>
          <div className="flex flex-1 flex-col gap-5 overflow-y-auto px-4">
            <div className="space-y-1.5">
              <Label>{t('ccPayments.form.creditCard')}</Label>
              <Select value={form.creditCardId} disabled={!!editingPaymentId} onValueChange={(v) => {
                const card = cards.find((c) => c.creditCardId === v)
                const cur = card?.currencyCode ?? ''
                setForm((p) => ({
                  ...p,
                  creditCardId: v,
                  cashBoxId: '', bankAccountId: '', statementPeriodId: '',
                  primaryExchangeRate: cur === primaryCurrency ? '1' : p.primaryExchangeRate,
                  secondaryExchangeRate: cur === secondaryCurrency ? '1' : p.secondaryExchangeRate,
                }))
              }}>
                <SelectTrigger><SelectValue>
                  {form.creditCardId ? cardLabel(form.creditCardId) : t('ccPayments.form.selectCard')}
                </SelectValue></SelectTrigger>
                <SelectContent>
                  {cards.filter((c) => c.isActive || c.creditCardId === form.creditCardId).map((c) => (
                    <SelectItem key={c.creditCardId} value={c.creditCardId}>
                      {c.name} ({c.currencyCode})
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>{t('ccPayments.form.paymentDate')}</Label>
              <Input
                type="date"
                value={form.paymentDate}
                onChange={(e) => setForm((p) => ({ ...p, paymentDate: e.target.value }))}
              />
            </div>
            <div className="space-y-1.5">
              <Label>{t('ccPayments.form.description')}</Label>
              <Input
                value={form.description}
                onChange={(e) => setForm((p) => ({ ...p, description: e.target.value }))}
                placeholder={t('ccPayments.form.descriptionPlaceholder')}
                maxLength={200}
              />
            </div>
            <div className="flex items-center gap-2">
              <Checkbox
                id="isTotalPayment"
                checked={form.isTotalPayment}
                onCheckedChange={(v) => {
                  const isTotal = !!v
                  setForm((p) => {
                    const selectedPeriod = periods.find((sp) => sp.statementPeriodId === p.statementPeriodId)
                    return {
                      ...p,
                      isTotalPayment: isTotal,
                      amount: isTotal && selectedPeriod ? String(selectedPeriod.pendingBalance) : p.amount,
                    }
                  })
                }}
              />
              <Label htmlFor="isTotalPayment">{t('ccPayments.form.isTotalPayment')}</Label>
            </div>
            {form.creditCardId && (
              <div className="space-y-1.5">
                <Label>{t('ccPayments.form.statementPeriod')}</Label>
                <Select
                  value={form.statementPeriodId}
                  onValueChange={(v) => {
                    const selectedId = v === '_none_' ? '' : v
                    const selectedPeriod = periods.find((p) => p.statementPeriodId === selectedId)
                    setForm((p) => ({
                      ...p,
                      statementPeriodId: selectedId,
                      amount: p.isTotalPayment && selectedPeriod
                        ? String(selectedPeriod.pendingBalance)
                        : p.amount,
                    }))
                  }}
                >
                  <SelectTrigger><SelectValue>
                    {form.statementPeriodId
                      ? (() => {
                          const sp = periods.find((p) => p.statementPeriodId === form.statementPeriodId)
                          return sp ? `${formatDateDisplay(sp.periodStart)} - ${formatDateDisplay(sp.periodEnd)} (${fmt(sp.pendingBalance)})` : t('ccPayments.form.selectPeriod')
                        })()
                      : t('ccPayments.form.selectPeriod')}
                  </SelectValue></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="_none_">{t('ccPayments.form.selectPeriod')}</SelectItem>
                    {periods.map((sp) => (
                      <SelectItem key={sp.statementPeriodId} value={sp.statementPeriodId}>
                        {formatDateDisplay(sp.periodStart)} - {formatDateDisplay(sp.periodEnd)} ({fmt(sp.pendingBalance)})
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {periods.length === 0 && (
                  <p className="text-xs text-muted-foreground">{t('ccPayments.form.noPeriods')}</p>
                )}
              </div>
            )}
            <div className="space-y-1.5">
              <Label>{t('ccPayments.form.paymentSource')}</Label>
              <Select
                value={form.paymentSource}
                onValueChange={(v) => setForm((p) => ({
                  ...p,
                  paymentSource: v as 'CashBox' | 'BankAccount',
                  cashBoxId: '', bankAccountId: '',
                }))}
              >
                <SelectTrigger><SelectValue>
                  {form.paymentSource === 'CashBox' ? t('ccPayments.form.fromCashBox') : t('ccPayments.form.fromBank')}
                </SelectValue></SelectTrigger>
                <SelectContent>
                  <SelectItem value="CashBox">{t('ccPayments.form.fromCashBox')}</SelectItem>
                  <SelectItem value="BankAccount">{t('ccPayments.form.fromBank')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
            {form.paymentSource === 'CashBox' && (
              <div className="space-y-1.5">
                <Label>{t('ccPayments.form.selectCashBox')}</Label>
                <Select value={form.cashBoxId} onValueChange={(v) => setForm((p) => ({ ...p, cashBoxId: v }))}>
                  <SelectTrigger><SelectValue>
                    {filteredCashBoxes.find((cb) => cb.value === form.cashBoxId)?.label ?? t('ccPayments.form.selectCashBox')}
                  </SelectValue></SelectTrigger>
                  <SelectContent>
                    {filteredCashBoxes.map((cb) => (
                      <SelectItem key={cb.value} value={cb.value}>{cb.label}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}
            {form.paymentSource === 'BankAccount' && (
              <div className="space-y-1.5">
                <Label>{t('ccPayments.form.selectBank')}</Label>
                <Select value={form.bankAccountId} onValueChange={(v) => setForm((p) => ({ ...p, bankAccountId: v }))}>
                  <SelectTrigger><SelectValue>
                    {filteredBankAccounts.find((ba) => ba.value === form.bankAccountId)?.label ?? t('ccPayments.form.selectBank')}
                  </SelectValue></SelectTrigger>
                  <SelectContent>
                    {filteredBankAccounts.map((ba) => (
                      <SelectItem key={ba.value} value={ba.value}>{ba.label}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}
            <div className="space-y-1.5">
              <Label>{t('ccPayments.form.amount')}</Label>
              <AmountInput
                value={form.amount}
                onChange={(v) => setForm((p) => ({ ...p, amount: v }))}
                maxDecimals={2}
                disabled={form.isTotalPayment && !!form.statementPeriodId}
              />
              {form.isTotalPayment && form.statementPeriodId && (
                <p className="text-xs text-muted-foreground">{t('ccPayments.form.totalPaymentHint')}</p>
              )}
            </div>
            {erState.show && (
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1.5">
                  <Label>{t('ccPayments.form.primaryExchangeRate')}</Label>
                  <AmountInput
                    value={form.primaryExchangeRate}
                    onChange={(v) => setForm((p) => ({ ...p, primaryExchangeRate: v }))}
                    maxDecimals={6}
                    disabled={erState.lockPrimary}
                  />
                </div>
                <div className="space-y-1.5">
                  <Label>{t('ccPayments.form.secondaryExchangeRate')}</Label>
                  <AmountInput
                    value={form.secondaryExchangeRate}
                    onChange={(v) => setForm((p) => ({ ...p, secondaryExchangeRate: v }))}
                    maxDecimals={6}
                    disabled={erState.lockSecondary}
                  />
                </div>
              </div>
            )}
          </div>
          <SheetFooter className="flex-row gap-2 border-t pt-4">
            <SheetClose render={<Button variant="outline" className="flex-1" />}>
              {t('common.cancel')}
            </SheetClose>
            <Button className="flex-1" disabled={saving} onClick={savePayment}>
              {saving ? t('common.saving') : t('common.save')}
            </Button>
          </SheetFooter>
        </SheetContent>
      </Sheet>

      <ConfirmDialog
        open={deleteId !== null}
        onOpenChange={(open) => { if (!open) setDeleteId(null) }}
        onConfirm={handleDelete}
      />
    </div>
  )
}
