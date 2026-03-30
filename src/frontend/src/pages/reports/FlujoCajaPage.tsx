import { useState, useEffect } from 'react'
import { format } from 'date-fns'
import type { DateRange } from 'react-day-picker'
import {
  ResponsiveContainer, Tooltip,
  ComposedChart, BarChart, PieChart,
  Bar, Line, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Legend,
} from 'recharts'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { ReportHeader, defaultDateRange } from '@/components/reports/ReportHeader'
import api from '@/lib/api'
import type {
  FamilySettingsDto,
  CashFlowReportDto,
} from '@/types/api'

// ─── Colors ──────────────────────────────────────────────────────────────────

const COLORS = ['#3b82f6','#818cf8','#38bdf8','#2dd4bf','#a78bfa','#34d399','#fbbf24','#fb923c','#f472b6','#94a3b8']
const EXPENSE_COLOR = '#f87171'
const INCOME_COLOR = '#3b82f6'
const NET_COLOR = '#34d399'
const EMERALD_COLOR = '#10b981'

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

function fmtPct(v: number) {
  return `${(v * 100).toFixed(0)}%`
}

// ─── Chart card ───────────────────────────────────────────────────────────────

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

// ─── Custom Tooltip ──────────────────────────────────────────────────────────

type TooltipProps = {
  active?: boolean
  payload?: Array<{ name: string; value: number; color: string; dataKey: string }>
  label?: string
}

function CustomTooltip({ active, payload, label }: TooltipProps): React.ReactNode {
  if (!active || !payload?.length) return null
  return (
    <div className="rounded-lg border bg-background p-2 shadow-md text-xs space-y-0.5">
      {label && <p className="font-semibold">{label}</p>}
      {payload.map((p) => (
        <p key={p.dataKey || p.name} style={{ color: p.color }}>
          {p.name}: {fmtShort(p.value)}
        </p>
      ))}
    </div>
  )
}

// ─── Loading skeleton ─────────────────────────────────────────────────────────

