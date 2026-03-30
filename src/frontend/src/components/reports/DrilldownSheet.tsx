import { useState, useEffect } from 'react'
import { format } from 'date-fns'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import api from '@/lib/api'
import type { DrilldownResultDto } from '@/types/api'

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
  if (Math.abs(v) >= 1_000) return `${currencyCode} ${v.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
  return `${currencyCode} ${v.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

function movementTypeLabel(mt: string): string {
  if (mt === 'Income') return 'Ingreso'
  if (mt === 'Expense') return 'Gasto'
  return mt
}

const PAGE_SIZE = 50

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
}: DrilldownSheetProps) {
  const [data, setData] = useState<DrilldownResultDto | null>(null)
  const [loading, setLoading] = useState(false)
  const [page, setPage] = useState(1)

  // Reset page when filter changes
  useEffect(() => {
    setPage(1)
  }, [dimension, dimensionValue, movementType, dateRange, currency])

  // Fetch drilldown data
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
  }, [open, dateRange, currency, dimension, dimensionValue, movementType, page])

  const totalPages = data ? Math.ceil(data.totalCount / PAGE_SIZE) : 1

  return (
    <Sheet open={open} onOpenChange={(isOpen: boolean) => { if (!isOpen) onClose() }}>
      <SheetContent side="right" className="w-full sm:max-w-2xl flex flex-col overflow-hidden">
        <SheetHeader>
          <SheetTitle>Detalle: {title}</SheetTitle>
          {data && !loading && (
            <div className="flex gap-4 text-sm text-muted-foreground pt-1">
              <span>{data.totalCount} movimientos</span>
              <span>·</span>
              <span className="font-medium text-foreground">{fmtAmount(data.totalAmount, currency)}</span>
            </div>
          )}
        </SheetHeader>

        <div className="flex-1 overflow-y-auto mt-2">
          {loading ? (
            <div className="space-y-2 px-1">
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
                  <th className="text-left py-2 pr-3 font-medium">Fecha</th>
                  <th className="text-left py-2 pr-3 font-medium">Descripción</th>
                  <th className="text-left py-2 pr-3 font-medium hidden sm:table-cell">Categoría</th>
                  <th className="text-left py-2 pr-3 font-medium hidden md:table-cell">Subcategoría</th>
                  <th className="text-right py-2 pr-3 font-medium">Importe</th>
                  <th className="text-left py-2 font-medium hidden sm:table-cell">Tipo</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((item) => (
                  <tr key={item.movementId} className="border-b last:border-0 hover:bg-muted/40 transition-colors">
                    <td className="py-2 pr-3 text-muted-foreground whitespace-nowrap">
                      {fmtDate(item.date)}
                    </td>
                    <td className="py-2 pr-3 max-w-[180px] truncate">
                      {item.description ?? <span className="text-muted-foreground italic">Sin descripción</span>}
                    </td>
                    <td className="py-2 pr-3 text-muted-foreground hidden sm:table-cell whitespace-nowrap">
                      {item.categoryName}
                    </td>
                    <td className="py-2 pr-3 text-muted-foreground hidden md:table-cell whitespace-nowrap">
                      {item.subcategoryName}
                    </td>
                    <td className="py-2 pr-3 text-right font-medium whitespace-nowrap">
                      <span className={item.movementType === 'Expense' ? 'text-rose-500' : 'text-emerald-600'}>
                        {item.movementType === 'Expense' ? '-' : '+'}{fmtAmount(item.amount, item.currencyCode)}
                      </span>
                    </td>
                    <td className="py-2 text-xs text-muted-foreground hidden sm:table-cell whitespace-nowrap">
                      {movementTypeLabel(item.movementType)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        {/* Pagination */}
        {!loading && data && data.totalCount > PAGE_SIZE && (
          <div className="border-t pt-3 flex items-center justify-between text-sm shrink-0">
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
      </SheetContent>
    </Sheet>
  )
}
