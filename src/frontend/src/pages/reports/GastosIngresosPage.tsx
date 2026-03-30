import { useState, useEffect } from 'react'
import { format } from 'date-fns'
import type { DateRange } from 'react-day-picker'
import {
  ResponsiveContainer, Tooltip,
  BarChart, AreaChart, PieChart,
  Bar, Area, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Legend,
  Treemap,
} from 'recharts'
import { TrendingUp, TrendingDown } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { ReportHeader, defaultDateRange } from '@/components/reports/ReportHeader'
import type { ActiveFilter } from '@/components/reports/ReportHeader'
import { DrilldownSheet } from '@/components/reports/DrilldownSheet'
import api from '@/lib/api'
import type {
  FamilySettingsDto,
  IncomeExpenseReportDto,
} from '@/types/api'

// ─── Colors ──────────────────────────────────────────────────────────────────

const COLORS = ['#3b82f6','#818cf8','#38bdf8','#2dd4bf','#a78bfa','#34d399','#fbbf24','#fb923c','#f472b6','#94a3b8']
const EXPENSE_COLOR = '#f87171'
const INCOME_COLOR = '#3b82f6'

// ─── Helpers ─────────────────────────────────────────────────────────────────

function fmtCurrency(v: number, currency: string) {
  if (Math.abs(v) >= 1_000_000) return `${currency} ${(v / 1_000_000).toFixed(1)}M`
  if (Math.abs(v) >= 1_000) return `${currency} ${(v / 1_000).toFixed(0)}k`
  return `${currency} ${v.toLocaleString('es-AR', { maximumFractionDigits: 0 })}`
}

function fmtShort(v: number) {
  if (Math.abs(v) >= 1_000_000) return `${(v / 1_000_000).toFixed(1)}M`
  if (Math.abs(v) >= 1_000) return `${(v / 1_000).toFixed(0)}k`
  return v.toLocaleString('es-AR', { maximumFractionDigits: 0 })
}

// ─── Chart card wrapper ───────────────────────────────────────────────────────

function ChartCard({ title, children, className }: { title: string; children: React.ReactNode; className?: string }) {
  return (
    <Card className={className}>
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-semibold">{title}</CardTitle>
      </CardHeader>
      <CardContent>{children}</CardContent>
    </Card>
  )
}

// ─── KPI card ────────────────────────────────────────────────────────────────

function KpiCard({ label, value, color, trend }: { label: string; value: string; color?: string; trend?: 'up' | 'down' }) {
  return (
    <Card>
      <CardContent className="pt-5 pb-4 px-5">
        <p className="text-xs text-muted-foreground font-medium mb-1.5">{label}</p>
        <p className="text-2xl font-bold" style={color ? { color } : undefined}>{value}</p>
        {trend && (
          <p className={`text-xs mt-1.5 flex items-center gap-1 ${trend === 'up' ? 'text-emerald-500' : 'text-rose-400'}`}>
            {trend === 'up' ? <TrendingUp className="h-3 w-3" /> : <TrendingDown className="h-3 w-3" />}
          </p>
        )}
      </CardContent>
    </Card>
  )
}

// ─── Custom Tooltip ──────────────────────────────────────────────────────────

type TooltipProps = {
  active?: boolean
  payload?: Array<{ name?: string; value?: number; color?: string; dataKey?: string }>
  label?: string
}

function CustomTooltip({ active, payload, label }: TooltipProps): React.ReactNode {
  if (!active || !payload?.length) return null
  return (
    <div className="rounded-lg border bg-background p-2 shadow-md text-xs space-y-0.5">
      {label && <p className="font-semibold">{label}</p>}
      {payload.map((p) => (
        <p key={p.dataKey ?? p.name ?? String(Math.random())} style={{ color: p.color }}>
          {p.name}: {fmtShort(p.value ?? 0)}
        </p>
      ))}
    </div>
  )
}

// ─── Treemap cell ─────────────────────────────────────────────────────────────

type TreemapContentProps = {
  x?: number
  y?: number
  width?: number
  height?: number
  name?: string
  value?: number
  index?: number
}

function TreemapCell(props: TreemapContentProps): React.ReactNode {
  const { x = 0, y = 0, width = 0, height = 0, name, value, index = 0 } = props
  if (!width || !height) return null
  const fill = COLORS[index % COLORS.length]
  return (
    <g>
      <rect
        x={x} y={y} width={width} height={height}
        fill={fill}
        stroke="hsl(var(--background))"
        strokeWidth={2}
        rx={4}
        style={{ cursor: 'pointer' }}
      />
      {width > 55 && height > 28 && (
        <>
          <text x={x + 6} y={y + 16} fill="#fff" fontSize={11} fontWeight={600}>{name}</text>
          <text x={x + 6} y={y + 30} fill="rgba(255,255,255,0.7)" fontSize={9}>{fmtShort(value ?? 0)}</text>
        </>
      )}
      {width > 35 && width <= 55 && height > 18 && (
        <text x={x + 4} y={y + 14} fill="#fff" fontSize={8} fontWeight={500}>{name}</text>
      )}
    </g>
  )
}

