import { useState, useEffect } from 'react'
import { format } from 'date-fns'
import type { DateRange } from 'react-day-picker'
import {
  ResponsiveContainer, Tooltip,
  BarChart, AreaChart, PieChart,
  Bar, Area, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Legend,
} from 'recharts'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { ReportHeader, defaultDateRange } from '@/components/reports/ReportHeader'
import api from '@/lib/api'
import type {
  FamilySettingsDto,
  PatrimonyReportDto,
} from '@/types/api'

// ─── Colors ──────────────────────────────────────────────────────────────────

const COLORS = ['#3b82f6','#818cf8','#38bdf8','#2dd4bf','#a78bfa','#34d399','#fbbf24','#fb923c','#f472b6','#94a3b8']
const EXPENSE_COLOR = '#f87171'
const INCOME_COLOR = '#3b82f6'
const PATRIMONY_COLOR = '#34d399'

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

function CustomTooltip({ active, payload, label }: {
  active?: boolean
  payload?: Array<{ name?: string; value?: number; color?: string; dataKey?: unknown }>
  label?: string
}): React.ReactNode {
  if (!active || !payload?.length) return null
  return (
    <div className="rounded-lg border bg-background p-2 shadow-md text-xs space-y-0.5">
      {label && <p className="font-semibold">{label}</p>}
      {payload.map((p, i) => (
        <p key={String(p.dataKey ?? p.name ?? i)} style={{ color: p.color }}>
          {p.name}: {fmtShort(p.value ?? 0)}
        </p>
      ))}
    </div>
  )
}

// ─── Loading skeleton ─────────────────────────────────────────────────────────