function LoadingSkeleton() {
  return (
    <div className="space-y-4">
      <div className="grid gap-4 lg:grid-cols-2">
        {[0, 1, 2, 3].map((i) => (
          <Card key={i}>
            <CardHeader className="pb-2"><Skeleton className="h-4 w-40" /></CardHeader>
            <CardContent><Skeleton className="h-[300px] w-full" /></CardContent>
          </Card>
        ))}
      </div>
    </div>
  )
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function FlujoCajaPage() {
  const [dateRange, setDateRange] = useState<DateRange>(defaultDateRange())
  const [currency, setCurrency] = useState('')
  const [primaryCurrency, setPrimaryCurrency] = useState('ARS')
  const [secondaryCurrency, setSecondaryCurrency] = useState('')
  const [data, setData] = useState<CashFlowReportDto | null>(null)
  const [loading, setLoading] = useState(true)

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

    api.get<CashFlowReportDto>('/reports/cashflow', {
      params: { from, to, currency },
    }).then((res) => {
      setData(res.data)
    }).catch(() => {
      setData(null)
    }).finally(() => {
      setLoading(false)
    })
  }, [dateRange, currency])

  // Payment method evolution: extract keys from first item
  const pmEvoKeys = data?.paymentMethodEvolution.length
    ? Object.keys(data.paymentMethodEvolution[0].values)
    : []
  const pmEvoData = (data?.paymentMethodEvolution ?? []).map((pt) => ({
    label: pt.label,
    ...pt.values,
  }))

  // Group futureInstallments by label
  const futureByLabel = (data?.futureInstallments ?? []).reduce<Record<string, Record<string, number>>>((acc, item) => {
    if (!acc[item.label]) acc[item.label] = {}
    acc[item.label][item.cardName] = (acc[item.label][item.cardName] ?? 0) + item.amount
    return acc
  }, {})
  const futureData = Object.entries(futureByLabel).map(([label, cards]) => ({ label, ...cards }))
  const cardNames = Array.from(new Set((data?.futureInstallments ?? []).map((f) => f.cardName)))

  const isEmpty = data && data.cashFlow.length === 0 && data.futureInstallments.length === 0

  return (
    <div className="space-y-4 pb-24">
      <ReportHeader
        title="Flujo de Caja"
        dateRange={dateRange}
        onDateRangeChange={setDateRange}
        currency={currency}
        onCurrencyChange={setCurrency}
        primaryCurrency={primaryCurrency}
        secondaryCurrency={secondaryCurrency}
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
          <div className="grid gap-4 lg:grid-cols-2">

            {/* Cash Flow */}
            <ChartCard title="Flujo de Caja" className="lg:col-span-2">
              <ResponsiveContainer width="100%" height={320}>
                <ComposedChart data={data?.cashFlow ?? []} barGap={0}>
                  <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
                  <XAxis dataKey="label" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
                  <YAxis yAxisId="bars" tickFormatter={fmtShort} tick={{ fontSize: 11 }} width={50} axisLine={false} tickLine={false} />
                  <YAxis yAxisId="line" orientation="right" tickFormatter={fmtShort} tick={{ fontSize: 11 }} width={55} axisLine={false} tickLine={false} />
                  <Tooltip content={<CustomTooltip />} cursor={{ fill: 'hsl(var(--muted))' }} />
                  <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
                  <Bar yAxisId="bars" dataKey="income" name="Ingresos" fill={INCOME_COLOR} radius={[3, 3, 0, 0]} />
                  <Bar yAxisId="bars" dataKey="expense" name="Egresos" fill={EXPENSE_COLOR} radius={[3, 3, 0, 0]} />
                  <Line yAxisId="line" dataKey="net" name="Neto" type="monotone" stroke={NET_COLOR} strokeWidth={2} dot={false} />
                </ComposedChart>
              </ResponsiveContainer>
              <p className="text-[11px] text-muted-foreground mt-1">Barras = flujo por período. Línea verde = neto (eje derecho).</p>
            </ChartCard>

            {/* Cuotas Futuras TC */}
            <ChartCard title="Cuotas Futuras por Tarjeta" className="lg:col-span-2">
              {futureData.length === 0 ? (
                <div className="flex items-center justify-center h-[200px] text-muted-foreground text-sm">
                  Sin cuotas futuras proyectadas
                </div>
              ) : (
                <ResponsiveContainer width="100%" height={300}>
                  <BarChart data={futureData} barCategoryGap="20%">
                    <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
                    <XAxis dataKey="label" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
                    <YAxis tickFormatter={fmtShort} tick={{ fontSize: 11 }} width={50} axisLine={false} tickLine={false} />
                    <Tooltip content={<CustomTooltip />} cursor={{ fill: 'hsl(var(--muted))' }} />
                    <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
                    {cardNames.map((name, i) => (
                      <Bar key={name} dataKey={name} stackId="a" fill={i === 0 ? EMERALD_COLOR : COLORS[i % COLORS.length]} radius={i === cardNames.length - 1 ? [3, 3, 0, 0] : undefined} />
                    ))}
                  </BarChart>
                </ResponsiveContainer>
              )}
            </ChartCard>

            {/* Medios de Pago */}
            <ChartCard title="Medios de Pago">
              <div className="flex items-center gap-6 pt-2">
                <ResponsiveContainer width={180} height={180}>
                  <PieChart>
                    <Pie
                      data={data?.paymentMethods ?? []}
                      dataKey="amount"
                      nameKey="name"
                      cx="50%" cy="50%"
                      innerRadius={50} outerRadius={80}
                      paddingAngle={3}
                      strokeWidth={0}
                    >
                      {(data?.paymentMethods ?? []).map((_, i) => (
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
                  {(data?.paymentMethods ?? []).map((pm, i) => (
                    <div key={pm.name} className="flex items-center gap-2">
                      <div className="w-3 h-3 rounded-sm shrink-0" style={{ background: COLORS[i % COLORS.length] }} />
                      <span className="text-muted-foreground">{pm.name}</span>
                      <span className="font-semibold ml-2">{fmtCurrency(pm.amount, currency)}</span>
                    </div>
                  ))}
                </div>
              </div>
            </ChartCard>

            {/* Evolución Medios de Pago 100% stacked */}
            <ChartCard title="Evolución Medios de Pago (% del total)">
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={pmEvoData} stackOffset="expand" barCategoryGap="20%">
                  <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
                  <XAxis dataKey="label" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
                  <YAxis tickFormatter={fmtPct} tick={{ fontSize: 11 }} width={40} axisLine={false} tickLine={false} />
                  <Tooltip content={<CustomTooltip />} />
                  <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
                  {pmEvoKeys.map((k, i) => (
                    <Bar key={k} dataKey={k} stackId="a" fill={COLORS[i % COLORS.length]} />
                  ))}
                </BarChart>
              </ResponsiveContainer>
            </ChartCard>

          </div>
        </div>
      )}
    </div>
  )
}
