import { startOfMonth } from 'date-fns'
import type { DateRange } from 'react-day-picker'
import { DateRangePicker } from '@/components/ui/date-range-picker'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { X } from 'lucide-react'

export interface ActiveFilter {
  key: string
  label: string
}

interface ReportHeaderProps {
  title: string
  dateRange: DateRange
  onDateRangeChange: (range: DateRange) => void
  currency: string
  onCurrencyChange: (currency: string) => void
  primaryCurrency: string
  secondaryCurrency: string
  activeFilters?: ActiveFilter[]
  onRemoveFilter?: (key: string) => void
  onClearFilters?: () => void
}

export function ReportHeader({
  title,
  dateRange,
  onDateRangeChange,
  currency,
  onCurrencyChange,
  primaryCurrency,
  secondaryCurrency,
  activeFilters = [],
  onRemoveFilter,
  onClearFilters,
}: ReportHeaderProps) {
  return (
    <div className="space-y-2">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-bold">{title}</h1>
        <div className="flex items-center gap-2">
          <Select value={currency} onValueChange={(v) => v && onCurrencyChange(v)}>
            <SelectTrigger className="w-24">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={primaryCurrency}>{primaryCurrency}</SelectItem>
              {secondaryCurrency && secondaryCurrency !== primaryCurrency && (
                <SelectItem value={secondaryCurrency}>{secondaryCurrency}</SelectItem>
              )}
            </SelectContent>
          </Select>
          <DateRangePicker value={dateRange} onChange={onDateRangeChange} />
        </div>
      </div>

      {activeFilters.length > 0 && (
        <div className="flex flex-wrap items-center gap-2">
          <span className="text-xs text-muted-foreground">Filtros:</span>
          {activeFilters.map((f) => (
            <Badge key={f.key} variant="secondary" className="gap-1 pr-1">
              {f.label}
              {onRemoveFilter && (
                <button
                  type="button"
                  onClick={() => onRemoveFilter(f.key)}
                  className="ml-0.5 rounded-full hover:bg-muted-foreground/20 p-0.5"
                >
                  <X className="h-3 w-3" />
                </button>
              )}
            </Badge>
          ))}
          {onClearFilters && (
            <Button variant="ghost" size="sm" className="h-6 text-xs px-2" onClick={onClearFilters}>
              Limpiar
            </Button>
          )}
        </div>
      )}
    </div>
  )
}

export function defaultDateRange(): DateRange {
  return { from: startOfMonth(new Date()), to: new Date() }
}
