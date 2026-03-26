import { useCallback, useEffect, useRef, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { ArrowLeft, RefreshCw, Info } from 'lucide-react'
import { Tooltip, TooltipTrigger, TooltipContent, TooltipProvider } from '@/components/ui/tooltip'
import { AmountInput } from '@/components/ui/amount-input'
import { AuditInfo } from '@/components/ui/audit-info'
import api from '@/lib/api'
import axios from 'axios'
import { useAuth } from '@/contexts/AuthContext'
import { loadCashBoxOptions, loadBankAccountOptions, type PaymentEntityOption } from '@/lib/payment-entities'
import type { TransferDto, FamilySettingsDto, TransferStatus } from '@/types/api'

function extractError(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data
    if (data?.message) return data.message
  }
  return 'Ocurrió un error inesperado'
}

function formatDateISO(d: Date): string {
  return d.toISOString().slice(0, 10)
}

function normalizeDecimal(value: string): string {
  return value.replace(',', '.')
}

function round2(n: number): string {
  return n.toFixed(2)
}

type AccountType = 'CashBox' | 'BankAccount'

interface TransferForm {
  date: string
  fromType: AccountType
  fromId: string
  toType: AccountType
  toId: string
  amount: string
  transferRate: string
  amountTo: string
  fromPrimaryExchangeRate: string
  fromSecondaryExchangeRate: string
  toPrimaryExchangeRate: string
  toSecondaryExchangeRate: string
  description: string
}

const defaultForm = (): TransferForm => ({
  date: formatDateISO(new Date()),
  fromType: 'CashBox',
  fromId: '',
  toType: 'CashBox',
  toId: '',
  amount: '',
  transferRate: '1',
  amountTo: '',
  fromPrimaryExchangeRate: '1',
  fromSecondaryExchangeRate: '1',
  toPrimaryExchangeRate: '1',
  toSecondaryExchangeRate: '1',
  description: '',
})

