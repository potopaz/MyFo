import { useState, useEffect } from 'react'
import { format } from 'date-fns'
import type { DateRange } from 'react-day-picker'
import {
  ResponsiveContainer, Tooltip,
  BarChart, PieChart,
  Bar, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Legend,
} from 'recharts'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { ReportHeader, defaultDateRange } from '@/components/reports/ReportHeader'
import api from '@/lib/api'
import type {
  FamilySettingsDto,
  CardsCCReportDto,
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

// ─── KPI card ────────────────────────────────────────────────────────────────

function KpiCard({ label, value, sub, color }: { label: string; value: string; sub?: string; color?: string }) {
  return (
    <Card>
      <CardContent className="pt-5 pb-4 px-5">
        <p className="text-xs text-muted-foreground font-medium mb-1.5">{label}</p>
        <p className="text-2xl font-bold" style={color ? { color } : undefined}>{value}</p>
        {sub && <p className="text-xs text-muted-foreground mt-1">{sub}</p>}
      </CardContent>
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
      <div className="grid grid-cols-3 gap-3">
        {[0, 1, 2].map((i) => (
          <Card key={i}><CardContent className="pt-5 pb-4 px-5"><Skeleton className="h-4 w-24 mb-2" /><Skeleton className="h-8 w-32" /></CardContent></Card>
        ))}
      </div>
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

export default function TarjetasCCPage() {
  const [dateRange, setDateRange] = useState<DateRange>(defaultDateRange())
  const [currency, setCurrency] = useState('')
  const [primaryCurrency, setPrimaryCurrency] = useState('ARS')
  const [secondaryCurrency, setSecondaryCurrency] = useState('')
  const [data, setData] = useState<CardsCCReportDto | null>(null)
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

    api.get<CardsCCReportDto>('/reports/cards-cc', {
      params: { from, to, currency },
    }).then((res) => {
      setData(res.data)
    }).catch(() => {
      setData(null)
    }).finally(() => {
      setLoading(false)
    })
  }, [dateRange, currency])

  // Cost center evolution: extract keys
  const ccEvoKeys = data?.costCenterEvolution.length
    ? Object.keys(data.costCenterEvolution[0].values)
    : []
  const ccEvoData = (data?.costCenterEvolution ?? []).map((pt) => ({
    label: pt.label,
    ...pt.values,
  }))

  // installmentsByCard sorted for horizontal bar
  const cardsSorted = [...(data?.installmentsByCard ?? [])].sort((a, b) => b.totalDebt - a.totalDebt)

  const netCharges = data?.chargesVsBonifications.net ?? 0

  const isEmpty = data && data.totalDebt === 0 && data.totalPaid === 0

  return (
    <div className="space-y-4 pb-24">
      <ReportHeader
        title="Tarjetas de Crédito"
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
          {/* KPIs */}
          <div className="grid grid-cols-3 gap-3">
            <KpiCard
              label="Deuda Total TC"
              value={fmtCurrency(data?.totalDebt ?? 0, currency)}
              color={EXPENSE_COLOR}
            />
            <KpiCard
              label="Pagado en período"
              value={fmtCurrency(data?.totalPaid ?? 0, currency)}
              color={INCOME_COLOR}
            />
            <KpiCard
              label="Cargos vs Bonificaciones (neto)"
              value={fmtCurrency(netCharges, currency)}
              sub={`$${fmtShort(data?.chargesVsBonifications.totalCharges ?? 0)} cargos - $${fmtShort(data?.chargesVsBonifications.totalBonifications ?? 0)} bonif.`}
              color={netCharges <= 0 ? '#34d399' : EXPENSE_COLOR}
            />
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            {/* Deuda por Tarjeta */}
            <ChartCard title="Deuda por Tarjeta">
              {cardsSorted.length === 0 ? (
                <div className="flex items-center justify-center h-[200px] text-muted-foreground text-sm">
                  Sin deuda pendiente
                </div>
              ) : (
                <ResponsiveContainer width="100%" height={Math.max(200, cardsSorted.length * 48 + 40)}>
                  <BarChart data={cardsSorted} layout="vertical" margin={{ left: 10, right: 10 }}>
                    <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" horizontal={false} />
                    <XAxis type="number" tickFormatter={fmtShort} tick={{ fontSize: 11 }} axisLine={false} />
                    <YAxis type="category" dataKey="cardName" width={100} tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
                    <Tooltip content={<CustomTooltip />} cursor={{ fill: 'hsl(var(--muted))' }} />
                    <Bar dataKey="totalDebt" name="Deuda" fill={EXPENSE_COLOR} radius={[0, 4, 4, 0]} barSize={20} />
                  </BarChart>
                </ResponsiveContainer>
              )}
            </ChartCard>

            {/* Gastos por Centro de Costo */}
            <ChartCard title="Gastos por Centro de Costo">
              <div className="flex items-center gap-6 pt-2">
                <ResponsiveContainer width={180} height={180}>
                  <PieChart>
                    <Pie
                      data={data?.byCostCenter ?? []}
                      dataKey="amount"
                      nameKey="name"
                      cx="50%" cy="50%"
                      innerRadius={50} outerRadius={80}
                      paddingAngle={3}
                      strokeWidth={0}
                    >
                      {(data?.byCostCenter ?? []).map((_, i) => (
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
                  {(data?.byCostCenter ?? []).map((cc, i) => (
                    <div key={cc.name} className="flex items-center gap-2">
                      <div className="w-3 h-3 rounded-sm shrink-0" style={{ background: COLORS[i % COLORS.length] }} />
                      <span className="text-muted-foreground">{cc.name}</span>
                      <span className="font-semibold ml-2">{fmtCurrency(cc.amount, currency)}</span>
                    </div>
                  ))}
                </div>
              </div>
            </ChartCard>

            {/* Evolución por Centro de Costo */}
            <ChartCard title="Evolución por Centro de Costo" className="lg:col-span-2">
              <ResponsiveContainer width="100%" height={320}>
                <BarChart data={ccEvoData} barCategoryGap="20%">
                  <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
                  <XAxis dataKey="label" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
                  <YAxis tickFormatter={fmtShort} tick={{ fontSize: 11 }} width={50} axisLine={false} tickLine={false} />
                  <Tooltip content={<CustomTooltip />} cursor={{ fill: 'hsl(var(--muted))' }} />
                  <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
                  {ccEvoKeys.map((k, i) => (
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