function LoadingSkeleton() {
  return (
    <div className="space-y-4">
      <div className="grid grid-cols-4 gap-3">
        {[0, 1, 2, 3].map((i) => (
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

export default function PatrimonioPage() {
  const [dateRange, setDateRange] = useState<DateRange>(defaultDateRange())
  const [currency, setCurrency] = useState('')
  const [primaryCurrency, setPrimaryCurrency] = useState('ARS')
  const [secondaryCurrency, setSecondaryCurrency] = useState('')
  const [data, setData] = useState<PatrimonyReportDto | null>(null)
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

    api.get<PatrimonyReportDto>('/reports/patrimony', {
      params: { from, to, currency },
    }).then((res) => {
      setData(res.data)
    }).catch(() => {
      setData(null)
    }).finally(() => {
      setLoading(false)
    })
  }, [dateRange, currency])

  // Top accounts sorted by balanceInReportCurrency desc, reversed for horizontal bar
  const topAccountsSorted = [...(data?.topAccounts ?? [])]
    .sort((a, b) => b.balanceInReportCurrency - a.balanceInReportCurrency)
    .slice(0, 10)
    .reverse()

  const savingsRatioDisplay = data?.savingsRatio != null
    ? `${(data.savingsRatio * 100).toFixed(1)}%`
    : 'N/D'

  const isEmpty = data && data.totalAssets === 0 && data.totalLiabilities === 0

  return (
    <div className="space-y-4 pb-24">
      <ReportHeader
        title="Patrimonio"
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
          <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
            <KpiCard
              label="Activos Totales"
              value={fmtCurrency(data?.totalAssets ?? 0, currency)}
              color={INCOME_COLOR}
            />
            <KpiCard
              label="Pasivos Totales"
              value={fmtCurrency(data?.totalLiabilities ?? 0, currency)}
              color={EXPENSE_COLOR}
            />
            <KpiCard
              label="Patrimonio Neto"
              value={fmtCurrency(data?.netPatrimony ?? 0, currency)}
              color={PATRIMONY_COLOR}
            />
            <KpiCard
              label="Tasa de Ahorro"
              value={savingsRatioDisplay}
              sub={data?.periodSavings != null ? `${fmtCurrency(data.periodSavings, currency)} ahorrado` : undefined}
              color={PATRIMONY_COLOR}
            />
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            {/* Evolución del Patrimonio */}
            <ChartCard title="Evolución del Patrimonio" className="lg:col-span-2">
              <ResponsiveContainer width="100%" height={320}>
                <AreaChart data={data?.patrimonyEvolution ?? []}>
                  <defs>
                    <linearGradient id="gradPatrimony" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor={PATRIMONY_COLOR} stopOpacity={0.3} />
                      <stop offset="95%" stopColor={PATRIMONY_COLOR} stopOpacity={0} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
                  <XAxis dataKey="label" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
                  <YAxis tickFormatter={fmtShort} tick={{ fontSize: 11 }} width={55} axisLine={false} tickLine={false} />
                  <Tooltip content={<CustomTooltip />} cursor={{ fill: 'hsl(var(--muted))' }} />
                  <Area
                    dataKey="amount"
                    name="Patrimonio"
                    type="monotone"
                    fill="url(#gradPatrimony)"
                    stroke={PATRIMONY_COLOR}
                    strokeWidth={2.5}
                    dot={{ r: 3, fill: PATRIMONY_COLOR }}
                  />
                </AreaChart>
              </ResponsiveContainer>
            </ChartCard>

            {/* Composición por Moneda */}
            <ChartCard title="Composición por Moneda">
              <div className="flex items-center gap-6 pt-2">
                <ResponsiveContainer width={180} height={180}>
                  <PieChart>
                    <Pie
                      data={data?.balanceByCurrency ?? []}
                      dataKey="amount"
                      nameKey="name"
                      cx="50%" cy="50%"
                      innerRadius={50} outerRadius={80}
                      paddingAngle={3}
                      strokeWidth={0}
                    >
                      {(data?.balanceByCurrency ?? []).map((_, i) => (
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
                  {(data?.balanceByCurrency ?? []).map((item, i) => (
                    <div key={item.name} className="flex items-center gap-2">
                      <div className="w-3 h-3 rounded-sm shrink-0" style={{ background: COLORS[i % COLORS.length] }} />
                      <span className="text-muted-foreground">{item.name}</span>
                      <span className="font-semibold ml-2">{fmtCurrency(item.amount, currency)}</span>
                    </div>
                  ))}
                </div>
              </div>
            </ChartCard>

            {/* Por tipo de cuenta */}
            <ChartCard title="Por Tipo de Cuenta">
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={data?.balanceByAccountType ?? []} barCategoryGap="40%">
                  <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
                  <XAxis dataKey="name" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
                  <YAxis tickFormatter={fmtShort} tick={{ fontSize: 11 }} width={55} axisLine={false} tickLine={false} />
                  <Tooltip content={<CustomTooltip />} cursor={{ fill: 'hsl(var(--muted))' }} />
                  <Bar dataKey="amount" name="Saldo" fill={INCOME_COLOR} radius={[4, 4, 0, 0]}>
                    {(data?.balanceByAccountType ?? []).map((_, i) => (
                      <Cell key={i} fill={COLORS[i % COLORS.length]} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            </ChartCard>

            {/* Top Cuentas */}
            <ChartCard title="Top Cuentas" className="lg:col-span-2">
              {topAccountsSorted.length === 0 ? (
                <div className="flex items-center justify-center h-[200px] text-muted-foreground text-sm">
                  Sin cuentas con saldo
                </div>
              ) : (
                <ResponsiveContainer width="100%" height={Math.max(200, topAccountsSorted.length * 44 + 40)}>
                  <BarChart data={topAccountsSorted} layout="vertical" margin={{ left: 10, right: 10 }}>
                    <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" horizontal={false} />
                    <XAxis type="number" tickFormatter={fmtShort} tick={{ fontSize: 11 }} axisLine={false} />
                    <YAxis
                      type="category"
                      dataKey="name"
                      width={120}
                      tick={{ fontSize: 11 }}
                      axisLine={false}
                      tickLine={false}
                      tickFormatter={(name: string) => name.length > 16 ? `${name.slice(0, 14)}…` : name}
                    />
                    <Tooltip
                      content={(p) => {
                        const { active, payload, label } = p as { active?: boolean; payload?: Array<{value?: number}>; label?: string }
                        if (!active || !payload?.length) return null
                        const item = topAccountsSorted.find((a) => a.name === label)
                        return (
                          <div className="rounded-lg border bg-background p-2 shadow-md text-xs space-y-0.5">
                            <p className="font-semibold">{label}</p>
                            <p className="text-muted-foreground">{item?.accountType} · {item?.currencyCode}</p>
                            <p>Saldo: {fmtCurrency(item?.balance ?? 0, item?.currencyCode ?? currency)}</p>
                            <p>En {currency}: {fmtCurrency(payload[0]?.value ?? 0, currency)}</p>
                          </div>
                        )
                      }}
                      cursor={{ fill: 'hsl(var(--muted))' }}
                    />
                    <Bar
                      dataKey="balanceInReportCurrency"
                      name={`Saldo en ${currency}`}
                      fill={INCOME_COLOR}
                      radius={[0, 4, 4, 0]}
                      barSize={20}
                    >
                      {topAccountsSorted.map((item, i) => (
                        <Cell key={i} fill={item.accountType === 'BankAccount' ? INCOME_COLOR : COLORS[3]} />
                      ))}
                    </Bar>
                  </BarChart>
                </ResponsiveContainer>
              )}
              <div className="flex gap-4 mt-3 text-xs text-muted-foreground">
                <span className="flex items-center gap-1.5"><span className="w-3 h-3 rounded-sm inline-block" style={{ background: INCOME_COLOR }} />Banco</span>
                <span className="flex items-center gap-1.5"><span className="w-3 h-3 rounded-sm inline-block" style={{ background: COLORS[3] }} />Caja</span>
              </div>
            </ChartCard>

          </div>
        </div>
      )}
    </div>
  )
}