export default function TransferFormPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { id } = useParams<{ id: string }>()
  const { userId: currentUserId } = useAuth()
  const isEdit = !!id

  const [form, setForm] = useState<TransferForm>(defaultForm())
  const [saving, setSaving] = useState(false)
  const [fetchingRate, setFetchingRate] = useState(false)
  const [loading, setLoading] = useState(true)
  const [rowVersion, setRowVersion] = useState<number | null>(null)
  const [familySettings, setFamilySettings] = useState<FamilySettingsDto | null>(null)
  const [isReadOnly, setIsReadOnly] = useState(false)
  const [transferStatus, setTransferStatus] = useState<TransferStatus | null>(null)
  const [isAutoConfirmed, setIsAutoConfirmed] = useState(false)
  const [auditInfo, setAuditInfo] = useState<{ createdAt: string; createdByName: string | null; modifiedAt: string | null; modifiedByName: string | null } | null>(null)

  const [cashBoxes, setCashBoxes] = useState<PaymentEntityOption[]>([])
  const [bankAccounts, setBankAccounts] = useState<PaymentEntityOption[]>([])

  // Prevents auto-fill from running once when form is loaded from server (edit mode)
  const skipAutoFill = useRef(false)

  const loadOptions = useCallback(async () => {
    const [cbs, bas] = await Promise.all([loadCashBoxOptions(), loadBankAccountOptions()])
    setCashBoxes(cbs)
    setBankAccounts(bas)
  }, [])

  const loadSettings = useCallback(async () => {
    try {
      const { data } = await api.get<FamilySettingsDto>('/family-settings')
      setFamilySettings(data)
      return data
    } catch { return null }
  }, [])

  useEffect(() => {
    const init = async () => {
      setLoading(true)
      await Promise.all([loadSettings(), loadOptions()])

      if (isEdit && id) {
        try {
          const { data } = await api.get<TransferDto>(`/transfers/${id}`)
          const canEdit = data.status === 'PendingConfirmation' && data.creatorUserId === currentUserId
          if (!canEdit) setIsReadOnly(true)
          setTransferStatus(data.status)
          setIsAutoConfirmed(data.isAutoConfirmed)
          setAuditInfo({
            createdAt: data.createdAt,
            createdByName: data.createdByName ?? null,
            modifiedAt: data.modifiedAt ?? null,
            modifiedByName: data.modifiedByName ?? null,
          })
          setRowVersion(data.rowVersion)
          skipAutoFill.current = true
          setForm({
            date: data.date,
            fromType: data.fromCashBoxId ? 'CashBox' : 'BankAccount',
            fromId: data.fromCashBoxId ?? data.fromBankAccountId ?? '',
            toType: data.toCashBoxId ? 'CashBox' : 'BankAccount',
            toId: data.toCashBoxId ?? data.toBankAccountId ?? '',
            amount: data.amount.toFixed(2),
            transferRate: String(data.exchangeRate),
            amountTo: data.amountTo.toFixed(2),
            fromPrimaryExchangeRate: String(data.fromPrimaryExchangeRate),
            fromSecondaryExchangeRate: String(data.fromSecondaryExchangeRate),
            toPrimaryExchangeRate: String(data.toPrimaryExchangeRate),
            toSecondaryExchangeRate: String(data.toSecondaryExchangeRate),
            description: data.description ?? '',
          })
        } catch (err) {
          toast.error(extractError(err))
          navigate('/transfers')
        }
      }

      setLoading(false)
    }
    init()
  }, [id, isEdit, loadSettings, loadOptions, navigate])

  const primaryCurrency = familySettings?.primaryCurrencyCode ?? ''
  const secondaryCurrency = familySettings?.secondaryCurrencyCode ?? ''

  const fromOptions = form.fromType === 'CashBox'
    ? cashBoxes.filter((c) => (c.isActive !== false && c.canOperate !== false) || c.value === form.fromId)
    : bankAccounts.filter((b) => b.isActive !== false || b.value === form.fromId)
  const toOptions = form.toType === 'CashBox'
    ? cashBoxes.filter((c) => c.isActive !== false || c.value === form.toId)
    : bankAccounts.filter((b) => b.isActive !== false || b.value === form.toId)

  const fromCurrencyCode = fromOptions.find((o) => o.value === form.fromId)?.currencyCode ?? ''
  const toCurrencyCode = toOptions.find((o) => o.value === form.toId)?.currencyCode ?? ''
  const sameCurrency = fromCurrencyCode !== '' && fromCurrencyCode === toCurrencyCode

  // ── Auto-fill TC + bimonetary rates when date / currencies change ────────────
  useEffect(() => {
    if (!fromCurrencyCode || !toCurrencyCode || !form.date) return

    if (skipAutoFill.current) {
      skipAutoFill.current = false
      return
    }

    const fetchAllRates = async () => {
      const updates: Partial<TransferForm> = {}

      // Transfer rate
      if (fromCurrencyCode === toCurrencyCode) {
        updates.transferRate = '1'
      } else {
        try {
          const { data } = await api.get<{ rate: number }>('/exchange-rates', {
            params: { base_currency: fromCurrencyCode, target_currency: toCurrencyCode, date: form.date },
          })
          updates.transferRate = String(data.rate)
        } catch { /* user can enter manually */ }
      }

      // Bimonetary exchange rates
      if (primaryCurrency && secondaryCurrency) {
        // From-currency rates
        if (fromCurrencyCode === primaryCurrency) {
          updates.fromPrimaryExchangeRate = '1'
        } else {
          try {
            const { data } = await api.get<{ rate: number }>('/exchange-rates', {
              params: { base_currency: fromCurrencyCode, target_currency: primaryCurrency, date: form.date },
            })
            updates.fromPrimaryExchangeRate = String(data.rate)
          } catch { /* ignore */ }
        }

        if (fromCurrencyCode === secondaryCurrency) {
          updates.fromSecondaryExchangeRate = '1'
        } else {
          try {
            const { data } = await api.get<{ rate: number }>('/exchange-rates', {
              params: { base_currency: fromCurrencyCode, target_currency: secondaryCurrency, date: form.date },
            })
            updates.fromSecondaryExchangeRate = String(data.rate)
          } catch { /* ignore */ }
        }

        // To-currency rates (skip if same currency — synced below)
        if (fromCurrencyCode !== toCurrencyCode) {
          if (toCurrencyCode === primaryCurrency) {
            updates.toPrimaryExchangeRate = '1'
          } else {
            try {
              const { data } = await api.get<{ rate: number }>('/exchange-rates', {
                params: { base_currency: toCurrencyCode, target_currency: primaryCurrency, date: form.date },
              })
              updates.toPrimaryExchangeRate = String(data.rate)
            } catch { /* ignore */ }
          }

          if (toCurrencyCode === secondaryCurrency) {
            updates.toSecondaryExchangeRate = '1'
          } else {
            try {
              const { data } = await api.get<{ rate: number }>('/exchange-rates', {
                params: { base_currency: toCurrencyCode, target_currency: secondaryCurrency, date: form.date },
              })
              updates.toSecondaryExchangeRate = String(data.rate)
            } catch { /* ignore */ }
          }
        }
      }

      setForm((p) => {
        const next = { ...p, ...updates }
        if (fromCurrencyCode === toCurrencyCode) {
          next.transferRate = '1'
          next.amountTo = p.amount
          next.toPrimaryExchangeRate = next.fromPrimaryExchangeRate
          next.toSecondaryExchangeRate = next.fromSecondaryExchangeRate
        } else if (updates.transferRate) {
          const rate = parseFloat(updates.transferRate)
          const amount = parseFloat(normalizeDecimal(p.amount))
          if (!isNaN(rate) && rate > 0 && !isNaN(amount) && amount > 0) {
            next.amountTo = round2(amount * rate)
          }
        }
        return next
      })
    }

    fetchAllRates()
  }, [fromCurrencyCode, toCurrencyCode, form.date, primaryCurrency, secondaryCurrency]) // eslint-disable-line react-hooks/exhaustive-deps

  // ── Lock states for exchange rate inputs ─────────────────────────────────────
  const getExchangeRateState = (currencyCode: string) => {
    if (currencyCode === primaryCurrency) return { lockPrimary: true, lockSecondary: false }
    if (currencyCode === secondaryCurrency) return { lockPrimary: false, lockSecondary: true }
    return { lockPrimary: false, lockSecondary: false }
  }

  const fromErState = getExchangeRateState(fromCurrencyCode)
  const toErState = getExchangeRateState(toCurrencyCode)
  const showBimonetary = !!(fromCurrencyCode && primaryCurrency && secondaryCurrency)

  // ── Tooltip values ───────────────────────────────────────────────────────────
  const ttFmt = (n: number) => n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  const ttFromPrimary  = parseFloat(normalizeDecimal(form.fromPrimaryExchangeRate)) || 1
  const ttFromSecondary = parseFloat(normalizeDecimal(form.fromSecondaryExchangeRate)) || 1
  const ttToPrimary    = parseFloat(normalizeDecimal(form.toPrimaryExchangeRate)) || 1
  const ttToSecondary  = parseFloat(normalizeDecimal(form.toSecondaryExchangeRate)) || 1

  const ttFromAmount     = parseFloat(normalizeDecimal(form.amount)) || 0
  const ttFromIsPrimary  = fromCurrencyCode === primaryCurrency
  const ttFromIsSecondary = fromCurrencyCode === secondaryCurrency
  const ttFromHasAmount  = ttFromAmount > 0

  const ttToAmountRaw = parseFloat(normalizeDecimal(sameCurrency ? form.amount : form.amountTo)) || 0
  const ttToIsPrimary   = toCurrencyCode === primaryCurrency
  const ttToIsSecondary = toCurrencyCode === secondaryCurrency
  const ttToHasAmount   = ttToAmountRaw > 0

  const handleRateInput = (value: string, setter: (v: string) => void) => {
    const normalized = normalizeDecimal(value)
    if (normalized === '' || /^\d*\.?\d{0,8}$/.test(normalized)) setter(normalized)
  }

  // Called when transferRate changes (after validation): updates amountTo + bimonetary ERs
  const fromIsPrimary = fromCurrencyCode === primaryCurrency
  const fromIsSecondary = fromCurrencyCode === secondaryCurrency
  const toIsPrimary = toCurrencyCode === primaryCurrency
  const toIsSecondary = toCurrencyCode === secondaryCurrency

  const handleTransferRateChange = (v: string) => {
    setForm((p) => {
      const next = { ...p, transferRate: v }
      if (!sameCurrency) {
        const rate = parseFloat(normalizeDecimal(v))
        const amount = parseFloat(normalizeDecimal(p.amount))
        if (!isNaN(rate) && rate > 0 && !isNaN(amount) && amount > 0) {
          next.amountTo = round2(amount * rate)
        }
        // Derive bimonetary ERs from transferRate when at least one side is a reference currency.
        // If both are tertiary, leave ERs untouched (already fetched from API).
        if (!isNaN(rate) && rate > 0) {
          const prevFromPrimary  = parseFloat(normalizeDecimal(p.fromPrimaryExchangeRate))  || 1
          const prevFromSecondary = parseFloat(normalizeDecimal(p.fromSecondaryExchangeRate)) || 1
          const prevToPrimary    = parseFloat(normalizeDecimal(p.toPrimaryExchangeRate))    || 1
          const prevToSecondary  = parseFloat(normalizeDecimal(p.toSecondaryExchangeRate))  || 1

          if (fromIsPrimary) {
            next.fromPrimaryExchangeRate = '1'
            next.toPrimaryExchangeRate = String(1 / rate)
            if (!toIsSecondary)
              // to is tertiary: to→secondary = to→primary × primary→secondary
              next.toSecondaryExchangeRate = String((1 / rate) * prevFromSecondary)
          }
          if (fromIsSecondary) {
            next.fromSecondaryExchangeRate = '1'
            next.toSecondaryExchangeRate = String(1 / rate)
            if (!toIsPrimary)
              // to is tertiary: to→primary = to→secondary × secondary→primary
              next.toPrimaryExchangeRate = String((1 / rate) * prevFromPrimary)
          }
          if (toIsPrimary) {
            next.toPrimaryExchangeRate = '1'
            next.fromPrimaryExchangeRate = v
            if (!fromIsSecondary)
              // from is tertiary: from→secondary = from→primary × primary→secondary
              next.fromSecondaryExchangeRate = String(rate * prevToSecondary)
          }
          if (toIsSecondary) {
            next.toSecondaryExchangeRate = '1'
            next.fromSecondaryExchangeRate = v
            if (!fromIsPrimary)
              // from is tertiary: from→primary = from→secondary × secondary→primary
              next.fromPrimaryExchangeRate = String(rate * prevToPrimary)
          }
        }
      }
      return next
    })
  }

  const handleFetchRate = async () => {
    if (!fromCurrencyCode || !form.date) return
    setFetchingRate(true)
    try {
      const updates: Partial<TransferForm> = {}
      const fromSt = getExchangeRateState(fromCurrencyCode)
      const toSt   = getExchangeRateState(toCurrencyCode)

      // From-currency rates
      if (!fromSt.lockPrimary && primaryCurrency && primaryCurrency !== fromCurrencyCode) {
        const { data } = await api.get<{ rate: number }>('/exchange-rates', {
          params: { base_currency: fromCurrencyCode, target_currency: primaryCurrency, date: form.date }
        })
        updates.fromPrimaryExchangeRate = String(data.rate)
      }
      if (!fromSt.lockSecondary && secondaryCurrency && secondaryCurrency !== fromCurrencyCode) {
        const { data } = await api.get<{ rate: number }>('/exchange-rates', {
          params: { base_currency: fromCurrencyCode, target_currency: secondaryCurrency, date: form.date }
        })
        updates.fromSecondaryExchangeRate = String(data.rate)
      }

      // To-currency rates (skip if same currency — will be synced from from-rates)
      if (!sameCurrency && toCurrencyCode) {
        if (!toSt.lockPrimary && primaryCurrency && primaryCurrency !== toCurrencyCode) {
          const { data } = await api.get<{ rate: number }>('/exchange-rates', {
            params: { base_currency: toCurrencyCode, target_currency: primaryCurrency, date: form.date }
          })
          updates.toPrimaryExchangeRate = String(data.rate)
        }
        if (!toSt.lockSecondary && secondaryCurrency && secondaryCurrency !== toCurrencyCode) {
          const { data } = await api.get<{ rate: number }>('/exchange-rates', {
            params: { base_currency: toCurrencyCode, target_currency: secondaryCurrency, date: form.date }
          })
          updates.toSecondaryExchangeRate = String(data.rate)
        }
      }

      if (Object.keys(updates).length > 0) {
        setForm((p) => {
          const next = { ...p, ...updates }
          // If same currency, keep to-rates in sync with from-rates
          if (sameCurrency) {
            next.toPrimaryExchangeRate = next.fromPrimaryExchangeRate
            next.toSecondaryExchangeRate = next.fromSecondaryExchangeRate
          }
          return next
        })
      }
    } catch {
      toast.error(t('common.fetchRateError'))
    } finally {
      setFetchingRate(false)
    }
  }

  const reloadForm = async () => {
    if (!id) return
    try {
      const { data } = await api.get<TransferDto>(`/transfers/${id}`)
      setRowVersion(data.rowVersion)
      skipAutoFill.current = true
      setForm({
        date: data.date,
        fromType: data.fromCashBoxId ? 'CashBox' : 'BankAccount',
        fromId: data.fromCashBoxId ?? data.fromBankAccountId ?? '',
        toType: data.toCashBoxId ? 'CashBox' : 'BankAccount',
        toId: data.toCashBoxId ?? data.toBankAccountId ?? '',
        amount: data.amount.toFixed(2),
        transferRate: String(data.exchangeRate),
        amountTo: data.amountTo.toFixed(2),
        fromPrimaryExchangeRate: String(data.fromPrimaryExchangeRate),
        fromSecondaryExchangeRate: String(data.fromSecondaryExchangeRate),
        toPrimaryExchangeRate: String(data.toPrimaryExchangeRate),
        toSecondaryExchangeRate: String(data.toSecondaryExchangeRate),
        description: data.description ?? '',
      })
    } catch {
      toast.error(t('transfers.form.reloadError'))
    }
  }

  const handleConflictError = (err: unknown) => {
    if (axios.isAxiosError(err) && err.response?.status === 409) {
      toast.error(t('transfers.form.concurrentModification'), {
        description: t('transfers.form.concurrentModificationDesc'),
        action: { label: t('transfers.form.reload'), onClick: reloadForm },
        duration: 10000,
      })
    } else {
      toast.error(extractError(err))
    }
  }

  const validate = (): boolean => {
    if (!form.date) { toast.error(t('transfers.form.errors.dateRequired')); return false }
    if (!form.fromId) { toast.error(t('transfers.form.errors.fromRequired')); return false }
    if (!form.toId) { toast.error(t('transfers.form.errors.toRequired')); return false }
    if (form.fromType === form.toType && form.fromId === form.toId) {
      toast.error(t('transfers.form.errors.sameAccount'))
      return false
    }
    const amount = parseFloat(normalizeDecimal(form.amount))
    if (isNaN(amount) || amount <= 0) { toast.error(t('transfers.form.errors.fromAmountRequired')); return false }
    const amountTo = parseFloat(normalizeDecimal(form.amountTo))
    if (isNaN(amountTo) || amountTo <= 0) { toast.error(t('transfers.form.errors.toAmountRequired')); return false }
    return true
  }

  const buildPayload = () => {
    const amount = parseFloat(normalizeDecimal(form.amount))
    const amountTo = sameCurrency ? amount : parseFloat(normalizeDecimal(form.amountTo))
    const fromPrimary = parseFloat(normalizeDecimal(form.fromPrimaryExchangeRate)) || 1
    const fromSecondary = parseFloat(normalizeDecimal(form.fromSecondaryExchangeRate)) || 1
    return {
      date: form.date,
      fromCashBoxId: form.fromType === 'CashBox' ? form.fromId : null,
      fromBankAccountId: form.fromType === 'BankAccount' ? form.fromId : null,
      toCashBoxId: form.toType === 'CashBox' ? form.toId : null,
      toBankAccountId: form.toType === 'BankAccount' ? form.toId : null,
      amount,
      amountTo,
      fromPrimaryExchangeRate: fromPrimary,
      fromSecondaryExchangeRate: fromSecondary,
      toPrimaryExchangeRate: sameCurrency ? fromPrimary : parseFloat(normalizeDecimal(form.toPrimaryExchangeRate)) || 1,
      toSecondaryExchangeRate: sameCurrency ? fromSecondary : parseFloat(normalizeDecimal(form.toSecondaryExchangeRate)) || 1,
      description: form.description.trim() || null,
      source: 'Web',
      clientRowVersion: isEdit ? rowVersion : null,
    }
  }

  const handleSave = async () => {
    if (!validate()) return
    setSaving(true)
    try {
      const payload = buildPayload()
      if (isEdit && id) {
        const { data } = await api.put<TransferDto>(`/transfers/${id}`, payload)
        setRowVersion(data.rowVersion)
        toast.success(t('transfers.form.updated'))
      } else {
        await api.post('/transfers', payload)
        toast.success(t('transfers.form.created'))
      }
      navigate('/transfers')
    } catch (err) {
      handleConflictError(err)
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return <div className="flex items-center justify-center py-20 text-muted-foreground">{t('common.loading')}</div>
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6 pb-10">
      {/* Header */}
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" onClick={() => navigate('/transfers')}>
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <h1 className="text-2xl font-bold">
          {isReadOnly ? t('transfers.form.viewTitle') : isEdit ? t('transfers.form.editTitle') : t('transfers.form.newTitle')}
        </h1>
        {isReadOnly && transferStatus && (
          <Badge variant={transferStatus === 'Confirmed' ? 'default' : transferStatus === 'Rejected' ? 'destructive' : 'secondary'}>
            {transferStatus === 'Confirmed' && isAutoConfirmed
              ? t('transfers.status.autoConfirmed')
              : t(`transfers.status.${transferStatus === 'Confirmed' ? 'confirmed' : transferStatus === 'Rejected' ? 'rejected' : 'pending'}`)}
          </Badge>
        )}
      </div>

      <div className="space-y-5">
        {/* Fecha */}
        <div className="space-y-1.5">
          <Label>{t('transfers.form.date')}</Label>
          <Input
            type="date"
            value={form.date}
            onChange={(e) => setForm((p) => ({ ...p, date: e.target.value }))}
            max={formatDateISO(new Date())}
            className="w-48"
            disabled={isReadOnly}
          />
        </div>

        {/* Origen */}
        <div className="space-y-2">
          <Label className="text-base font-semibold">{t('transfers.form.origin')}</Label>
          <div className="grid gap-3" style={{ gridTemplateColumns: '160px 1fr' }}>
            <div className="space-y-1.5">
              <Label className="text-xs">{t('transfers.form.type')}</Label>
              <Select
                value={form.fromType}
                onValueChange={(v) => setForm((p) => ({ ...p, fromType: v as AccountType, fromId: '' }))}
                disabled={isReadOnly}
              >
                <SelectTrigger>
                  <SelectValue>
                    {{ CashBox: t('transfers.form.cashBox'), BankAccount: t('transfers.form.bank') }[form.fromType]}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="CashBox">{t('transfers.form.cashBox')}</SelectItem>
                  <SelectItem value="BankAccount">{t('transfers.form.bank')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label className="text-xs">{form.fromType === 'CashBox' ? t('transfers.form.cashBox') : t('transfers.form.bank')}</Label>
              <Select
                value={form.fromId}
                onValueChange={(v) => setForm((p) => ({ ...p, fromId: v }))}
                disabled={isReadOnly}
              >
                <SelectTrigger>
                  <SelectValue>
                    {form.fromId
                      ? fromOptions.find((o) => o.value === form.fromId)?.label ?? t('transfers.form.select')
                      : t('transfers.form.select')}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  {fromOptions.length === 0
                    ? <SelectItem value="_none_" disabled>{t('transfers.form.noOptions')}</SelectItem>
                    : fromOptions.map((o) => (
                      <SelectItem key={o.value} value={o.value}>
                        {o.label} <span className="text-xs text-muted-foreground ml-1">({o.currencyCode})</span>
                      </SelectItem>
                    ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </div>

        {/* Destino */}
        <div className="space-y-2">
          <Label className="text-base font-semibold">{t('transfers.form.destination')}</Label>
          <div className="grid gap-3" style={{ gridTemplateColumns: '160px 1fr' }}>
            <div className="space-y-1.5">
              <Label className="text-xs">{t('transfers.form.type')}</Label>
              <Select
                value={form.toType}
                onValueChange={(v) => setForm((p) => ({ ...p, toType: v as AccountType, toId: '' }))}
                disabled={isReadOnly}
              >
                <SelectTrigger>
                  <SelectValue>
                    {{ CashBox: t('transfers.form.cashBox'), BankAccount: t('transfers.form.bank') }[form.toType]}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="CashBox">{t('transfers.form.cashBox')}</SelectItem>
                  <SelectItem value="BankAccount">{t('transfers.form.bank')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label className="text-xs">{form.toType === 'CashBox' ? t('transfers.form.cashBox') : t('transfers.form.bank')}</Label>
              <Select
                value={form.toId}
                onValueChange={(v) => setForm((p) => ({ ...p, toId: v }))}
                disabled={isReadOnly}
              >
                <SelectTrigger>
                  <SelectValue>
                    {form.toId
                      ? toOptions.find((o) => o.value === form.toId)?.label ?? t('transfers.form.select')
                      : t('transfers.form.select')}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  {toOptions.length === 0
                    ? <SelectItem value="_none_" disabled>{t('transfers.form.noOptions')}</SelectItem>
                    : toOptions.map((o) => (
                      <SelectItem key={o.value} value={o.value}>
                        {o.label} <span className="text-xs text-muted-foreground ml-1">({o.currencyCode})</span>
                      </SelectItem>
                    ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </div>

        {/* ── Importes + TC en una fila ── */}
        <div className="grid gap-3 items-end" style={{ gridTemplateColumns: fromCurrencyCode && toCurrencyCode ? '1fr auto 1fr' : '1fr 1fr' }}>

          {/* Importe origen */}
          <div className="space-y-1.5">
            <div className="flex items-center gap-1.5">
              <Label>
                {fromCurrencyCode ? t('transfers.form.fromAmount', { currency: fromCurrencyCode }) : t('transfers.form.fromAmountLabel')}
              </Label>
              {fromCurrencyCode && showBimonetary && (
                <TooltipProvider>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <Info className="h-3.5 w-3.5 text-muted-foreground cursor-default shrink-0" />
                    </TooltipTrigger>
                    <TooltipContent side="top">
                      <div className="space-y-0.5">
                        {!ttFromIsPrimary && <p>TC {primaryCurrency}: {ttFromPrimary}</p>}
                        {!ttFromIsPrimary && ttFromHasAmount && <p>{ttFmt(ttFromAmount * ttFromPrimary)} {primaryCurrency}</p>}
                        {!ttFromIsSecondary && <p>TC {secondaryCurrency}: {ttFromSecondary}</p>}
                        {!ttFromIsSecondary && ttFromHasAmount && <p>{ttFmt(ttFromAmount * ttFromSecondary)} {secondaryCurrency}</p>}
                      </div>
                    </TooltipContent>
                  </Tooltip>
                </TooltipProvider>
              )}
            </div>
            <AmountInput
              value={form.amount}
              onChange={(v) => {
                setForm((p) => {
                  const next = { ...p, amount: v }
                  const rate = parseFloat(normalizeDecimal(p.transferRate))
                  const amount = parseFloat(v)
                  if (sameCurrency) {
                    next.amountTo = v
                  } else if (!isNaN(rate) && rate > 0 && !isNaN(amount) && amount > 0) {
                    next.amountTo = round2(amount * rate)
                  } else if (v === '') {
                    next.amountTo = ''
                  }
                  return next
                })
              }}
              placeholder="0.00"
              disabled={isReadOnly}
            />
          </div>

          {/* TC origen → destino */}
          {fromCurrencyCode && toCurrencyCode && (
            <div className="space-y-1.5">
              <Label className="text-xs text-muted-foreground whitespace-nowrap">
                TC {fromCurrencyCode}/{toCurrencyCode}
              </Label>
              <Input
                inputMode="decimal"
                value={form.transferRate}
                onChange={(e) => handleRateInput(e.target.value, handleTransferRateChange)}
                disabled={sameCurrency || isReadOnly}
                className={`w-28 ${sameCurrency ? 'bg-muted' : ''}`}
                placeholder="1.000000"
              />
            </div>
          )}

          {/* Importe destino */}
          <div className="space-y-1.5">
            <div className="flex items-center gap-1.5">
              <Label>
                {toCurrencyCode ? t('transfers.form.toAmount', { currency: toCurrencyCode }) : t('transfers.form.toAmountLabel')}
              </Label>
              {toCurrencyCode && showBimonetary && (
                <TooltipProvider>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <Info className="h-3.5 w-3.5 text-muted-foreground cursor-default shrink-0" />
                    </TooltipTrigger>
                    <TooltipContent side="top">
                      <div className="space-y-0.5">
                        {!ttToIsPrimary && <p>TC {primaryCurrency}: {ttToPrimary}</p>}
                        {!ttToIsPrimary && ttToHasAmount && <p>{ttFmt(ttToAmountRaw * ttToPrimary)} {primaryCurrency}</p>}
                        {!ttToIsSecondary && <p>TC {secondaryCurrency}: {ttToSecondary}</p>}
                        {!ttToIsSecondary && ttToHasAmount && <p>{ttFmt(ttToAmountRaw * ttToSecondary)} {secondaryCurrency}</p>}
                      </div>
                    </TooltipContent>
                  </Tooltip>
                </TooltipProvider>
              )}
            </div>
            <AmountInput
              value={sameCurrency ? form.amount : form.amountTo}
              onChange={(v) => {
                if (sameCurrency) return
                setForm((p) => {
                  const next = { ...p, amountTo: v }
                  const rate = parseFloat(normalizeDecimal(p.transferRate))
                  const amountTo = parseFloat(v)
                  if (!isNaN(rate) && rate > 0 && !isNaN(amountTo) && amountTo > 0) {
                    next.amount = round2(amountTo / rate)
                  } else if (v === '') {
                    next.amount = ''
                  }
                  return next
                })
              }}
              placeholder="0.00"
              disabled={sameCurrency || isReadOnly}
              className={sameCurrency ? 'bg-muted' : ''}
            />
          </div>

        </div>


        {/* Descripción */}
        <div className="space-y-1.5">
          <Label>{t('transfers.form.description')}</Label>
          <Input
            value={form.description}
            onChange={(e) => setForm((p) => ({ ...p, description: e.target.value }))}
            placeholder={t('transfers.form.descriptionPlaceholder')}
            maxLength={500}
            disabled={isReadOnly}
          />
        </div>
      </div>

      {/* Footer */}
      <div className="border-t pt-6 space-y-3">
        {isEdit && auditInfo && (
          <AuditInfo
            createdAt={auditInfo.createdAt}
            createdByName={auditInfo.createdByName}
            modifiedAt={auditInfo.modifiedAt}
            modifiedByName={auditInfo.modifiedByName}
          />
        )}
        <div className="flex items-center justify-between">
          <Button variant="outline" onClick={() => navigate('/transfers')}>
            {isReadOnly ? t('common.back') : t('transfers.form.cancel')}
          </Button>
          {!isReadOnly && (
            <Button disabled={saving} onClick={handleSave}>
              {saving ? t('transfers.form.saving') : t('transfers.form.save')}
            </Button>
          )}
        </div>
      </div>
    </div>
  )
}
