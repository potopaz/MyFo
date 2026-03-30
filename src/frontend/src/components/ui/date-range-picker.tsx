import { useState } from 'react'
import { format, startOfMonth, endOfMonth, subMonths, startOfYear } from 'date-fns'
import { es } from 'date-fns/locale'
import { DayPicker } from 'react-day-picker'
import type { DateRange } from 'react-day-picker'
import { Popover } from '@base-ui/react/popover'
import { Button } from '@/components/ui/button'
import { CalendarDays } from 'lucide-react'
import { cn } from '@/lib/utils'

const PRESETS = [
  { label: 'Este mes', fn: (): DateRange => ({ from: startOfMonth(new Date()), to: new Date() }) },
  {
    label: 'Mes anterior',
    fn: (): DateRange => {
      const d = subMonths(new Date(), 1)
      return { from: startOfMonth(d), to: endOfMonth(d) }
    },
  },
  { label: 'Trimestre', fn: (): DateRange => ({ from: subMonths(new Date(), 3), to: new Date() }) },
  { label: 'Semestre', fn: (): DateRange => ({ from: subMonths(new Date(), 6), to: new Date() }) },
  { label: 'Este año', fn: (): DateRange => ({ from: startOfYear(new Date()), to: new Date() }) },
]

function formatRange(range: DateRange): string {
  if (!range.from) return 'Seleccionar período'
  if (!range.to) return format(range.from, 'd MMM yyyy', { locale: es })

  const sameYear = range.from.getFullYear() === range.to.getFullYear()
  const isFullMonth =
    sameYear &&
    range.from.getMonth() === range.to.getMonth() &&
    range.from.getDate() === 1 &&
    range.to.getDate() === endOfMonth(range.from).getDate()

  if (isFullMonth) return format(range.from, 'MMM yyyy', { locale: es })
  if (sameYear)
    return `${format(range.from, 'd MMM', { locale: es })} – ${format(range.to, 'd MMM yyyy', { locale: es })}`
  return `${format(range.from, 'd MMM yyyy', { locale: es })} – ${format(range.to, 'd MMM yyyy', { locale: es })}`
}

interface DateRangePickerProps {
  value: DateRange
  onChange: (range: DateRange) => void
  className?: string
}

export function DateRangePicker({ value, onChange, className }: DateRangePickerProps) {
  const [open, setOpen] = useState(false)
  const [pending, setPending] = useState<DateRange>(value)

  const handleOpenChange = (isOpen: boolean) => {
    if (isOpen) setPending(value)
    setOpen(isOpen)
  }

  const handleApply = () => {
    if (pending.from) {
      onChange({ from: pending.from, to: pending.to ?? pending.from })
    }
    setOpen(false)
  }

  return (
    <Popover.Root open={open} onOpenChange={handleOpenChange}>
      <Popover.Trigger
        render={
          <Button variant="outline" className={cn('gap-2 font-normal', className)}>
            <CalendarDays className="h-4 w-4 shrink-0" />
            <span className="capitalize">{formatRange(value)}</span>
          </Button>
        }
      />
      <Popover.Portal>
        <Popover.Positioner side="bottom" align="end" sideOffset={6} className="z-50">
          <Popover.Popup className="rounded-lg border bg-popover text-popover-foreground shadow-lg">
            <div className="flex flex-col sm:flex-row">
              {/* Presets panel */}
              <div className="border-b sm:border-b-0 sm:border-r p-3 flex flex-row sm:flex-col gap-1 sm:w-38">
                <p className="hidden sm:block text-xs font-medium text-muted-foreground px-2 pb-1">Período</p>
                {PRESETS.map((p) => (
                  <button
                    key={p.label}
                    type="button"
                    onClick={() => setPending(p.fn())}
                    className="text-left text-sm px-2 py-1.5 rounded-md hover:bg-accent hover:text-accent-foreground transition-colors whitespace-nowrap"
                  >
                    {p.label}
                  </button>
                ))}
              </div>

              {/* Calendar + actions */}
              <div className="p-3">
                <DayPicker
                  mode="range"
                  selected={pending}
                  onSelect={(r) => r && setPending(r)}
                  numberOfMonths={2}
                  locale={es}
                  defaultMonth={pending.from ? subMonths(pending.from, 1) : subMonths(new Date(), 1)}
                />
                <div className="flex justify-end gap-2 pt-3 mt-1 border-t">
                  <Button variant="ghost" size="sm" onClick={() => setOpen(false)}>
                    Cancelar
                  </Button>
                  <Button size="sm" onClick={handleApply} disabled={!pending.from}>
                    Aplicar
                  </Button>
                </div>
              </div>
            </div>
          </Popover.Popup>
        </Popover.Positioner>
      </Popover.Portal>
    </Popover.Root>
  )
}
