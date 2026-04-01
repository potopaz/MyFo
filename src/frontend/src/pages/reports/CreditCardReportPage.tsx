import { useState, useEffect } from 'react'
import { format } from 'date-fns'
import type { DateRange } from 'react-day-picker'
import {
  ResponsiveContainer, Tooltip,
  BarChart, Bar, ComposedChart, Line,
  XAxis, YAxis, CartesianGrid, Legend,
  ReferenceLine,
  PieChart, Pie, Cell,
} from 'recharts'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { ReportHeader, defaultDateRange } from '@/components/reports/ReportHeader'
import { DrilldownSheet } from '@/components/reports/DrilldownSheet'
import api from '@/lib/api'
import type {
  FamilySettingsDto,
  CardsCCReportDto,
} from '@/types/api'

// ─── Colors ──────────────────────────────────────────────────────────────────

const COLORS = ['#3b82f6','#818cf8','#38bdf8','#2dd4bf','#a78bfa','#34d399','#fbbf24','#fb923c','#f472b6','#94a3b8']
const EXPENSE_COLOR = '#f87171'
const INCOME_COLOR = '#3b82f6'
const NET_COLOR = '#a78bfa'

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
        {[0, 1, 2].map((i) => (
          <Card key={i}><CardHeader className="pb-2"><Skeleton className="h-4 w-40" /></CardHeader><CardContent><Skeleton className="h-[300px] w-full" /></CardContent></Card>
        ))}
      </div>
    </div>
  )
}

// ─── Drilldown state ──────────────────────────────────────────────────────────

