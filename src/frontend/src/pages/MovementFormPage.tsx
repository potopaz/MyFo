import { useCallback, useEffect, useRef, useState } from 'react'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { ComboboxField } from '@/components/crud/ComboboxField'
import { AmountInput } from '@/components/ui/amount-input'
import { Tooltip, TooltipTrigger, TooltipContent, TooltipProvider } from '@/components/ui/tooltip'
import { Plus, X, Copy, ArrowLeft, Info, Lock } from 'lucide-react'
import { AuditInfo } from '@/components/ui/audit-info'
import api from '@/lib/api'
import axios from 'axios'
import { loadFamilyCurrencyOptions } from '@/lib/currency-options'
import { loadCostCenterOptions, clearCostCenterCache } from '@/lib/costcenter-options'
import { loadSubcategoryOptions, clearSubcategoryCache, type SubcategoryOption } from '@/lib/subcategory-options'
import { loadCashBoxOptions, loadBankAccountOptions, loadCreditCardOptions, type PaymentEntityOption, type CreditCardOption } from '@/lib/payment-entities'
import type { MovementDto, FamilySettingsDto, FrequentMovementDto } from '@/types/api'
import { useNotifications } from '@/contexts/NotificationsContext'

function extractError(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data
    if (data?.message) return data.message
  }
  return 'Ocurrió un error inesperado'
}

function normalizeDecimal(value: string): string {
  return value.replace(',', '.')
}

function formatDateISO(d: Date): string {
  return d.toISOString().slice(0, 10)
}


interface PaymentRow {
  movementPaymentId?: string
  paymentMethodType: string
  amount: string
  cashBoxId: string
  bankAccountId: string
  creditCardId: string
  creditCardMemberId: string
  installments: string
  bonificationType: string
  bonificationValue: string
  locked?: boolean
}

const emptyPayment: PaymentRow = {
  paymentMethodType: 'CashBox',
  amount: '',
  cashBoxId: '',
  bankAccountId: '',
  creditCardId: '',
  creditCardMemberId: '',
  installments: '1',
  bonificationType: '',
  bonificationValue: '',
}

function stripCreditCardPayments(payments: PaymentRow[]): PaymentRow[] {
  const filtered = payments.filter((p) => p.paymentMethodType !== 'CreditCard')
  return filtered.length > 0 ? filtered : [{ ...emptyPayment }]
}

function recomputeAmount(payments: PaymentRow[]): string {
  const total = Math.round(payments.reduce((sum, p) => sum + parseFloat(p.amount.replace(',', '.') || '0'), 0) * 100) / 100
  return total > 0 ? String(total) : ''
}

interface MovementForm {
  date: string
  movementType: string
  amount: string
  currencyCode: string
  primaryExchangeRate: string
  secondaryExchangeRate: string
  description: string
  subcategoryId: string
  accountingType: string
  isOrdinary: string
  costCenterId: string
  payments: PaymentRow[]
}

const defaultForm = (primaryCurrency = ''): MovementForm => ({
  date: formatDateISO(new Date()),
  movementType: '',
  amount: '',
  currencyCode: primaryCurrency,
  primaryExchangeRate: '1',
  secondaryExchangeRate: '1',
  description: '',
  subcategoryId: '',
  accountingType: '',
  isOrdinary: '',
  costCenterId: '',
  payments: [{ ...emptyPayment }],
})

