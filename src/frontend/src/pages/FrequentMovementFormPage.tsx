import { useCallback, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
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
import { Switch } from '@/components/ui/switch'
import { ArrowLeft } from 'lucide-react'
import { AuditInfo } from '@/components/ui/audit-info'
import api from '@/lib/api'
import axios from 'axios'
import { loadFamilyCurrencyOptions } from '@/lib/currency-options'
import { loadCostCenterOptions, clearCostCenterCache } from '@/lib/costcenter-options'
import { loadSubcategoryOptions, clearSubcategoryCache, type SubcategoryOption } from '@/lib/subcategory-options'
import { loadCashBoxOptions, loadBankAccountOptions, loadCreditCardOptions, type PaymentEntityOption, type CreditCardOption } from '@/lib/payment-entities'
import type { FrequentMovementDto } from '@/types/api'

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

interface FrequentMovementForm {
  name: string
  frequencyMonths: string
  movementType: string
  amount: string
  currencyCode: string
  description: string
  subcategoryId: string
  accountingType: string
  isOrdinary: string
  costCenterId: string
  paymentMethodType: string
  cashBoxId: string
  bankAccountId: string
  creditCardId: string
  creditCardMemberId: string
  nextDueDate: string
  isActive: boolean
}

const defaultForm = (isEdit: boolean): FrequentMovementForm => {
  const today = isEdit ? '' : (() => { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}` })()
  return {
    name: '',
    frequencyMonths: '1',
    movementType: '',
    amount: '',
    currencyCode: '',
    description: '',
    subcategoryId: '',
    accountingType: '',
    isOrdinary: '',
    costCenterId: '',
    paymentMethodType: 'CashBox',
    cashBoxId: '',
    bankAccountId: '',
    creditCardId: '',
    creditCardMemberId: '',
    nextDueDate: today,
    isActive: true,
  }
}

export default function FrequentMovementFormPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { id } = useParams<{ id: string }>()
  const isEdit = !!id

  const [form, setForm] = useState<FrequentMovementForm>(() => defaultForm(isEdit))
  const [saving, setSaving] = useState(false)
  const [loading, setLoading] = useState(true)
  const [rowVersion, setRowVersion] = useState<number | null>(null)
  const [auditInfo, setAuditInfo] = useState<{ createdAt: string; createdByName: string | null; modifiedAt: string | null; modifiedByName: string | null } | null>(null)

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

  useEffect(() => {
    const init = async () => {
      setLoading(true)
      await loadOptions()
      if (isEdit && id) {
        try {
          const { data } = await api.get<FrequentMovementDto>(`/frequent-movements/${id}`)
          setRowVersion(data.rowVersion)
          setAuditInfo({
            createdAt: data.createdAt,
            createdByName: data.createdByName ?? null,
            modifiedAt: data.modifiedAt ?? null,
            modifiedByName: data.modifiedByName ?? null,
          })
          setForm({
            name: data.name,
            frequencyMonths: String(data.frequencyMonths),
            movementType: data.movementType,
            amount: data.amount > 0 ? data.amount.toFixed(2) : '',
            currencyCode: data.currencyCode,
            description: data.description ?? '',
            subcategoryId: data.subcategoryId,
            accountingType: data.accountingType ?? '',
            isOrdinary: data.isOrdinary === null ? '' : data.isOrdinary ? 'true' : 'false',
            costCenterId: data.costCenterId ?? '',
            paymentMethodType: data.paymentMethodType,
            cashBoxId: data.cashBoxId ?? '',
            bankAccountId: data.bankAccountId ?? '',
            creditCardId: data.creditCardId ?? '',
            creditCardMemberId: data.creditCardMemberId ?? '',
            nextDueDate: data.nextDueDate ?? '',
            isActive: data.isActive,
          })
        } catch (err) {
          toast.error(extractError(err))
          navigate('/frequent-movements')
        }
      }
      setLoading(false)
    }
    init()
  }, [id, isEdit, loadOptions, navigate])

  const selectedSub = subcategoryOptions.find((s) => s.value === form.subcategoryId)
  const showMovementType = selectedSub?.subcategoryType === 'Both'
  const filteredCashBoxes = cashBoxes.filter((c) => c.currencyCode === form.currencyCode && (c.isActive !== false || c.value === form.cashBoxId))
  const filteredBankAccounts = bankAccounts.filter((b) => b.currencyCode === form.currencyCode && (b.isActive !== false || b.value === form.bankAccountId))
  const filteredCreditCards = creditCards.filter((c) => c.currencyCode === form.currencyCode && (c.isActive !== false || c.value === form.creditCardId))
  const placeholderClass = (isPlaceholder: boolean) => isPlaceholder ? 'text-muted-foreground' : ''

  const reloadForm = async () => {
    if (!id) return
    try {
      const { data } = await api.get<FrequentMovementDto>(`/frequent-movements/${id}`)
      setRowVersion(data.rowVersion)
      setForm({
        name: data.name,
        frequencyMonths: String(data.frequencyMonths),
        movementType: data.movementType,
        amount: data.amount > 0 ? data.amount.toFixed(2) : '',
        currencyCode: data.currencyCode,
        description: data.description ?? '',
        subcategoryId: data.subcategoryId,
        accountingType: data.accountingType ?? '',
        isOrdinary: data.isOrdinary === null ? '' : data.isOrdinary ? 'true' : 'false',
        costCenterId: data.costCenterId ?? '',
        paymentMethodType: data.paymentMethodType,
        cashBoxId: data.cashBoxId ?? '',
        bankAccountId: data.bankAccountId ?? '',
        creditCardId: data.creditCardId ?? '',
        creditCardMemberId: data.creditCardMemberId ?? '',
        nextDueDate: data.nextDueDate ?? '',
        isActive: data.isActive,
      })
    } catch {
      toast.error(t('frequentMovements.form.reloadError'))
    }
  }

  const handleConflictError = (err: unknown) => {
    if (axios.isAxiosError(err) && err.response?.status === 409) {
      toast.error(t('frequentMovements.form.concurrentModification'), {
        description: t('frequentMovements.form.concurrentModificationDesc'),
        action: { label: t('frequentMovements.form.reload'), onClick: reloadForm },
        duration: 10000,
      })
    } else {
      toast.error(extractError(err))
    }
  }

  const validate = (): boolean => {
    if (!form.name.trim()) { toast.error(t('frequentMovements.form.errors.nameRequired')); return false }
    const freq = parseInt(form.frequencyMonths)
    if (!form.frequencyMonths || isNaN(freq) || freq < 1) { toast.error(t('frequentMovements.form.errors.frequencyRequired')); return false }
    if (!form.subcategoryId) { toast.error(t('frequentMovements.form.errors.subcategoryRequired')); return false }
    if (showMovementType && !form.movementType) { toast.error(t('frequentMovements.form.errors.typeRequired')); return false }
    return true
  }

  const buildPayload = () => ({
    name: form.name.trim(),
    frequencyMonths: parseInt(form.frequencyMonths),
    movementType: showMovementType ? form.movementType : null,
    amount: form.amount ? parseFloat(normalizeDecimal(form.amount)) : 0,
    currencyCode: form.currencyCode || null,
    description: form.description.trim() || null,
    subcategoryId: form.subcategoryId,
    accountingType: form.accountingType || null,
    isOrdinary: form.isOrdinary === '' ? null : form.isOrdinary === 'true',
    costCenterId: form.costCenterId || null,
    paymentMethodType: form.paymentMethodType || null,
    cashBoxId: form.paymentMethodType === 'CashBox' && form.cashBoxId ? form.cashBoxId : null,
    bankAccountId: form.paymentMethodType === 'BankAccount' && form.bankAccountId ? form.bankAccountId : null,
    creditCardId: form.paymentMethodType === 'CreditCard' && form.creditCardId ? form.creditCardId : null,
    creditCardMemberId: form.paymentMethodType === 'CreditCard' && form.creditCardMemberId ? form.creditCardMemberId : null,
    nextDueDate: form.nextDueDate || null,
    isActive: form.isActive,
    clientRowVersion: isEdit ? rowVersion : null,
  })

  const handleSave = async () => {
    if (!validate()) return
    setSaving(true)
    try {
      const payload = buildPayload()
      if (isEdit && id) {
        const { data } = await api.put<FrequentMovementDto>(`/frequent-movements/${id}`, payload)
        setRowVersion(data.rowVersion)
        toast.success(t('frequentMovements.form.updated'))
      } else {
        await api.post('/frequent-movements', payload)
        toast.success(t('frequentMovements.form.created'))
      }
      navigate('/frequent-movements')
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
    <div className="mx-auto max-w-3xl space-y-6 pb-10">
      {/* Header */}
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" onClick={() => navigate('/frequent-movements')}>
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <h1 className="text-2xl font-bold">
          {isEdit ? t('frequentMovements.form.editTitle') : t('frequentMovements.form.newTitle')}
        </h1>
      </div>

      <div className="space-y-6">
        {/* Nombre + Frecuencia + Próximo vencimiento */}
        <div className="grid grid-cols-[1fr_auto_auto] gap-4 items-end">
          <div className="space-y-1.5">
            <Label>{t('frequentMovements.form.name')}</Label>
            <Input
              value={form.name}
              onChange={(e) => setForm((p) => ({ ...p, name: e.target.value }))}
              placeholder={t('frequentMovements.form.namePlaceholder')}
              maxLength={100}
            />
          </div>
          <div className="space-y-1.5">
            <Label className="text-sm">{t('frequentMovements.form.frequencyMonths')}</Label>
            <Input
              inputMode="numeric"
              value={form.frequencyMonths}
              onChange={(e) => {
                if (/^\d*$/.test(e.target.value)) setForm((p) => ({ ...p, frequencyMonths: e.target.value }))
              }}
              placeholder="1"
              maxLength={2}
              className="w-20 text-center"
            />
          </div>
          <div className="space-y-1.5">
            <Label>{t('frequentMovements.form.nextDueDate')}</Label>
            <Input
              type="date"
              value={form.nextDueDate}
              onChange={(e) => setForm((p) => ({ ...p, nextDueDate: e.target.value }))}
            />
          </div>
        </div>

        {/* Tipo + Subcategoría */}
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-1.5">
            <Label>{t('frequentMovements.form.type')}</Label>
            <Select
              value={form.movementType}
              onValueChange={(v) => setForm((p): FrequentMovementForm => {
                const newSub = subcategoryOptions.find(s => s.value === p.subcategoryId)
                const isValidForNewType = !newSub || newSub.subcategoryType === 'Both' || newSub.subcategoryType === v
                return { ...p, movementType: v, subcategoryId: isValidForNewType ? p.subcategoryId : '' }
              })}
            >
              <SelectTrigger className="w-full">
                <SelectValue>
                  {{ Income: t('frequentMovements.form.income'), Expense: t('frequentMovements.form.expense'), '': t('frequentMovements.form.selectType') }[form.movementType]}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Income">{t('frequentMovements.form.income')}</SelectItem>
                <SelectItem value="Expense">{t('frequentMovements.form.expense')}</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-1.5">
            <Label>{t('frequentMovements.form.subcategory')}</Label>
            <ComboboxField
              id="subcategoryId"
              value={form.subcategoryId}
              onChange={(subcategoryId) => {
                const sub = subcategoryOptions.find((s) => s.value === subcategoryId)
                if (!sub) { setForm((p) => ({ ...p, subcategoryId })); return }
                const newMovementType: string = sub.subcategoryType === 'Both'
                  ? form.movementType
                  : (sub.subcategoryType === 'Income' ? 'Income' : 'Expense')
                setForm((p): FrequentMovementForm => ({
                  ...p,
                  subcategoryId,
                  movementType: newMovementType,
                  accountingType: sub.suggestedAccountingType ?? '',
                  costCenterId: sub.suggestedCostCenterId ?? '',
                  isOrdinary: sub.isOrdinary === null ? '' : sub.isOrdinary ? 'true' : 'false',
                }))
              }}
              loadOptions={async () => {
                const filtered = subcategoryOptions.filter(s =>
                  (s.isActive || s.value === form.subcategoryId) &&
                  (!form.movementType || s.subcategoryType === form.movementType || s.subcategoryType === 'Both')
                )
                return filtered.map((s) => ({ value: s.value, label: `${s.group} › ${s.label}` }))
              }}
              placeholder={t('frequentMovements.form.searchSubcategory')}
            />
          </div>
        </div>

        {/* Moneda + Importe */}
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-1.5">
            <Label>{t('frequentMovements.form.currency')}</Label>
            <ComboboxField
              id="currencyCode"
              value={form.currencyCode}
              onChange={(v) => setForm((p): FrequentMovementForm => ({
                ...p,
                currencyCode: v,
                cashBoxId: '',
                bankAccountId: '',
                creditCardId: '',
                creditCardMemberId: '',
              }))}
              loadOptions={async () => {
                const all = await loadFamilyCurrencyOptions()
                return all.filter(c => c.isActive || c.value === form.currencyCode)
              }}
              placeholder={t('frequentMovements.form.searchCurrency')}
            />
          </div>
          <div className="space-y-1.5">
            <Label>{t('frequentMovements.form.amount')}</Label>
            <AmountInput
              value={form.amount}
              onChange={(v) => setForm((p) => ({ ...p, amount: v }))}
              placeholder="0.00"
            />
          </div>
        </div>

        {/* Descripción */}
        <div className="space-y-1.5">
          <Label>{t('frequentMovements.form.description')}</Label>
          <Input
            value={form.description}
            onChange={(e) => setForm((p) => ({ ...p, description: e.target.value }))}
            placeholder={t('frequentMovements.form.descriptionPlaceholder')}
            maxLength={500}
          />
        </div>

        {/* Clasificación */}
        <div className="grid grid-cols-3 gap-4">
          <div className="space-y-1.5">
            <Label>{t('frequentMovements.form.accountingType')}</Label>
            <Select
              value={form.accountingType || '_none_'}
              onValueChange={(v) => setForm((p): FrequentMovementForm => ({ ...p, accountingType: v === '_none_' ? '' : v }))}
            >
              <SelectTrigger className="w-full">
                <SelectValue className={placeholderClass(!form.accountingType)}>
                  {{ _none_: t('frequentMovements.form.noAccountingType'), Asset: t('frequentMovements.form.asset'), Liability: t('frequentMovements.form.liability'), Income: t('frequentMovements.form.incomeAccounting'), Expense: t('frequentMovements.form.expenseAccounting') }[form.accountingType || '_none_'] ?? t('frequentMovements.form.noAccountingType')}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="_none_">{t('frequentMovements.form.noAccountingType')}</SelectItem>
                <SelectItem value="Asset">{t('frequentMovements.form.asset')}</SelectItem>
                <SelectItem value="Liability">{t('frequentMovements.form.liability')}</SelectItem>
                <SelectItem value="Income">{t('frequentMovements.form.incomeAccounting')}</SelectItem>
                <SelectItem value="Expense">{t('frequentMovements.form.expenseAccounting')}</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-1.5">
            <Label>{t('frequentMovements.form.character')}</Label>
            <Select
              value={form.isOrdinary || '_none_'}
              onValueChange={(v) => setForm((p): FrequentMovementForm => ({ ...p, isOrdinary: v === '_none_' ? '' : v }))}
            >
              <SelectTrigger className="w-full">
                <SelectValue className={placeholderClass(!form.isOrdinary)}>
                  {{ _none_: t('frequentMovements.form.noCharacter'), true: t('frequentMovements.form.ordinary'), false: t('frequentMovements.form.extraordinary') }[form.isOrdinary || '_none_'] ?? t('frequentMovements.form.noCharacter')}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="_none_">{t('frequentMovements.form.noCharacter')}</SelectItem>
                <SelectItem value="true">{t('frequentMovements.form.ordinary')}</SelectItem>
                <SelectItem value="false">{t('frequentMovements.form.extraordinary')}</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-1.5">
            <Label>{t('frequentMovements.form.costCenter')}</Label>
            <ComboboxField
              id="costCenterId"
              value={form.costCenterId}
              onChange={(v) => setForm((p) => ({ ...p, costCenterId: v }))}
              loadOptions={async () => {
                const all = await loadCostCentersNoCache()
                return all.filter(c => c.isActive || c.value === form.costCenterId)
              }}
              placeholder={t('frequentMovements.form.noCostCenter')}
            />
          </div>
        </div>

        {/* Forma de pago */}
        <Card>
          <CardContent className="p-4 space-y-4">
            <Label className="text-base font-semibold">{t('frequentMovements.form.paymentSection')}</Label>

            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label className="text-xs">{t('frequentMovements.form.method')}</Label>
                <Select
                  value={form.paymentMethodType}
                  onValueChange={(v) => setForm((p): FrequentMovementForm => ({
                    ...p,
                    paymentMethodType: v,
                    cashBoxId: '',
                    bankAccountId: '',
                    creditCardId: '',
                    creditCardMemberId: '',
                  }))}
                >
                  <SelectTrigger className="w-full">
                    <SelectValue>
                      {{ CashBox: t('frequentMovements.form.cashBox'), BankAccount: t('frequentMovements.form.bank'), CreditCard: t('frequentMovements.form.creditCard') }[form.paymentMethodType] ?? form.paymentMethodType}
                    </SelectValue>
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="CashBox">{t('frequentMovements.form.cashBox')}</SelectItem>
                    <SelectItem value="BankAccount">{t('frequentMovements.form.bank')}</SelectItem>
                    <SelectItem value="CreditCard">{t('frequentMovements.form.creditCard')}</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-1.5">
                {form.paymentMethodType === 'CashBox' && (
                  <>
                    <Label className="text-xs">{t('frequentMovements.form.cashBox')}</Label>
                    {filteredCashBoxes.length === 0 ? (
                      <p className="text-xs text-muted-foreground py-1">{t('frequentMovements.form.noCashBoxes', { currency: form.currencyCode || '—' })}</p>
                    ) : (
                      <Select value={form.cashBoxId} onValueChange={(v) => setForm((p) => ({ ...p, cashBoxId: v }))}>
                        <SelectTrigger className="w-full">
                          <SelectValue>
                            {form.cashBoxId ? filteredCashBoxes.find((c) => c.value === form.cashBoxId)?.label : t('frequentMovements.form.selectCashBox')}
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
                {form.paymentMethodType === 'BankAccount' && (
                  <>
                    <Label className="text-xs">{t('frequentMovements.form.bank')}</Label>
                    {filteredBankAccounts.length === 0 ? (
                      <p className="text-xs text-muted-foreground py-1">{t('frequentMovements.form.noBanks', { currency: form.currencyCode || '—' })}</p>
                    ) : (
                      <Select value={form.bankAccountId} onValueChange={(v) => setForm((p) => ({ ...p, bankAccountId: v }))}>
                        <SelectTrigger className="w-full">
                          <SelectValue>
                            {form.bankAccountId ? filteredBankAccounts.find((b) => b.value === form.bankAccountId)?.label : t('frequentMovements.form.selectBank')}
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
                {form.paymentMethodType === 'CreditCard' && (
                  <>
                    <Label className="text-xs">{t('frequentMovements.form.creditCard')}</Label>
                    {filteredCreditCards.length === 0 ? (
                      <p className="text-xs text-muted-foreground py-1">{t('frequentMovements.form.noCards', { currency: form.currencyCode || '—' })}</p>
                    ) : (
                      <Select value={form.creditCardId} onValueChange={(v) => setForm((p) => ({ ...p, creditCardId: v, creditCardMemberId: '' }))}>
                        <SelectTrigger className="w-full">
                          <SelectValue>
                            {form.creditCardId ? filteredCreditCards.find((c) => c.value === form.creditCardId)?.label : t('frequentMovements.form.selectCard')}
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
            </div>

            {/* Miembro (solo CC) */}
            {form.paymentMethodType === 'CreditCard' && form.creditCardId && (
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1.5">
                  <Label className="text-xs">{t('frequentMovements.form.member')}</Label>
                  <Select
                    value={form.creditCardMemberId || '_none_'}
                    onValueChange={(v) => setForm((p) => ({ ...p, creditCardMemberId: v === '_none_' ? '' : v }))}
                  >
                    <SelectTrigger className="w-full">
                      <SelectValue>
                        {form.creditCardMemberId
                          ? creditCards.find((c) => c.value === form.creditCardId)?.members.find((m) => m.value === form.creditCardMemberId)?.label ?? t('frequentMovements.form.member')
                          : t('frequentMovements.form.noMember')}
                      </SelectValue>
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="_none_">{t('frequentMovements.form.noMember')}</SelectItem>
                      {creditCards.find((c) => c.value === form.creditCardId)?.members.map((m) => (
                        <SelectItem key={m.value} value={m.value}>{m.label}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Activo (solo en edición) */}
        {isEdit && (
          <div className="flex items-center gap-3">
            <Switch
              checked={form.isActive}
              onCheckedChange={(v) => setForm((p) => ({ ...p, isActive: v }))}
            />
            <Label>{t('frequentMovements.form.isActive')}</Label>
          </div>
        )}
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
          <Button variant="outline" onClick={() => navigate('/frequent-movements')}>
            {t('frequentMovements.form.cancel')}
          </Button>
          <Button disabled={saving} onClick={handleSave}>
            {saving ? t('frequentMovements.form.saving') : t('frequentMovements.form.save')}
          </Button>
        </div>
      </div>
    </div>
  )
}