interface DrilldownState {
  open: boolean
  title: string
  dimension: string
  dimensionValue: string
  installmentMonth?: string
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function CreditCardReportPage() {
  const [dateRange, setDateRange] = useState<DateRange>(defaultDateRange())
  const [currency, setCurrency] = useState('')
  const [primaryCurrency, setPrimaryCurrency] = useState('ARS')
  const [secondaryCurrency, setSecondaryCurrency] = useState('')
  const [data, setData] = useState<CardsCCReportDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [drilldown, setDrilldown] = useState<DrilldownState>({ open: false, title: '', dimension: '', dimensionValue: '' })

  useEffect(() => {
    api.get<FamilySettingsDto>('/family-settings').then((res) => {
      setPrimaryCurrency(res.data.primaryCurrencyCode)
      setSecondaryCurrency(res.data.secondaryCurrencyCode)
      setCurrency(res.data.primaryCurrencyCode)
    }).catch(() => {
      setCurrency('ARS')
    })
  }, [])

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

  // Cards sorted by debt desc
  const cardsSorted = [...(data?.installmentsByCard ?? [])].sort((a, b) => b.totalDebt - a.totalDebt)

  // Map cardName → cardId for drilldown
  const cardNameToId = Object.fromEntries(cardsSorted.map((c) => [c.cardName, c.cardId]))

  // Future installments stacked bar (group by label, spread cards)
  const futureByLabel = (data?.futureInstallments ?? []).reduce<Record<string, Record<string, number>>>((acc, item) => {
    if (!acc[item.label]) acc[item.label] = {}
    acc[item.label][item.cardName] = (acc[item.label][item.cardName] ?? 0) + item.amount
    return acc
  }, {})
  // Include the month key (YYYY-MM) so we can pass it to drilldown
  const labelToMonth = Object.fromEntries(
    (data?.futureInstallments ?? []).map((f) => [f.label, f.month])
  )
  const futureData = Object.entries(futureByLabel).map(([label, cards]) => ({ label, month: labelToMonth[label] ?? '', ...cards }))
  const cardNames = Array.from(new Set((data?.futureInstallments ?? []).map((f) => f.cardName)))

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
              sub={`${fmtShort(data?.chargesVsBonifications.totalCharges ?? 0)} cargos - ${fmtShort(data?.chargesVsBonifications.totalBonifications ?? 0)} bonif.`}
              color={netCharges <= 0 ? '#34d399' : EXPENSE_COLOR}
            />
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            {/* Deuda por Tarjeta — pie chart clickable para drill-down */}
            <ChartCard title="Deuda por Tarjeta (click para ver detalle)">
              {cardsSorted.length === 0 ? (
                <div className="flex items-center justify-center h-[200px] text-muted-foreground text-sm">
                  Sin deuda pendiente
                </div>
              ) : (
                <ResponsiveContainer width="100%" height={280}>
                  <PieChart>
                    <Pie
                      data={cardsSorted}
                      dataKey="totalDebt"
                      nameKey="cardName"
                      cx="50%"
                      cy="50%"
                      innerRadius={55}
                      outerRadius={100}
                      paddingAngle={2}
                      style={{ cursor: 'pointer' }}
                      onClick={(entry: { cardId: string; cardName: string }) => {
                        setDrilldown({ open: true, title: entry.cardName, dimension: 'creditcard', dimensionValue: entry.cardId })
                      }}
                      label={({ cardName, percent }: { cardName: string; percent: number }) =>
                        `${cardName} ${(percent * 100).toFixed(0)}%`
                      }
                      labelLine={{ strokeWidth: 1 }}
                    >
                      {cardsSorted.map((_, i) => (
                        <Cell key={i} fill={COLORS[i % COLORS.length]} />
                      ))}
                    </Pie>
                    <Tooltip
                      formatter={(value: number) => fmtCurrency(value, currency)}
                    />
                  </PieChart>
                </ResponsiveContainer>
              )}
            </ChartCard>

            {/* Evolución mensual de deuda: nueva deuda vs pagos + línea neta */}
            <ChartCard title="Nueva Deuda vs Pagos por Mes">
              {(data?.monthlyDebtEvolution ?? []).length === 0 ? (
                <div className="flex items-center justify-center h-[200px] text-muted-foreground text-sm">
                  Sin datos en el período
                </div>
              ) : (
                <ResponsiveContainer width="100%" height={280}>
                  <ComposedChart data={data?.monthlyDebtEvolution ?? []} barCategoryGap="30%">
                    <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
                    <XAxis dataKey="label" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
                    <YAxis tickFormatter={fmtShort} tick={{ fontSize: 11 }} width={55} axisLine={false} tickLine={false} />
                    <Tooltip content={<CustomTooltip />} cursor={{ fill: 'rgba(0,0,0,0.05)' }} />
                    <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
                    <ReferenceLine y={0} stroke="hsl(var(--border))" />
                    <Bar
                      dataKey="newDebt"
                      name="Nueva deuda"
                      fill={EXPENSE_COLOR}
                      activeBar={{ fill: EXPENSE_COLOR, fillOpacity: 0.7 }}
                      radius={[3, 3, 0, 0]}
                      barSize={20}
                    />
                    <Bar
                      dataKey="paid"
                      name="Pagado"
                      fill={INCOME_COLOR}
                      activeBar={{ fill: INCOME_COLOR, fillOpacity: 0.7 }}
                      radius={[3, 3, 0, 0]}
                      barSize={20}
                    />
                    <Line
                      dataKey="net"
                      name="Neto"
                      type="monotone"
                      stroke={NET_COLOR}
                      strokeWidth={2}
                      dot={{ r: 3, fill: NET_COLOR }}
                      strokeDasharray="4 2"
                    />
                  </ComposedChart>
                </ResponsiveContainer>
              )}
            </ChartCard>

            {/* Cuotas Futuras por Tarjeta */}
            <ChartCard title="Cuotas Futuras por Tarjeta (próximos 12 meses)" className="lg:col-span-2">
              {futureData.length === 0 ? (
                <div className="flex items-center justify-center h-[200px] text-muted-foreground text-sm">
                  Sin cuotas futuras proyectadas
                </div>
              ) : (
                <ResponsiveContainer width="100%" height={300}>
                  <BarChart data={futureData} barCategoryGap="20%">
                    <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
                    <XAxis dataKey="label" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
                    <YAxis tickFormatter={fmtShort} tick={{ fontSize: 11 }} width={55} axisLine={false} tickLine={false} />
                    <Tooltip content={<CustomTooltip />} cursor={{ fill: 'rgba(0,0,0,0.05)' }} />
                    <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
                    {cardNames.map((name, i) => (
                      <Bar
                        key={name}
                        dataKey={name}
                        stackId="a"
                        fill={COLORS[i % COLORS.length]}
                        activeBar={{ fill: COLORS[i % COLORS.length], fillOpacity: 0.7 }}
                        radius={i === cardNames.length - 1 ? [3, 3, 0, 0] : undefined}
                        style={{ cursor: cardNameToId[name] ? 'pointer' : 'default' }}
                        onClick={(barData: { label?: string; month?: string }) => {
                          const cardId = cardNameToId[name]
                          if (!cardId) return
                          setDrilldown({ open: true, title: name, dimension: 'creditcard', dimensionValue: cardId, installmentMonth: barData.month ?? undefined })
                        }}
                      />
                    ))}
                  </BarChart>
                </ResponsiveContainer>
              )}
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
        dateRange={dateRange}
        currency={currency}
        installmentMonth={drilldown.installmentMonth}
      />
    </div>
  )
}
