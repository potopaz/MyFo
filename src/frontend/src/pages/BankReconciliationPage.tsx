import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { CheckCircle2 } from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Checkbox } from '@/components/ui/checkbox'
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
import api from '@/lib/api'
import axios from 'axios'
import type { BankAccountDto, BankReconciliationDto, BankReconciliationItemDto } from '@/types/api'

function extractError(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data
    if (data?.message) return data.message
  }
  return 'Ocurrió un error inesperado'
}

function fmt(n: number) {
  return n.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatDate(dateISO: string): string {
  const [year, month, day] = dateISO.split('-')
  return `${day}/${month}/${year}`
}

function getDefaultFrom(): string {
  const d = new Date()
  d.setDate(d.getDate() - 30)
  return d.toISOString().split('T')[0]
}

function getDefaultTo(): string {
  return new Date().toISOString().split('T')[0]
}

export default function BankReconciliationPage() {
  const { t } = useTranslation()

  const [bankAccounts, setBankAccounts] = useState<BankAccountDto[]>([])
  const [selectedBankId, setSelectedBankId] = useState('')
  const [from, setFrom] = useState(getDefaultFrom())
  const [to, setTo] = useState(getDefaultTo())
  const [showReconciled, setShowReconciled] = useState(false)

  const [data, setData] = useState<BankReconciliationDto | null>(null)
  const [loading, setLoading] = useState(false)

  // Local override of isReconciled per item (optimistic)
  const [reconciled, setReconciled] = useState<Record<string, boolean>>({})

  useEffect(() => {
    api.get<BankAccountDto[]>('/bankaccounts').then((r) => setBankAccounts(r.data))
  }, [])

  const loadData = useCallback(async () => {
    if (!selectedBankId) return
    setLoading(true)
    try {
      const params = new URLSearchParams()
      if (from) params.set('from', from)
      if (to) params.set('to', to)
      const { data: res } = await api.get<BankReconciliationDto>(
        `/bank-reconciliation/${selectedBankId}?${params}`
      )
      setData(res)
      // Initialize local state from server
      const initial: Record<string, boolean> = {}
      res.items.forEach((item) => {
        initial[item.id] = item.isReconciled
      })
      setReconciled(initial)
    } catch {
      toast.error('Error al cargar la conciliación')
    } finally {
      setLoading(false)
    }
  }, [selectedBankId, from, to])

  useEffect(() => {
    loadData()
  }, [loadData])

  const toggleItem = async (item: BankReconciliationItemDto) => {
    const newValue = !reconciled[item.id]
    setReconciled((prev) => ({ ...prev, [item.id]: newValue }))

    try {
      const body = { isReconciled: newValue }
      if (item.type === 'InitialBalance') {
        await api.patch(`/bank-reconciliation/${selectedBankId}/initial-balance`, body)
      } else if (item.type === 'MovementPayment') {
        await api.patch(`/bank-reconciliation/${selectedBankId}/movement-payment/${item.id}`, body)
      } else if (item.type === 'Transfer') {
        await api.patch(`/bank-reconciliation/${selectedBankId}/transfer/${item.id}`, body)
      } else if (item.type === 'CreditCardPayment') {
        await api.patch(`/bank-reconciliation/${selectedBankId}/credit-card-payment/${item.id}`, body)
      }
    } catch (err) {
      // Revert on error
      setReconciled((prev) => ({ ...prev, [item.id]: !newValue }))
      toast.error(extractError(err))
    }
  }

  // previousReconciledBalance (from server) already includes InitialBalance when it's reconciled
  // on the server. We need to adjust for local optimistic toggles of InitialBalance.
  const initItem = data?.items.find((i) => i.type === 'InitialBalance')
  const effectivePreviousBalance = (() => {
    if (!data || !initItem) return data?.previousReconciledBalance ?? 0
    const initAmount = (initItem.credit ?? 0) - (initItem.debit ?? 0)
    const serverIncluded = initItem.isReconciled
    const localIncluded = reconciled[initItem.id] ?? initItem.isReconciled
    let base = data.previousReconciledBalance
    if (serverIncluded && !localIncluded) base -= initAmount
    if (!serverIncluded && localIncluded) base += initAmount
    return base
  })()

  // Compute running reconciled balance (InitialBalance is already in effectivePreviousBalance)
  const computeRunningBalance = () => {
    if (!data) return {}
    const running: Record<string, number> = {}
    let balance = effectivePreviousBalance
    for (const item of data.items) {
      if (item.type === 'InitialBalance') continue // already in effectivePreviousBalance
      if (item.isOutsideDateRange) continue
      if (reconciled[item.id]) {
        balance += (item.credit ?? 0) - (item.debit ?? 0)
      }
      running[item.id] = balance
    }
    return running
  }

  const runningBalance = showReconciled ? computeRunningBalance() : {}

  // Filter items for display — use server-side isReconciled for visibility,
  // so items don't disappear until the next data reload.
  const outsideRange = data?.items.filter((i) => i.isOutsideDateRange && !i.isReconciled) ?? []
  const inRange = data?.items.filter((i) => !i.isOutsideDateRange) ?? []

  // If not showing reconciled, hide items that were already reconciled on load (server state)
  const visibleInRange = showReconciled
    ? inRange
    : inRange.filter((i) => !i.isReconciled)

  const totalReconciled = data
    ? effectivePreviousBalance +
      data.items.reduce((sum, item) => {
        if (item.type === 'InitialBalance') return sum // already in effectivePreviousBalance
        if (!reconciled[item.id]) return sum
        return sum + (item.credit ?? 0) - (item.debit ?? 0)
      }, 0)
    : 0

  const pendingCount = data
    ? data.items.filter((i) => !reconciled[i.id]).length
    : 0

  const selectedBank = bankAccounts.find((b) => b.bankAccountId === selectedBankId)

  const renderItem = (item: BankReconciliationItemDto, key: string) => {
    const isRec = reconciled[item.id] ?? item.isReconciled
    return (
      <TableRow key={key} className={isRec ? 'opacity-60' : ''}>
        <TableCell className="w-8 pl-3">
          <Checkbox
            checked={isRec}
            onCheckedChange={() => toggleItem(item)}
          />
        </TableCell>
        <TableCell className="text-sm text-muted-foreground w-24">
          {item.date ? formatDate(item.date) : '—'}
        </TableCell>
        <TableCell className="text-sm max-w-[280px] truncate">
          <span className="flex items-center gap-1.5">
            {isRec && <CheckCircle2 className="h-3 w-3 text-green-500 shrink-0" />}
            {item.description}
          </span>
        </TableCell>
        <TableCell className="text-right tabular-nums text-sm text-green-600 dark:text-green-400 w-28">
          {item.credit != null ? fmt(item.credit) : ''}
        </TableCell>
        <TableCell className="text-right tabular-nums text-sm text-red-600 dark:text-red-400 w-28">
          {item.debit != null ? fmt(item.debit) : ''}
        </TableCell>
        {showReconciled && (
          <TableCell className="text-right tabular-nums text-sm font-medium w-32">
            {isRec && runningBalance[item.id] != null ? fmt(runningBalance[item.id]) : ''}
          </TableCell>
        )}
      </TableRow>
    )
  }

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">{t('nav.bankReconciliation')}</h1>

      {/* Filters */}
      <Card>
        <CardContent className="flex flex-wrap items-end gap-3 p-3">
          <div className="flex flex-col gap-1">
            <Label className="text-xs">Banco</Label>
            <Select value={selectedBankId} onValueChange={setSelectedBankId}>
              <SelectTrigger className="h-8 w-56">
                <SelectValue>
                  {selectedBankId
                    ? (() => { const b = bankAccounts.find((x) => x.bankAccountId === selectedBankId); return b ? `${b.name} (${b.currencyCode})` : '' })()
                    : 'Seleccioná un banco'}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                {bankAccounts.filter((b) => b.isActive).map((b) => (
                  <SelectItem key={b.bankAccountId} value={b.bankAccountId}>
                    {b.name} ({b.currencyCode})
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="flex flex-col gap-1">
            <Label className="text-xs">{t('common.from')}</Label>
            <Input
              type="date"
              className="h-8 w-36"
              value={from}
              onChange={(e) => setFrom(e.target.value)}
            />
          </div>

          <div className="flex flex-col gap-1">
            <Label className="text-xs">{t('common.to')}</Label>
            <Input
              type="date"
              className="h-8 w-36"
              value={to}
              onChange={(e) => setTo(e.target.value)}
            />
          </div>

          <div className="flex flex-col gap-1">
            <Label className="text-xs opacity-0 select-none">_</Label>
            <div className="flex h-8 items-center gap-2">
              <Checkbox
                id="showReconciled"
                checked={showReconciled}
                onCheckedChange={(v) => setShowReconciled(!!v)}
              />
              <Label htmlFor="showReconciled" className="text-sm cursor-pointer">
                Mostrar conciliados
              </Label>
            </div>
          </div>
        </CardContent>
      </Card>

      {!selectedBankId && (
        <p className="py-8 text-center text-muted-foreground">
          Seleccioná un banco para ver los movimientos a conciliar.
        </p>
      )}

      {selectedBankId && loading && (
        <p className="py-8 text-center text-muted-foreground">{t('common.loading')}</p>
      )}

      {selectedBankId && !loading && data && (
        <>
          {/* Pending outside range */}
          {outsideRange.length > 0 && (
            <div className="rounded-md border border-yellow-200 dark:border-yellow-800">
              <div className="bg-yellow-50 dark:bg-yellow-950/40 px-4 py-2 text-sm font-medium text-yellow-800 dark:text-yellow-300">
                Pendientes fuera del período ({outsideRange.length})
              </div>
              <Table>
                <TableBody>
                  {outsideRange.map((item) => renderItem(item, `out-${item.id}`))}
                </TableBody>
              </Table>
            </div>
          )}

          {/* Main table */}
          <div className="rounded-md border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-8 pl-3" />
                  <TableHead className="w-24">Fecha</TableHead>
                  <TableHead>Descripción</TableHead>
                  <TableHead className="text-right w-28">Crédito</TableHead>
                  <TableHead className="text-right w-28">Débito</TableHead>
                  {showReconciled && (
                    <TableHead className="text-right w-32">
                      Saldo {selectedBank?.currencyCode ?? ''}
                    </TableHead>
                  )}
                </TableRow>
              </TableHeader>
              <TableBody>
                {/* Saldo inicial conciliado: always show when date filter is active */}
                {from && (
                  <TableRow className="bg-muted/30 hover:bg-muted/30">
                    <TableCell className="w-8 pl-3" />
                    <TableCell className="w-24 text-sm text-muted-foreground">—</TableCell>
                    <TableCell colSpan={showReconciled ? 4 : 3} className="text-sm text-muted-foreground italic">
                      Saldo inicial conciliado al {formatDate(from)}:{' '}
                      <span className="font-semibold not-italic tabular-nums">
                        {fmt(effectivePreviousBalance)}
                      </span>
                    </TableCell>
                  </TableRow>
                )}
                {visibleInRange.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={showReconciled ? 6 : 5} className="py-8 text-center text-muted-foreground">
                      No hay movimientos en el período seleccionado.
                    </TableCell>
                  </TableRow>
                ) : (
                  visibleInRange.map((item) => renderItem(item, item.id))
                )}
              </TableBody>
            </Table>
          </div>

          {/* Summary */}
          <div className="flex items-center justify-between rounded-md border bg-muted/30 px-4 py-3 text-sm">
            <span className="text-muted-foreground">
              {pendingCount > 0
                ? `${pendingCount} movimiento${pendingCount !== 1 ? 's' : ''} pendiente${pendingCount !== 1 ? 's' : ''}`
                : 'Todo conciliado ✓'}
            </span>
            <span className="font-semibold tabular-nums">
              Saldo conciliado: {fmt(totalReconciled)} {selectedBank?.currencyCode ?? ''}
            </span>
          </div>
        </>
      )}
    </div>
  )
}