export default function MovementFormPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { id } = useParams<{ id: string }>()
  const [searchParams] = useSearchParams()
  const duplicateId = searchParams.get('duplicate')
  const fromFrequentId = searchParams.get('from')

  const isEdit = !!id
  const isDuplicate = !!duplicateId
  const isFromFrequent = !!fromFrequentId

  const { refresh: refreshNotifications } = useNotifications()

  const [form, setForm] = useState<MovementForm>(defaultForm())
  const [saving, setSaving] = useState(false)
  const [fetchingRate, setFetchingRate] = useState(false)
  const [familySettings, setFamilySettings] = useState<FamilySettingsDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [auditInfo, setAuditInfo] = useState<{ createdAt: string; createdByName: string | null; modifiedAt: string | null; modifiedByName: string | null } | null>(null)
  const [rowVersion, setRowVersion] = useState<number | null>(null)
  const skipAutoFetchRef = useRef(0)

  const [subcategoryOptions, setSubcategoryOptions] = useState<SubcategoryOption[]>([])
  const [cashBoxes, setCashBoxes] = useState<PaymentEntityOption[]>([])
  const [bankAccounts, setBankAccounts] = useState<PaymentEntityOption[]>([])
  const [creditCards, setCreditCards] = useState<CreditCardOption[]>([])

  const loadOptions = useCallback(async () => {
    clearSubcategoryCache()
    const [subs, cbs, bas, ccs] = await Promise.all([
      loadSubcategoryOptions(),
      loadCashBoxOptions(),
      loadBankAccountOptions(),
      loadCreditCardOptions(),
    ])
    setSubcategoryOptions(subs)
    setCashBoxes(cbs)
    setBankAccounts(bas)
    setCreditCards(ccs)
  }, [])

  const loadCostCentersNoCache = useCallback(async () => {
    clearCostCenterCache()
    return loadCostCenterOptions()
  }, [])

  const loadSettings = useCallback(async () => {
    try {
      const { data } = await api.get<FamilySettingsDto>('/family-settings')
      setFamilySettings(data)
      return data
    } catch { return null }
  }, [])

  // Cargar datos iniciales
  useEffect(() => {
    const init = async () => {
      setLoading(true)
      const [settings] = await Promise.all([loadSettings(), loadOptions()])
      const primaryCurrency = settings?.primaryCurrencyCode ?? ''

      if (isEdit && id) {
        try {
          const { data } = await api.get<MovementDto>(`/movements/${id}`)
          setAuditInfo({
            createdAt: data.createdAt,
            createdByName: data.createdByName ?? null,
            modifiedAt: data.modifiedAt ?? null,
            modifiedByName: data.modifiedByName ?? null,
          })
          setRowVersion(data.rowVersion)
          setForm({
            date: data.date,
            movementType: data.movementType,
            amount: data.amount.toFixed(2),
            currencyCode: data.currencyCode,
            primaryExchangeRate: String(data.primaryExchangeRate),
            secondaryExchangeRate: String(data.secondaryExchangeRate),
            description: data.description ?? '',
            subcategoryId: data.subcategoryId,
            accountingType: data.accountingType ?? '',
            isOrdinary: data.isOrdinary === null ? '' : data.isOrdinary ? 'true' : 'false',
            costCenterId: data.costCenterId ?? '',
            payments: data.payments.map((p) => ({
              movementPaymentId: p.movementPaymentId,
              paymentMethodType: p.paymentMethodType,
              amount: p.amount.toFixed(2),
              cashBoxId: p.cashBoxId ?? '',
              bankAccountId: p.bankAccountId ?? '',
              creditCardId: p.creditCardId ?? '',
              creditCardMemberId: p.creditCardMemberId ?? '',
              installments: p.installments ? String(p.installments) : '1',
              bonificationType: p.bonificationType ?? '',
              bonificationValue: p.bonificationValue ? String(p.bonificationValue) : '',
              locked: p.hasAssignedInstallments,
            })),
          })
          skipAutoFetchRef.current = 1 // skip auto-fetch on init for edit (keep saved rates)
        } catch (err) {
          toast.error(extractError(err))
          navigate('/movements')
        }
      } else if (isDuplicate && duplicateId) {
        try {
          const { data } = await api.get<MovementDto>(`/movements/${duplicateId}`)
          const newDate = formatDateISO(new Date())
          setForm({
            date: newDate, // fecha hoy, no la del original
            movementType: data.movementType,
            amount: data.amount.toFixed(2),
            currencyCode: data.currencyCode,
            primaryExchangeRate: String(data.primaryExchangeRate),
            secondaryExchangeRate: String(data.secondaryExchangeRate),
            description: data.description ?? '',
            subcategoryId: data.subcategoryId,
            accountingType: data.accountingType ?? '',
            isOrdinary: data.isOrdinary === null ? '' : data.isOrdinary ? 'true' : 'false',
            costCenterId: data.costCenterId ?? '',
            payments: data.payments.map((p) => ({
              paymentMethodType: p.paymentMethodType,
              amount: p.amount.toFixed(2),
              cashBoxId: p.cashBoxId ?? '',
              bankAccountId: p.bankAccountId ?? '',
              creditCardId: p.creditCardId ?? '',
              creditCardMemberId: p.creditCardMemberId ?? '',
              installments: p.installments ? String(p.installments) : '1',
              bonificationType: p.bonificationType ?? '',
              bonificationValue: p.bonificationValue ? String(p.bonificationValue) : '',
            })),
          })
        } catch (err) {
          toast.error(extractError(err))
          setForm(defaultForm(primaryCurrency))
        }
      } else if (isFromFrequent && fromFrequentId) {
        try {
          const { data } = await api.get<FrequentMovementDto>(`/frequent-movements/${fromFrequentId}`)
          const newDate = formatDateISO(new Date())
          const newCurrency = data.currencyCode

          setForm({
            date: newDate,
            movementType: data.movementType,
            amount: data.amount.toFixed(2),
            currencyCode: newCurrency,
            primaryExchangeRate: '1',
            secondaryExchangeRate: '1',
            description: data.description ?? '',
            subcategoryId: data.subcategoryId,
            accountingType: data.accountingType ?? '',
            isOrdinary: data.isOrdinary === null ? '' : data.isOrdinary ? 'true' : 'false',
            costCenterId: data.costCenterId ?? '',
            payments: [{
              paymentMethodType: data.paymentMethodType,
              amount: data.amount.toFixed(2),
              cashBoxId: data.cashBoxId ?? '',
              bankAccountId: data.bankAccountId ?? '',
              creditCardId: data.creditCardId ?? '',
              creditCardMemberId: data.creditCardMemberId ?? '',
              installments: '1',
              bonificationType: '',
              bonificationValue: '',
            }],
          })

          // Fetch exchange rates immediately with loaded settings
          const pCurr = settings?.primaryCurrencyCode ?? ''
          const sCurr = settings?.secondaryCurrencyCode ?? ''
          const lockPrimary = newCurrency === pCurr
          const lockSecondary = newCurrency === sCurr

          const updates: Record<string, string> = {}
          try {
            if (!lockPrimary && pCurr && pCurr !== newCurrency) {
              const { data: rateData } = await api.get<{ rate: number }>('/exchange-rates', {
                params: { base_currency: newCurrency, target_currency: pCurr, date: newDate },
              })
              updates.primaryExchangeRate = String(rateData.rate)
            }
            if (!lockSecondary && sCurr && sCurr !== newCurrency) {
              const { data: rateData } = await api.get<{ rate: number }>('/exchange-rates', {
                params: { base_currency: newCurrency, target_currency: sCurr, date: newDate },
              })
              updates.secondaryExchangeRate = String(rateData.rate)
            }
            if (Object.keys(updates).length > 0) {
              setForm((p) => ({ ...p, ...updates }))
            }
          } catch {
            // Silent fail - use default rates of 1
          }
        } catch (err) {
          toast.error(extractError(err))
          setForm(defaultForm(primaryCurrency))
        }
      } else {
        setForm(defaultForm(primaryCurrency))
      }
      setLoading(false)
    }
    init()
  }, [id, duplicateId, fromFrequentId, isEdit, isDuplicate, isFromFrequent, loadSettings, loadOptions, navigate])

  // Auto-fetch cotizaciones cuando cambia moneda o fecha
  useEffect(() => {
    if (!form.currencyCode || !form.date) return
    if (skipAutoFetchRef.current > 0) {
      skipAutoFetchRef.current--
      return
    }
    fetchRatesExplicit(form.currencyCode, form.date)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [form.currencyCode, form.date]) // eslint-disable-line react-hooks/exhaustive-deps

  // Atajos de teclado
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (!e.ctrlKey && !e.metaKey) return
      if (e.key === 'Enter') {
        e.preventDefault()
        if (e.shiftKey) {
          handleSaveAndNew()
        } else {
          handleSave()
        }
      }
    }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  })

  const primaryCurrency = familySettings?.primaryCurrencyCode ?? ''
  const secondaryCurrency = familySettings?.secondaryCurrencyCode ?? ''

  const getExchangeRateState = (currencyCode: string) => {
    if (currencyCode === primaryCurrency) return { showPrimary: true, showSecondary: true, lockPrimary: true, lockSecondary: false }
    if (currencyCode === secondaryCurrency) return { showPrimary: true, showSecondary: true, lockPrimary: false, lockSecondary: true }
    return { showPrimary: true, showSecondary: true, lockPrimary: false, lockSecondary: false }
  }

  const erState = getExchangeRateState(form.currencyCode)
  const selectedSub = subcategoryOptions.find((s) => s.value === form.subcategoryId)

  // Tooltip del importe total
  const ttAmount  = parseFloat(normalizeDecimal(form.amount)) || 0
  const ttExRate  = parseFloat(normalizeDecimal(form.primaryExchangeRate)) || 1
  const ttSecRate = parseFloat(normalizeDecimal(form.secondaryExchangeRate)) || 1
  const ttIsPrimary   = form.currencyCode === primaryCurrency
  const ttIsSecondary = form.currencyCode === secondaryCurrency
  const ttHasAmount   = ttAmount > 0
  const ttFmt = (n: number) => n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  const showMovementType = selectedSub?.subcategoryType === 'Both'

  const hasLockedPayments = form.payments.some((p) => p.locked)

  const usedCashBoxIds = new Set(form.payments.map(p => p.cashBoxId).filter(Boolean))
  const usedBankAccountIds = new Set(form.payments.map(p => p.bankAccountId).filter(Boolean))
  const usedCreditCardIds = new Set(form.payments.map(p => p.creditCardId).filter(Boolean))
  const filteredCashBoxes = cashBoxes.filter((c) => c.currencyCode === form.currencyCode && (c.isActive !== false || usedCashBoxIds.has(c.value)))
  const filteredBankAccounts = bankAccounts.filter((b) => b.currencyCode === form.currencyCode && (b.isActive !== false || usedBankAccountIds.has(b.value)))
  const filteredCreditCards = creditCards.filter((c) => c.currencyCode === form.currencyCode && (c.isActive !== false || usedCreditCardIds.has(c.value)))

  const updatePayment = (idx: number, updates: Partial<PaymentRow>) => {
    setForm((prev) => {
      const payments = [...prev.payments]
      payments[idx] = { ...payments[idx], ...updates }
      const totalAmount = Math.round(payments.reduce((sum, p) => sum + parseFloat(normalizeDecimal(p.amount) || '0'), 0) * 100) / 100
      return { ...prev, payments, amount: totalAmount > 0 ? String(totalAmount) : '' }
    })
  }

  const addPayment = () => {
    setForm((prev) => ({ ...prev, payments: [...prev.payments, { ...emptyPayment }] }))
  }

  const duplicatePayment = (idx: number) => {
    setForm((prev) => {
      const payments = [...prev.payments]
      payments.splice(idx + 1, 0, { ...payments[idx] })
      const totalAmount = Math.round(payments.reduce((sum, p) => sum + parseFloat(normalizeDecimal(p.amount) || '0'), 0) * 100) / 100
      return { ...prev, payments, amount: totalAmount > 0 ? String(totalAmount) : '' }
    })
  }

  const removePayment = (idx: number) => {
    setForm((prev) => {
      const payments = prev.payments.filter((_, i) => i !== idx)
      const totalAmount = Math.round(payments.reduce((sum, p) => sum + parseFloat(normalizeDecimal(p.amount) || '0'), 0) * 100) / 100
      return {
        ...prev,
        payments,
        amount: totalAmount > 0 ? String(totalAmount) : '',
      }
    })
  }

  const handleRateInput = (value: string, setter: (v: string) => void) => {
    const normalized = normalizeDecimal(value)
    if (normalized === '' || /^\d*\.?\d{0,8}$/.test(normalized)) setter(normalized)
  }

  const placeholderClass = (isPlaceholder: boolean) =>
    isPlaceholder ? 'text-muted-foreground' : ''

  const buildPayload = () => {
    const amount = parseFloat(normalizeDecimal(form.amount))
    return {
      date: form.date,
      movementType: showMovementType ? form.movementType : null,
      amount,
      currencyCode: form.currencyCode,
      primaryExchangeRate: parseFloat(normalizeDecimal(form.primaryExchangeRate)) || 1,
      secondaryExchangeRate: parseFloat(normalizeDecimal(form.secondaryExchangeRate)) || 1,
      description: form.description.trim() || null,
      subcategoryId: form.subcategoryId,
      accountingType: form.accountingType || null,
      isOrdinary: form.isOrdinary === '' ? null : form.isOrdinary === 'true',
      costCenterId: form.costCenterId || null,
      source: 'Web',
      clientRowVersion: isEdit ? rowVersion : null,
      payments: form.payments.map((p) => ({
        movementPaymentId: p.locked ? p.movementPaymentId : null,
        paymentMethodType: p.paymentMethodType,
        amount: parseFloat(normalizeDecimal(p.amount)),
        cashBoxId: p.paymentMethodType === 'CashBox' ? p.cashBoxId : null,
        bankAccountId: p.paymentMethodType === 'BankAccount' ? p.bankAccountId : null,
        creditCardId: p.paymentMethodType === 'CreditCard' ? p.creditCardId : null,
        creditCardMemberId: p.paymentMethodType === 'CreditCard' && p.creditCardMemberId ? p.creditCardMemberId : null,
        installments: p.paymentMethodType === 'CreditCard' ? parseInt(p.installments) || 1 : null,
        bonificationType: p.paymentMethodType === 'CreditCard' && p.bonificationType ? p.bonificationType : null,
        bonificationValue: p.paymentMethodType === 'CreditCard' && p.bonificationType && p.bonificationValue
          ? parseFloat(normalizeDecimal(p.bonificationValue)) : null,
      })),
    }
  }

  const validate = (): boolean => {
    if (!form.date) { toast.error(t('movements.form.errors.dateRequired')); return false }
    const amount = parseFloat(normalizeDecimal(form.amount))
    if (isNaN(amount) || amount <= 0) { toast.error(t('movements.form.errors.amountRequired')); return false }
    if (!form.currencyCode) { toast.error(t('movements.form.errors.currencyRequired')); return false }
    if (!form.subcategoryId) { toast.error(t('movements.form.errors.subcategoryRequired')); return false }
    if (showMovementType && !form.movementType) { toast.error(t('movements.form.errors.typeRequired')); return false }
    if (form.payments.length === 0) { toast.error(t('movements.form.errors.paymentsRequired')); return false }
    for (const p of form.payments) {
      const pAmount = parseFloat(normalizeDecimal(p.amount))
      if (isNaN(pAmount) || pAmount <= 0) { toast.error(t('movements.form.errors.paymentAmountRequired')); return false }
      if (p.paymentMethodType === 'CashBox' && !p.cashBoxId) { toast.error(t('movements.form.errors.cashBoxRequired')); return false }
      if (p.paymentMethodType === 'BankAccount' && !p.bankAccountId) { toast.error(t('movements.form.errors.bankRequired')); return false }
      if (p.paymentMethodType === 'CreditCard' && !p.creditCardId) { toast.error(t('movements.form.errors.cardRequired')); return false }
      if (p.paymentMethodType === 'CreditCard' && !p.creditCardMemberId) { toast.error(t('movements.form.errors.cardMemberRequired')); return false }
      const installmentsNum = parseInt(p.installments)
      if (p.paymentMethodType === 'CreditCard' && (isNaN(installmentsNum) || installmentsNum < 1)) { toast.error(t('movements.form.errors.installmentsRequired')); return false }
    }
    const paymentSum = form.payments.reduce((sum, p) => sum + parseFloat(normalizeDecimal(p.amount) || '0'), 0)
    if (Math.abs(paymentSum - amount) > 0.009) {
      toast.error(t('movements.form.errors.paymentsMismatch', { sum: paymentSum.toFixed(2), total: amount.toFixed(2) }))
      return false
    }
    return true
  }

  const fetchRatesExplicit = async (
    currencyCode: string,
    date: string,
    silent = true,
    settingsOverride?: FamilySettingsDto | null,
  ) => {
    const settings = settingsOverride ?? familySettings
    if (!currencyCode || !date) return
    const pCurr = settings.primaryCurrencyCode
    const sCurr = settings.secondaryCurrencyCode
    const lockPrimary = currencyCode === pCurr
    const lockSecondary = currencyCode === sCurr
    setFetchingRate(true)
    try {
      const updates: Record<string, string> = {}
      if (!lockPrimary && pCurr && pCurr !== currencyCode) {
        const { data } = await api.get<{ rate: number }>('/exchange-rates', {
          params: { base_currency: currencyCode, target_currency: pCurr, date },
        })
        updates.primaryExchangeRate = String(data.rate)
      }
      if (!lockSecondary && sCurr && sCurr !== currencyCode) {
        const { data } = await api.get<{ rate: number }>('/exchange-rates', {
          params: { base_currency: currencyCode, target_currency: sCurr, date },
        })
        updates.secondaryExchangeRate = String(data.rate)
      }
      if (Object.keys(updates).length > 0) {
        setForm((p) => ({ ...p, ...updates }))
      }
    } catch {
      if (!silent) toast.error(t('common.fetchRateError'))
    } finally {
      setFetchingRate(false)
    }
  }

  const reloadForm = async () => {
    if (!id) return
    try {
      const { data } = await api.get<MovementDto>(`/movements/${id}`)
      setRowVersion(data.rowVersion)
      setAuditInfo({
        createdAt: data.createdAt,
        createdByName: data.createdByName ?? null,
        modifiedAt: data.modifiedAt ?? null,
        modifiedByName: data.modifiedByName ?? null,
      })
      skipAutoFetchRef.current = 1
      setForm({
        date: data.date,
        movementType: data.movementType,
        amount: data.amount.toFixed(2),
        currencyCode: data.currencyCode,
        primaryExchangeRate: String(data.primaryExchangeRate),
        secondaryExchangeRate: String(data.secondaryExchangeRate),
        description: data.description ?? '',
        subcategoryId: data.subcategoryId,
        accountingType: data.accountingType ?? '',
        isOrdinary: data.isOrdinary === null ? '' : data.isOrdinary ? 'true' : 'false',
        costCenterId: data.costCenterId ?? '',
        payments: data.payments.map((p) => ({
          paymentMethodType: p.paymentMethodType,
          amount: p.amount.toFixed(2),
          cashBoxId: p.cashBoxId ?? '',
          bankAccountId: p.bankAccountId ?? '',
          creditCardId: p.creditCardId ?? '',
          creditCardMemberId: p.creditCardMemberId ?? '',
          installments: p.installments ? String(p.installments) : '1',
          bonificationType: p.bonificationType ?? '',
          bonificationValue: p.bonificationValue ? String(p.bonificationValue) : '',
        })),
      })
    } catch {
      toast.error(t('movements.form.reloadError'))
    }
  }

  const handleConflictError = (err: unknown) => {
    if (axios.isAxiosError(err) && err.response?.status === 409) {
      toast.error(t('movements.form.concurrentModification'), {
        description: t('movements.form.concurrentModificationDesc'),
        action: { label: t('movements.form.reload'), onClick: reloadForm },
        duration: 10000,
      })
    } else {
      toast.error(extractError(err))
    }
  }

  const handleSave = async () => {
    if (!validate()) return
    setSaving(true)
    try {
      const payload = buildPayload()
      if (isEdit && id) {
        const { data } = await api.put<MovementDto>(`/movements/${id}`, payload)
        setRowVersion(data.rowVersion)
        toast.success(t('movements.form.updated'))
      } else {
        await api.post('/movements', payload)
        toast.success(t('movements.form.created'))
        if (isFromFrequent && fromFrequentId) {
          await api.post(`/frequent-movements/${fromFrequentId}/apply`, { movementDate: form.date }).catch(() => {})
          refreshNotifications()
        }
      }
      navigate('/movements')
    } catch (err) {
      handleConflictError(err)
    } finally {
      setSaving(false)
    }
  }

  const handleSaveAndNew = async () => {
    if (!validate()) return
    setSaving(true)
    try {
      const payload = buildPayload()
      if (isEdit && id) {
        const { data } = await api.put<MovementDto>(`/movements/${id}`, payload)
        setRowVersion(data.rowVersion)
        toast.success(t('movements.form.updated'))
      } else {
        await api.post('/movements', payload)
        toast.success(t('movements.form.created'))
        if (isFromFrequent && fromFrequentId) {
          await api.post(`/frequent-movements/${fromFrequentId}/apply`, { movementDate: form.date }).catch(() => {})
          refreshNotifications()
        }
      }
      // Resetear form con la misma moneda para carga en lote
      setForm(defaultForm(form.currencyCode))
      navigate('/movements/new')
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
    <div className="mx-auto max-w-5xl space-y-6 pb-10">
      {/* Header */}
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" onClick={() => navigate('/movements')}>
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <h1 className="text-2xl font-bold">
          {isEdit ? t('movements.form.editTitle') : isDuplicate ? t('movements.form.duplicateTitle') : isFromFrequent ? t('movements.form.newTitle') : t('movements.form.newTitle')}
        </h1>
      </div>

      <div className="space-y-6">
        {/* === Fila 1: Fecha | Tipo | Subcategoría === */}
        <div className="grid grid-cols-3 gap-4">
          <div className="space-y-1.5">
            <Label>{t('movements.form.date')}</Label>
            <Input
              type="date"
              value={form.date}
              onChange={(e) => {
                const newDate = e.target.value
                setForm((p) => ({ ...p, date: newDate }))
              }}
              max={formatDateISO(new Date())}
            />
          </div>

          <div className="space-y-1.5">
            <Label>{t('movements.form.type')}</Label>
            <Select
              value={form.movementType}
              onValueChange={(v) => setForm((p): MovementForm => {
                const newSub = subcategoryOptions.find(s => s.value === p.subcategoryId)
                const isValidForNewType = !newSub || newSub.subcategoryType === 'Both' || newSub.subcategoryType === v
                const payments = v === 'Income' ? stripCreditCardPayments(p.payments) : p.payments
                return {
                  ...p,
                  movementType: v as string,
                  subcategoryId: isValidForNewType ? p.subcategoryId : '',
                  payments,
                  amount: recomputeAmount(payments),
                }
              })}
              disabled={hasLockedPayments}
            >
              <SelectTrigger className="w-full">
                <SelectValue>
                  {{ Income: t('movements.form.income'), Expense: t('movements.form.expense'), '': t('movements.form.selectType') }[form.movementType]}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Income">{t('movements.form.income')}</SelectItem>
                <SelectItem value="Expense">{t('movements.form.expense')}</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-1.5">
            <Label>{t('movements.form.subcategory')}</Label>
            <ComboboxField
              id="subcategoryId"
              value={form.subcategoryId}
              onChange={(subcategoryId) => {
                const sub = subcategoryOptions.find((s) => s.value === subcategoryId)
                if (!sub) { setForm((p) => ({ ...p, subcategoryId })); return }
                const newMovementType: string = sub.subcategoryType === 'Both'
                  ? form.movementType
                  : (sub.subcategoryType === 'Income' ? 'Income' : 'Expense')
                setForm((p): MovementForm => {
                  const payments = newMovementType === 'Income' ? stripCreditCardPayments(p.payments) : p.payments
                  return {
                    ...p,
                    subcategoryId,
                    movementType: newMovementType,
                    accountingType: sub.suggestedAccountingType ?? '',
                    costCenterId: sub.suggestedCostCenterId ?? '',
                    isOrdinary: sub.isOrdinary === null ? '' : sub.isOrdinary ? 'true' : 'false',
                    payments,
                    amount: recomputeAmount(payments),
                  }
                })
              }}
              loadOptions={async () => {
                const filtered = subcategoryOptions.filter(s =>
                  (s.isActive || s.value === form.subcategoryId) &&
                  (!form.movementType || s.subcategoryType === form.movementType || s.subcategoryType === 'Both')
                )
                return filtered.map((s) => ({ value: s.value, label: `${s.group} › ${s.label}` }))
              }}
              placeholder={t('movements.form.searchSubcategory')}
            />
          </div>
        </div>

        {/* === Fila 2: Moneda + Importe === */}
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-1.5">
            <Label>{t('movements.form.currency')}</Label>
            <ComboboxField
              id="currencyCode"
              value={form.currencyCode}
              onChange={(v) => {
                const erSt = getExchangeRateState(v)
                setForm((p): MovementForm => ({
                  ...p,
                  currencyCode: v,
                  primaryExchangeRate: erSt.lockPrimary ? '1' : p.primaryExchangeRate,
                  secondaryExchangeRate: erSt.lockSecondary ? '1' : p.secondaryExchangeRate,
                  payments: p.payments.map(py => ({
                    ...py,
                    cashBoxId: '',
                    bankAccountId: '',
                    creditCardId: '',
                    creditCardMemberId: '',
                  })),
                }))
              }}
              loadOptions={async () => {
                const all = await loadFamilyCurrencyOptions()
                return all.filter(c => c.isActive || c.value === form.currencyCode)
              }}
              placeholder={t('movements.form.searchCurrency')}
              disabled={hasLockedPayments}
            />
          </div>

          <div className="space-y-1.5">
            <div className="flex items-center gap-1.5">
              <Label>{t('movements.form.totalAmount')}</Label>
              {form.currencyCode && (
                <TooltipProvider>
                  <Tooltip>
                    <TooltipTrigger>
                      <Info className="h-3.5 w-3.5 text-muted-foreground cursor-default shrink-0" />
                    </TooltipTrigger>
                    <TooltipContent side="top">
                      <div className="space-y-0.5">
                        {!ttIsPrimary && <p>TC {primaryCurrency}: {ttExRate}</p>}
                        {!ttIsPrimary && ttHasAmount && <p>{ttFmt(ttAmount * ttExRate)} {primaryCurrency}</p>}
                        {!ttIsSecondary && <p>TC {secondaryCurrency}: {ttSecRate}</p>}
                        {!ttIsSecondary && ttHasAmount && <p>{ttFmt(ttAmount * ttSecRate)} {secondaryCurrency}</p>}
                      </div>
                    </TooltipContent>
                  </Tooltip>
                </TooltipProvider>
              )}
            </div>
            <AmountInput
              value={form.amount}
              onChange={() => {}}
              disabled
              className="bg-muted"
            />
          </div>
        </div>


        {/* === Tipo de movimiento extra (si subcategoría es Both) === */}
        {showMovementType && (
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label>{t('movements.form.type')}</Label>
              <Select
                value={form.movementType}
                onValueChange={(v) => setForm((p): MovementForm => {
                  const payments = v === 'Income' ? stripCreditCardPayments(p.payments) : p.payments
                  return { ...p, movementType: v as string, payments, amount: recomputeAmount(payments) }
                })}
              >
                <SelectTrigger className="w-full">
                  <SelectValue>
                    {{ Income: t('movements.form.income'), Expense: t('movements.form.expense'), '': t('movements.form.selectType') }[form.movementType]}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Income">{t('movements.form.income')}</SelectItem>
                  <SelectItem value="Expense">{t('movements.form.expense')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        )}

        {/* === Descripción === */}
        <div className="space-y-1.5">
          <Label>{t('movements.form.description')}</Label>
          <Input
            value={form.description}
            onChange={(e) => setForm((p) => ({ ...p, description: e.target.value }))}
            placeholder={t('movements.form.descriptionPlaceholder')}
            maxLength={500}
          />
        </div>

        {/* === Clasificación === */}
        <div className="grid grid-cols-3 gap-4">
          <div className="space-y-1.5">
            <Label>{t('movements.form.accountingType')}</Label>
            <Select
              value={form.accountingType || '_none_'}
              onValueChange={(v) => {
                const val: string = v === '_none_' ? '' : (v as string)
                setForm((p): MovementForm => ({ ...p, accountingType: val }))
              }}
            >
              <SelectTrigger className="w-full">
                <SelectValue className={placeholderClass(!form.accountingType)}>
                  {{ _none_: t('movements.form.noAccountingType'), Asset: t('movements.form.asset'), Liability: t('movements.form.liability'), Income: t('movements.form.incomeAccounting'), Expense: t('movements.form.expenseAccounting') }[form.accountingType || '_none_'] ?? t('movements.form.noAccountingType')}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="_none_">{t('movements.form.noAccountingType')}</SelectItem>
                <SelectItem value="Asset">{t('movements.form.asset')}</SelectItem>
                <SelectItem value="Liability">{t('movements.form.liability')}</SelectItem>
                <SelectItem value="Income">{t('movements.form.incomeAccounting')}</SelectItem>
                <SelectItem value="Expense">{t('movements.form.expenseAccounting')}</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-1.5">
            <Label>{t('movements.form.character')}</Label>
            <Select
              value={form.isOrdinary || '_none_'}
              onValueChange={(v) => {
                const val: string = v === '_none_' ? '' : (v as string)
                setForm((p): MovementForm => ({ ...p, isOrdinary: val }))
              }}
            >
              <SelectTrigger className="w-full">
                <SelectValue className={placeholderClass(!form.isOrdinary)}>
                  {{ _none_: t('movements.form.noCharacter'), true: t('movements.form.ordinary'), false: t('movements.form.extraordinary') }[form.isOrdinary || '_none_'] ?? t('movements.form.noCharacter')}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="_none_">{t('movements.form.noCharacter')}</SelectItem>
                <SelectItem value="true">{t('movements.form.ordinary')}</SelectItem>
                <SelectItem value="false">{t('movements.form.extraordinary')}</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-1.5">
            <Label>{t('movements.form.costCenter')}</Label>
            <ComboboxField
              id="costCenterId"
              value={form.costCenterId}
              onChange={(v) => setForm((p) => ({ ...p, costCenterId: v }))}
              loadOptions={async () => {
                const all = await loadCostCentersNoCache()
                return all.filter(c => c.isActive || c.value === form.costCenterId)
              }}
              placeholder={t('movements.form.noCostCenter')}
            />
          </div>
        </div>

        {/* === Formas de pago === */}
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <Label className="text-base font-semibold">{t('movements.form.paymentsSection')}</Label>
            <Button variant="outline" size="sm" className="h-7 text-xs" onClick={addPayment}>
              <Plus className="mr-1 h-3 w-3" />
              {t('movements.form.addPayment')}
            </Button>
          </div>

          {form.payments.map((payment, idx) => (
            <Card key={idx} className={payment.locked ? 'opacity-70' : ''}>
              <CardContent className="p-3">
                <div className="grid grid-cols-3 gap-3">
                  {/* Método */}
                  <div className="space-y-1.5">
                    <div className="flex items-center gap-1">
                      <Label className="text-xs">{t('movements.form.method')}</Label>
                      {payment.locked && <Lock className="h-3 w-3 text-muted-foreground" />}
                    </div>
                    <Select
                      value={payment.paymentMethodType}
                      onValueChange={(v) => updatePayment(idx, { paymentMethodType: v as string, cashBoxId: '', bankAccountId: '', creditCardId: '', creditCardMemberId: '' })}
                      disabled={payment.locked}
                    >
                      <SelectTrigger className="h-8 w-full">
                        <SelectValue>
                          {{ CashBox: t('movements.form.cashBox'), BankAccount: t('movements.form.bank'), CreditCard: t('movements.form.creditCard') }[payment.paymentMethodType] ?? payment.paymentMethodType}
                        </SelectValue>
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="CashBox">{t('movements.form.cashBox')}</SelectItem>
                        <SelectItem value="BankAccount">{t('movements.form.bank')}</SelectItem>
                        {form.movementType !== 'Income' && (
                          <SelectItem value="CreditCard">{t('movements.form.creditCard')}</SelectItem>
                        )}
                      </SelectContent>
                    </Select>
                  </div>

                  {/* Entidad */}
                  <div className="space-y-1.5">
                    {payment.paymentMethodType === 'CashBox' && (
                      <>
                        <Label className="text-xs">{t('movements.form.cashBox')}</Label>
                        {filteredCashBoxes.length === 0 ? (
                          <p className="text-xs text-muted-foreground py-1">{t('movements.form.noCashBoxes', { currency: form.currencyCode || '—' })}</p>
                        ) : (
                          <Select value={payment.cashBoxId} onValueChange={(v) => updatePayment(idx, { cashBoxId: v as string })} disabled={payment.locked}>
                            <SelectTrigger className="h-8 w-full">
                              <SelectValue>
                                {payment.cashBoxId ? filteredCashBoxes.find((c) => c.value === payment.cashBoxId)?.label : t('movements.form.selectCashBox')}
                              </SelectValue>
                            </SelectTrigger>
                            <SelectContent>
                              {filteredCashBoxes.map((cb) => (
                                <SelectItem key={cb.value} value={cb.value}>{cb.label}</SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                        )}
                      </>
                    )}
                    {payment.paymentMethodType === 'BankAccount' && (
                      <>
                        <Label className="text-xs">{t('movements.form.bank')}</Label>
                        {filteredBankAccounts.length === 0 ? (
                          <p className="text-xs text-muted-foreground py-1">{t('movements.form.noBanks', { currency: form.currencyCode || '—' })}</p>
                        ) : (
                          <Select value={payment.bankAccountId} onValueChange={(v) => updatePayment(idx, { bankAccountId: v as string })} disabled={payment.locked}>
                            <SelectTrigger className="h-8 w-full">
                              <SelectValue>
                                {payment.bankAccountId ? filteredBankAccounts.find((b) => b.value === payment.bankAccountId)?.label : t('movements.form.selectBank')}
                              </SelectValue>
                            </SelectTrigger>
                            <SelectContent>
                              {filteredBankAccounts.map((ba) => (
                                <SelectItem key={ba.value} value={ba.value}>{ba.label}</SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                        )}
                      </>
                    )}
                    {payment.paymentMethodType === 'CreditCard' && (
                      <>
                        <Label className="text-xs">{t('movements.form.creditCard')}</Label>
                        {filteredCreditCards.length === 0 ? (
                          <p className="text-xs text-muted-foreground py-1">{t('movements.form.noCards', { currency: form.currencyCode || '—' })}</p>
                        ) : (
                          <Select value={payment.creditCardId} onValueChange={(v) => {
                            const card = creditCards.find((c) => c.value === v)
                            const currentUserMember = card?.members.find((m) => m.isCurrentUser)
                            updatePayment(idx, { creditCardId: v as string, creditCardMemberId: currentUserMember?.value ?? '' })
                          }} disabled={payment.locked}>
                            <SelectTrigger className="h-8 w-full">
                              <SelectValue>
                                {payment.creditCardId ? filteredCreditCards.find((c) => c.value === payment.creditCardId)?.label : t('movements.form.selectCard')}
                              </SelectValue>
                            </SelectTrigger>
                            <SelectContent>
                              {filteredCreditCards.map((cc) => (
                                <SelectItem key={cc.value} value={cc.value}>{cc.label}</SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                        )}
                      </>
                    )}
                  </div>

                  {/* Importe */}
                  <div className="space-y-1.5">
                    <div className="flex items-center justify-between">
                      <Label className="text-xs">{t('movements.form.amountLabel')}</Label>
                      {!payment.locked && (
                        <div className="flex gap-0.5">
                          <Button variant="ghost" size="icon" className="h-5 w-5" title={t('movements.form.duplicatePayment')} onClick={() => duplicatePayment(idx)}>
                            <Copy className="h-3 w-3" />
                          </Button>
                          {form.payments.length > 1 && (
                            <Button variant="ghost" size="icon" className="h-5 w-5" onClick={() => removePayment(idx)}>
                              <X className="h-3 w-3" />
                            </Button>
                          )}
                        </div>
                      )}
                    </div>
                    <AmountInput
                      value={payment.amount}
                      onChange={(v) => updatePayment(idx, { amount: v })}
                      disabled={payment.locked}
                      placeholder="0.00"
                      className="h-8"
                    />
                  </div>
                </div>

                {/* Fila extra CC: miembro + cuotas + bonificacion en una linea */}
                {payment.paymentMethodType === 'CreditCard' && payment.creditCardId && (
                  <div className="flex gap-3 mt-3">
                    <div className="space-y-1.5 w-1/3">
                      <Label className="text-xs">{t('movements.form.member')}</Label>
                      <Select
                        value={payment.creditCardMemberId || '_none_'}
                        onValueChange={(v) => updatePayment(idx, { creditCardMemberId: v === '_none_' ? '' : (v as string) })}
                        disabled={payment.locked}
                      >
                        <SelectTrigger className="h-8 w-full">
                          <SelectValue>
                            {payment.creditCardMemberId
                              ? creditCards.find((c) => c.value === payment.creditCardId)?.members.find((m) => m.value === payment.creditCardMemberId)?.label ?? t('movements.form.member')
                              : t('movements.form.noMember')}
                          </SelectValue>
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="_none_">{t('movements.form.noMember')}</SelectItem>
                          {creditCards.find((c) => c.value === payment.creditCardId)?.members.map((m) => (
                            <SelectItem key={m.value} value={m.value}>{m.label}</SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                    <div className="space-y-1.5 flex-1">
                      <Label className="text-xs">{t('movements.form.installments')}</Label>
                      <Input
                        inputMode="numeric"
                        value={payment.installments}
                        onChange={(e) => {
                          if (/^\d*$/.test(e.target.value)) updatePayment(idx, { installments: e.target.value })
                        }}
                        className="h-8"
                        maxLength={2}
                        disabled={payment.locked}
                      />
                    </div>
                    <div className="space-y-1.5 flex-1">
                      <Label className="text-xs">{t('movements.form.bonificationType')}</Label>
                      <Select
                        value={payment.bonificationType || '_none_'}
                        onValueChange={(v) => updatePayment(idx, {
                          bonificationType: v === '_none_' ? '' : v,
                          bonificationValue: v === '_none_' ? '' : payment.bonificationValue,
                        })}
                        disabled={payment.locked}
                      >
                        <SelectTrigger className="h-8 w-full">
                          <SelectValue>
                            {payment.bonificationType === 'Percentage' ? t('movements.form.bonifPercentage')
                              : payment.bonificationType === 'FixedAmount' ? t('movements.form.bonifFixed')
                              : t('movements.form.noBonification')}
                          </SelectValue>
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="_none_">{t('movements.form.noBonification')}</SelectItem>
                          <SelectItem value="Percentage">{t('movements.form.bonifPercentage')}</SelectItem>
                          <SelectItem value="FixedAmount">{t('movements.form.bonifFixed')}</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                    {payment.bonificationType && (
                      <div className="space-y-1.5 flex-1">
                        <Label className="text-xs">
                          {payment.bonificationType === 'Percentage'
                            ? t('movements.form.bonifValuePercent')
                            : t('movements.form.bonifValueAmount')}
                        </Label>
                        <Input
                          inputMode="decimal"
                          value={payment.bonificationValue}
                          onChange={(e) => {
                            const v = e.target.value
                            if (/^\d*[.,]?\d{0,2}$/.test(v)) updatePayment(idx, { bonificationValue: v })
                          }}
                          className="h-8"
                          placeholder={payment.bonificationType === 'Percentage' ? 'Ej: 20' : 'Ej: 5000'}
                          disabled={payment.locked}
                        />
                      </div>
                    )}
                  </div>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      </div>

      {/* Footer con botones */}
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
          <Button variant="outline" onClick={() => navigate('/movements')}>
            {t('movements.form.cancel')}
          </Button>
          <div className="flex gap-2">
            <Button variant="outline" disabled={saving} onClick={handleSaveAndNew} title="Ctrl+Shift+Enter">
              {saving ? t('movements.form.saving') : t('movements.form.saveAndNew')}
            </Button>
            <Button disabled={saving} onClick={handleSave} title="Ctrl+Enter">
              {saving ? t('movements.form.saving') : t('movements.form.save')}
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
