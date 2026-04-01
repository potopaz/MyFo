import { useState, useEffect } from 'react'
import { format } from 'date-fns'
import { ChevronLeft, ChevronRight, Download, Printer } from 'lucide-react'
import * as XLSX from 'xlsx'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import api from '@/lib/api'
import type { DrilldownMovementDto, DrilldownResultDto } from '@/types/api'

// ─── Props ────────────────────────────────────────────────────────────────────

interface DrilldownSheetProps {
  open: boolean
  onClose: () => void
  title: string
  dimension: string
  dimensionValue: string
  movementType?: string
  dateRange: { from?: Date; to?: Date }
  currency: string
  /** "YYYY-MM" — when set, filters to movements with a CC installment due in that month */
  installmentMonth?: string
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

function fmtDate(dateStr: string): string {
  try {
    return format(new Date(dateStr), 'dd/MM/yyyy')
  } catch {
    return dateStr
  }
}

function fmtAmount(v: number, currencyCode: string): string {
  if (Math.abs(v) >= 1_000_000) return `${currencyCode} ${(v / 1_000_000).toFixed(2)}M`
  return `${currencyCode} ${v.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

function ordLabel(v: boolean | null | undefined): string {
  if (v === true) return 'Ord.'
  if (v === false) return 'Extra.'
  return '–'
}

function movTypeLabel(mt: string): string {
  return mt === 'Income' ? 'Ingreso' : 'Gasto'
}

const PAGE_SIZE = 50

// ─── Fetch all items (for export / print) ────────────────────────────────────

async function fetchAllItems(params: Record<string, string | number | undefined>): Promise<DrilldownMovementDto[]> {
  const res = await api.get<DrilldownResultDto>('/reports/drilldown', {
    params: { ...params, page: 1, pageSize: 5000 },
  })
  return res.data.items
}

// ─── Excel export ─────────────────────────────────────────────────────────────

function computeNet(items: DrilldownMovementDto[]): number {
  return items.reduce((acc, item) => acc + (item.movementType === 'Income' ? item.amount : -item.amount), 0)
}

function fmtSigned(v: number, currencyCode: string): string {
  const sign = v >= 0 ? '+' : '–'
  const abs = Math.abs(v)
  if (abs >= 1_000_000) return `${sign} ${currencyCode} ${(abs / 1_000_000).toFixed(2)}M`
  return `${sign} ${currencyCode} ${abs.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

function exportToExcel(items: DrilldownMovementDto[], title: string) {
  const rows = items.map((item) => ({
    Fecha: fmtDate(item.date),
    Descripción: item.description ?? '',
    Categoría: item.categoryName,
    Subcategoría: item.subcategoryName,
    'Centro de Costo': item.costCenterName ?? '',
    Carácter: ordLabel(item.isOrdinary),
    Importe: item.amount,
    Moneda: item.currencyCode,
    Tipo: movTypeLabel(item.movementType),
  }))

  const ws = XLSX.utils.json_to_sheet(rows)
  // Column widths
  ws['!cols'] = [{ wch: 12 }, { wch: 35 }, { wch: 20 }, { wch: 22 }, { wch: 20 }, { wch: 10 }, { wch: 14 }, { wch: 8 }, { wch: 10 }]
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, 'Detalle')
  const safeName = title.replace(/[^\w\s-]/g, '').trim().replace(/\s+/g, '-').slice(0, 40)
  XLSX.writeFile(wb, `detalle-${safeName}-${format(new Date(), 'yyyy-MM-dd')}.xlsx`)
}

// ─── Print ────────────────────────────────────────────────────────────────────

function printItems(items: DrilldownMovementDto[], title: string, currency: string, totalAmount: number, netAmount: number) {
  const rows = items.map((item) => `
    <tr>
      <td>${fmtDate(item.date)}</td>
      <td>${item.description ?? '<em>Sin descripción</em>'}</td>
      <td>${item.categoryName} › ${item.subcategoryName}</td>
      <td>${item.costCenterName ?? '–'}</td>
      <td>${ordLabel(item.isOrdinary)}</td>
      <td class="num ${item.movementType === 'Expense' ? 'neg' : 'pos'}">
        ${item.movementType === 'Expense' ? '–' : '+'}${fmtAmount(item.amount, item.currencyCode)}
      </td>
    </tr>`).join('')

  const html = `<!DOCTYPE html><html><head><meta charset="utf-8">
    <title>Detalle: ${title}</title>
    <style>
      body { font-family: Arial, sans-serif; font-size: 11px; color: #111; margin: 20px; }
      h2 { font-size: 14px; margin-bottom: 4px; }
      p { font-size: 11px; color: #666; margin-bottom: 12px; }
      table { width: 100%; border-collapse: collapse; }
      th { text-align: left; border-bottom: 2px solid #333; padding: 4px 6px; font-size: 10px; }
      td { border-bottom: 1px solid #ddd; padding: 4px 6px; }
      .num { text-align: right; }
      .neg { color: #dc2626; }
      .pos { color: #16a34a; }
      tfoot td { border-top: 2px solid #333; border-bottom: none; font-weight: bold; }
    </style>
  </head><body>
    <h2>Detalle: ${title}</h2>
    <p>${items.length} movimientos · ${fmtAmount(totalAmount, currency)}</p>
    <table>
      <thead><tr>
        <th>Fecha</th><th>Descripción</th><th>Categoría / Subcategoría</th>
        <th>CC</th><th>Carácter</th><th class="num">Importe</th>
      </tr></thead>
      <tbody>${rows}</tbody>
      <tfoot><tr>
        <td colspan="4"></td>
        <td>Total</td>
        <td class="num ${netAmount >= 0 ? 'pos' : 'neg'}">${fmtSigned(netAmount, currency)}</td>
      </tr></tfoot>
    </table>
  </body></html>`

  const win = window.open('', '_blank', 'width=900,height=700')
  if (!win) return
  win.document.write(html)
  win.document.close()
  win.focus()
  win.print()
}

// ─── Component ────────────────────────────────────────────────────────────────

export function DrilldownSheet({
  open,
  onClose,
  title,
  dimension,
  dimensionValue,
  movementType,
  dateRange,
  currency,
  installmentMonth,
}: DrilldownSheetProps) {
  const [data, setData] = useState<DrilldownResultDto | null>(null)
  const [loading, setLoading] = useState(false)
  const [actionLoading, setActionLoading] = useState(false)
  const [page, setPage] = useState(1)

  useEffect(() => {
    setPage(1)
  }, [dimension, dimensionValue, movementType, dateRange, currency, installmentMonth])

  useEffect(() => {
    if (!open) return
    if (!dateRange.from || !dateRange.to) return
    if (!dimensionValue) return

    setLoading(true)
    const from = format(dateRange.from, 'yyyy-MM-dd')
    const to = format(dateRange.to, 'yyyy-MM-dd')

    api.get<DrilldownResultDto>('/reports/drilldown', {
      params: {
        from,
        to,
        currency,
        dimension,
        dimensionValue,
        ...(movementType ? { movementType } : {}),
        ...(installmentMonth ? { installmentMonth } : {}),
        page,
        pageSize: PAGE_SIZE,
      },
    }).then((res) => {
      setData(res.data)
    }).catch(() => {
      setData(null)
    }).finally(() => {
      setLoading(false)
    })
  }, [open, dateRange, currency, dimension, dimensionValue, movementType, installmentMonth, page])

  const totalPages = data ? Math.ceil(data.totalCount / PAGE_SIZE) : 1

  function buildParams() {
    if (!dateRange.from || !dateRange.to) return null
    return {
      from: format(dateRange.from, 'yyyy-MM-dd'),
      to: format(dateRange.to, 'yyyy-MM-dd'),
      currency,
      dimension,
      dimensionValue,
      ...(movementType ? { movementType } : {}),
      ...(installmentMonth ? { installmentMonth } : {}),
    }
  }

  async function handleExport() {
    const params = buildParams()
    if (!params || !data) return
    setActionLoading(true)
    try {
      const items = await fetchAllItems(params)
      exportToExcel(items, title)
    } finally {
      setActionLoading(false)
    }
  }

  async function handlePrint() {
    const params = buildParams()
    if (!params || !data) return
    setActionLoading(true)
    try {
      const items = await fetchAllItems(params)
      const net = computeNet(items)
      printItems(items, title, currency, data.totalAmount, net)
    } finally {
      setActionLoading(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={(isOpen) => { if (!isOpen) onClose() }}>
      <DialogContent
        className="sm:max-w-[1100px] w-full max-h-[85vh] flex flex-col gap-0 p-0 overflow-hidden"
        showCloseButton
      >
        <DialogHeader className="px-6 pt-5 pb-3 border-b shrink-0">
          <DialogTitle>Detalle: {title}</DialogTitle>
          {data && !loading && (
            <div className="flex items-center justify-between pt-1">
              <div className="flex gap-4 text-sm text-muted-foreground">
                <span>{data.totalCount} movimientos</span>
                <span>·</span>
                <span className="font-medium text-foreground">{fmtAmount(data.totalAmount, currency)}</span>
              </div>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleExport}
                  disabled={actionLoading}
                  className="h-7 text-xs gap-1.5"
                >
                  <Download className="h-3.5 w-3.5" />
                  Excel
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handlePrint}
                  disabled={actionLoading}
                  className="h-7 text-xs gap-1.5"
                >
                  <Printer className="h-3.5 w-3.5" />
                  Imprimir
                </Button>
              </div>
            </div>
          )}
        </DialogHeader>

        <div className="flex-1 overflow-y-auto px-6 py-2">
          {loading ? (
            <div className="space-y-2 py-2">
              {Array.from({ length: 8 }).map((_, i) => (
                <div key={i} className="flex items-center gap-3 py-2">
                  <Skeleton className="h-4 w-24 shrink-0" />
                  <Skeleton className="h-4 flex-1" />
                  <Skeleton className="h-4 w-20 shrink-0" />
                </div>
              ))}
            </div>
          ) : !data || data.items.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-16 text-muted-foreground">
              <p className="text-sm">Sin movimientos para este filtro</p>
            </div>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-xs text-muted-foreground">
                  <th className="text-left py-2 pr-3 font-medium whitespace-nowrap">Fecha</th>
                  <th className="text-left py-2 pr-3 font-medium">Descripción</th>
                  <th className="text-left py-2 pr-3 font-medium hidden sm:table-cell">Categoría / Subcategoría</th>
                  <th className="text-left py-2 pr-3 font-medium hidden lg:table-cell">CC</th>
                  <th className="text-left py-2 pr-3 font-medium hidden lg:table-cell">Carácter</th>
                  <th className="text-right py-2 font-medium">Importe</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((item) => (
                  <tr key={item.movementId} className="border-b last:border-0 hover:bg-muted/30 transition-colors">
                    <td className="py-2 pr-3 text-muted-foreground whitespace-nowrap text-xs">
                      {fmtDate(item.date)}
                    </td>
                    <td className="py-2 pr-3 max-w-[200px] truncate">
                      {item.description ?? <span className="text-muted-foreground italic">Sin descripción</span>}
                    </td>
                    <td className="py-2 pr-3 hidden sm:table-cell">
                      <span className="text-xs text-muted-foreground">{item.categoryName}</span>
                      <span className="text-muted-foreground"> › </span>
                      <span className="text-xs">{item.subcategoryName}</span>
                    </td>
                    <td className="py-2 pr-3 text-xs text-muted-foreground hidden lg:table-cell whitespace-nowrap">
                      {item.costCenterName ?? '–'}
                    </td>
                    <td className="py-2 pr-3 text-xs text-muted-foreground hidden lg:table-cell whitespace-nowrap">
                      {ordLabel(item.isOrdinary)}
                    </td>
                    <td className="py-2 text-right font-medium whitespace-nowrap">
                      <span className={item.movementType === 'Expense' ? 'text-rose-500' : 'text-emerald-600'}>
                        {item.movementType === 'Expense' ? '–' : '+'}{fmtAmount(item.amount, item.currencyCode)}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr className="border-t-2">
                  <td className="py-2 pr-3" />
                  <td className="py-2 pr-3 hidden sm:table-cell" />
                  <td className="py-2 pr-3 hidden sm:table-cell" />
                  <td className="py-2 pr-3 hidden lg:table-cell" />
                  <td className="py-2 pr-3 text-xs text-muted-foreground font-semibold hidden lg:table-cell">Total</td>
                  <td className="py-2 text-right font-bold text-sm whitespace-nowrap">
                    <span className={(data.netAmount ?? 0) >= 0 ? 'text-emerald-600' : 'text-rose-500'}>
                      {fmtSigned(data.netAmount ?? 0, currency)}
                    </span>
                  </td>
                </tr>
              </tfoot>
            </table>
          )}
        </div>

        {!loading && data && data.totalCount > PAGE_SIZE && (
          <div className="border-t px-6 py-3 flex items-center justify-between text-sm shrink-0">
            <span className="text-muted-foreground">
              Página {page} de {totalPages}
            </span>
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1}
              >
                <ChevronLeft className="h-4 w-4" />
                Anterior
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page >= totalPages}
              >
                Siguiente
                <ChevronRight className="h-4 w-4" />
              </Button>
            </div>
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