// ─── Loading skeleton ─────────────────────────────────────────────────────────

function LoadingSkeleton() {
  return (
    <div className="space-y-4">
      <div className="grid grid-cols-3 gap-3">
        {[0, 1, 2].map((i) => (
          <Card key={i}><CardContent className="pt-5 pb-4 px-5"><Skeleton className="h-4 w-24 mb-2" /><Skeleton className="h-8 w-32" /></CardContent></Card>
        ))}
      </div>
      <div className="grid gap-4 lg:grid-cols-2">
        {[0, 1, 2, 3].map((i) => (
          <Card key={i}><CardHeader className="pb-2"><Skeleton className="h-4 w-40" /></CardHeader><CardContent><Skeleton className="h-[300px] w-full" /></CardContent></Card>
        ))}
      </div>
    </div>
  )
}

// ─── Drilldown state type ─────────────────────────────────────────────────────

interface DrilldownState {
  open: boolean
  title: string
  dimension: string
  dimensionValue: string
  movementType?: string
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function GastosIngresosPage() {
  const [dateRange, setDateRange] = useState<DateRange>(defaultDateRange())
  const [currency, setCurrency] = useState('')
  const [primaryCurrency, setPrimaryCurrency] = useState('ARS')
  const [secondaryCurrency, setSecondaryCurrency] = useState('')
  const [data, setData] = useState<IncomeExpenseReportDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [activeFilters, setActiveFilters] = useState<Record<string, string>>({})
  const [drilldown, setDrilldown] = useState<DrilldownState>({ open: false, title: '', dimension: '', dimensionValue: '' })

  // Fetch family settings once
  useEffect(() => {
    api.get<FamilySettingsDto>('/family-settings').then((res) => {
      setPrimaryCurrency(res.data.primaryCurrencyCode)
      setSecondaryCurrency(res.data.secondaryCurrencyCode)
      setCurrency(res.data.primaryCurrencyCode)
    }).catch(() => {
      setCurrency('ARS')
    })
  }, [])

  // Fetch report data
  useEffect(() => {
    if (!currency) return
    if (!dateRange.from || !dateRange.to) return

    setLoading(true)
    const from = format(dateRange.from, 'yyyy-MM-dd')
    const to = format(dateRange.to, 'yyyy-MM-dd')

    api.get<IncomeExpenseReportDto>('/reports/income-expense', {
      params: { from, to, currency, ...activeFilters },
    }).then((res) => {
      setData(res.data)
    }).catch(() => {
      setData(null)
    }).finally(() => {
      setLoading(false)
    })
  }, [dateRange, currency, activeFilters])

  const filters: ActiveFilter[] = Object.entries(activeFilters).map(([key, value]) => ({
    key,
    label: `${key}: ${value}`,
  }))

  function removeFilter(key: string) {
    setActiveFilters((prev) => {
      const next = { ...prev }
      delete next[key]
      return next
    })
  }

  function openDrilldown(title: string, dimension: string, dimensionValue: string, movementType?: string) {
    setDrilldown({ open: true, title, dimension, dimensionValue, movementType })
  }

  const isEmpty = data && data.totalExpense === 0 && data.totalIncome === 0

  // Prepare treemap data (expenseByCategory → flat with name/size/color)
  const treemapData = (data?.expenseByCategory ?? []).map((c, i) => ({
    name: c.categoryName,
    size: c.amount,
    color: COLORS[i % COLORS.length],
  }))

  // Top 10 subcategories sorted desc
  const top10Subcats = [...(data?.expenseBySubcategory ?? [])]
    .sort((a, b) => b.amount - a.amount)
    .slice(0, 10)
    .reverse()

  // OrdVsExtra pie (filter 0s)
  const ordVsExtraData = [
    { name: 'Ordinario', value: data?.ordVsExtra.ordinary ?? 0 },
    { name: 'Extraordinario', value: data?.ordVsExtra.extraordinary ?? 0 },
    { name: 'Sin especificar', value: data?.ordVsExtra.unspecified ?? 0 },
  ].filter((d) => d.value > 0)

  // Category evolution: extract keys from first item
  const catEvoKeys = data?.categoryEvolution.length
    ? Object.keys(data.categoryEvolution[0].values)
    : []
  const catEvoData = (data?.categoryEvolution ?? []).map((pt) => ({
    label: pt.label,
    ...pt.values,
  }))

  // Resultado
  const resultado = (data?.totalIncome ?? 0) - (data?.totalExpense ?? 0)

  return (
    <div className="space-y-4 pb-24">
      <ReportHeader
        title="Gastos e Ingresos"
        dateRange={dateRange}
        onDateRangeChange={setDateRange}
        currency={currency}
        onCurrencyChange={setCurrency}
        primaryCurrency={primaryCurrency}
        secondaryCurrency={secondaryCurrency}
        activeFilters={filters}
        onRemoveFilter={removeFilter}
        onClearFilters={() => setActiveFilters({})}
      />

      {loading ? (
        <LoadingSkeleton />
      ) : isEmpty ? (
        <div className="flex flex-col items-center justify-center py-24 text-muted-foreground">
          <p className="text-lg font-medium">Sin datos para el período seleccionado</p>
          <p className="text-sm mt-1">Probá cambiando el rango de fechas o la moneda.</p>
        </div>
      ) : (
        <div className="space-y-4">
          {/* KPIs */}
          <div className="grid grid-cols-3 gap-3">
            <KpiCard label="Total Gastos" value={fmtCurrency(data?.totalExpense ?? 0, currency)} color={EXPENSE_COLOR} />
            <KpiCard label="Total Ingresos" value={fmtCurrency(data?.totalIncome ?? 0, currency)} color={INCOME_COLOR} />
            <KpiCard
              label="Resultado"
              value={fmtCurrency(resultado, currency)}
              color={resultado >= 0 ? '#34d399' : EXPENSE_COLOR}
              trend={resultado >= 0 ? 'up' : 'down'}
            />
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            {/* Top 10 Subcategorías */}
            <ChartCard title="Top 10 Subcategorías de Gasto">
              <ResponsiveContainer width="100%" height={320}>
                <BarChart data={top10Subcats} layout="vertical" margin={{ left: 10, right: 10 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" horizontal={false} />
                  <XAxis type="number" tickFormatter={fmtShort} tick={{ fontSize: 11 }} axisLine={false} />
                  <YAxis type="category" dataKey="name" width={90} tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
                  <Tooltip content={<CustomTooltip />} cursor={{ fill: 'hsl(var(--muted))' }} />
                  <Bar
                    dataKey="amount"
                    name="Gasto"
                    fill={EXPENSE_COLOR}
                    radius={[0, 4, 4, 0]}
                    barSize={18}
                    onClick={(entry: { name: string; amount: number }) => {
                      openDrilldown(entry.name, 'subcategory', entry.name, 'Expense')
                    }}
                    style={{ cursor: 'pointer' }}
                  />
                </BarChart>
              </ResponsiveContainer>
            </ChartCard>

            {/* Ordinario vs Extraordinario */}
            <ChartCard title="Ordinario vs Extraordinario">
              <div className="flex items-center gap-6 pt-2">
                <ResponsiveContainer width={180} height={180}>
                  <PieChart>
                    <Pie
                      data={ordVsExtraData}
                      dataKey="value"
                      cx="50%" cy="50%"
                      innerRadius={50} outerRadius={80}
                      paddingAngle={3}
                      strokeWidth={0}
                      onClick={(entry: { name: string }) => {
                        openDrilldown(entry.name, 'ordinary', entry.name, 'Expense')
                      }}
                      style={{ cursor: 'pointer' }}
                    >
                      {ordVsExtraData.map((_, i) => (
                        <Cell key={i} fill={COLORS[i % COLORS.length]} />
                      ))}
                    </Pie>
                    <Tooltip
                      formatter={(v: number) => [fmtCurrency(v, currency), '']}
                      contentStyle={{ fontSize: 12, borderRadius: 8 }}
                    />
                  </PieChart>
                </ResponsiveContainer>
                <div className="space-y-2 text-sm">
                  {ordVsExtraData.map((d, i) => (
                    <div key={d.name} className="flex items-center gap-2">
                      <div className="w-3 h-3 rounded-sm shrink-0" style={{ background: COLORS[i % COLORS.length] }} />
                      <span className="text-muted-foreground">{d.name}</span>
                      <span className="font-semibold ml-2">{fmtCurrency(d.value, currency)}</span>
                    </div>
                  ))}
                </div>
              </div>
            </ChartCard>

            {/* Gastos por Categoría - Treemap */}
            <ChartCard title="Gastos por Categoría" className="lg:col-span-2">
              <ResponsiveContainer width="100%" height={300}>
                <Treemap
                  data={treemapData}
                  dataKey="size"
                  content={<TreemapCell />}
                >
                  <Tooltip
                    formatter={(v: number) => [fmtCurrency(v, currency), 'Gasto']}
                    contentStyle={{ fontSize: 12, borderRadius: 8 }}
                  />
                </Treemap>
              </ResponsiveContainer>
            </ChartCard>

            {/* Evolución de Gastos por Categoría */}
            <ChartCard title="Evolución de Gastos por Categoría" className="lg:col-span-2">
              <ResponsiveContainer width="100%" height={320}>
                <BarChart data={catEvoData} barCategoryGap="20%">
                  <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
                  <XAxis dataKey="label" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
                  <YAxis tickFormatter={fmtShort} tick={{ fontSize: 11 }} width={50} axisLine={false} tickLine={false} />
                  <Tooltip content={<CustomTooltip />} cursor={{ fill: 'hsl(var(--muted))' }} />
                  <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
                  {catEvoKeys.map((k, i) => (
                    <Bar
                      key={k}
                      dataKey={k}
                      stackId="a"
                      fill={COLORS[i % COLORS.length]}
                      onClick={() => {
                        openDrilldown(k, 'category', k, 'Expense')
                      }}
                      style={{ cursor: 'pointer' }}
                    />
                  ))}
                </BarChart>
              </ResponsiveContainer>
            </ChartCard>

            {/* Fuentes de Ingreso */}
            <ChartCard title="Fuentes de Ingreso">
              <div className="flex items-center gap-6 pt-2">
                <ResponsiveContainer width={180} height={180}>
                  <PieChart>
                    <Pie
                      data={data?.incomeBySource ?? []}
                      dataKey="amount"
                      nameKey="name"
                      cx="50%" cy="50%"
                      innerRadius={50} outerRadius={80}
                      paddingAngle={3}
                      strokeWidth={0}
                      onClick={(entry: { name: string }) => {
                        openDrilldown(entry.name, 'subcategory', entry.name, 'Income')
                      }}
                      style={{ cursor: 'pointer' }}
                    >
                      {(data?.incomeBySource ?? []).map((_, i) => (
                        <Cell key={i} fill={COLORS[i % COLORS.length]} />
                      ))}
                    </Pie>
                    <Tooltip
                      formatter={(v: number) => [fmtCurrency(v, currency), '']}
                      contentStyle={{ fontSize: 12, borderRadius: 8 }}
                    />
                  </PieChart>
                </ResponsiveContainer>
                <div className="space-y-2 text-sm">
                  {(data?.incomeBySource ?? []).map((s, i) => (
                    <div key={s.name} className="flex items-center gap-2">
                      <div className="w-3 h-3 rounded-sm shrink-0" style={{ background: COLORS[i % COLORS.length] }} />
                      <span className="text-muted-foreground">{s.name}</span>
                      <span className="font-semibold ml-2">{fmtCurrency(s.amount, currency)}</span>
                    </div>
                  ))}
                </div>
              </div>
            </ChartCard>

            {/* Evolución de Ingresos */}
            <ChartCard title="Evolución de Ingresos">
              <ResponsiveContainer width="100%" height={300}>
                <AreaChart data={data?.incomeEvolution ?? []}>
                  <defs>
                    <linearGradient id="gradIncome" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor={INCOME_COLOR} stopOpacity={0.3} />
                      <stop offset="95%" stopColor={INCOME_COLOR} stopOpacity={0} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
                  <XAxis dataKey="label" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
                  <YAxis tickFormatter={fmtShort} tick={{ fontSize: 11 }} width={50} axisLine={false} tickLine={false} />
                  <Tooltip content={<CustomTooltip />} cursor={{ fill: 'hsl(var(--muted))' }} />
                  <Area
                    dataKey="amount"
                    name="Ingresos"
                    type="monotone"
                    fill="url(#gradIncome)"
                    stroke={INCOME_COLOR}
                    strokeWidth={2}
                    dot={{ r: 3, fill: INCOME_COLOR }}
                  />
                </AreaChart>
              </ResponsiveContainer>
            </ChartCard>
          </div>
        </div>
      )}

      <DrilldownSheet
        open={drilldown.open}
        onClose={() => setDrilldown((prev) => ({ ...prev, open: false }))}
        title={drilldown.title}
        dimension={drilldown.dimension}
        dimensionValue={drilldown.dimensionValue}
        movementType={drilldown.movementType}
        dateRange={dateRange}
        currency={currency}
      />
    </div>
  )
}
