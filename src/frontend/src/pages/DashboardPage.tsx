import { useState, useEffect } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'
import {
  ComposedChart, Bar, Line, XAxis, YAxis, CartesianGrid, Tooltip,
  ResponsiveContainer, Legend, AreaChart, Area,
  PieChart, Pie, Cell,
} from 'recharts'
import { TrendingUp, TrendingDown } from 'lucide-react'
import api from '@/lib/api'
import type { DashboardSummaryDto, DisponibilidadesDto, FamilySettingsDto } from '@/types/api'

// ─── Colores ──────────────────────────────────────────────────────────────────

const C = {
  primary: '#3b82f6',
  income:  '#3b82f6',
  expense: '#f87171',
  palette: [
    '#3b82f6', '#818cf8', '#38bdf8', '#2dd4bf', '#a78bfa',
    '#34d399', '#fbbf24', '#fb923c', '#f472b6', '#94a3b8',
  ],
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

const MONTH_ABBR = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic']

function monthLabel(year: number, month: number) {
  return MONTH_ABBR[month - 1]
}

function fmtCurrency(v: number, currency: string) {
  return `${currency} ${v.toLocaleString('es-AR', { minimumFractionDigits: 0, maximumFractionDigits: 2 })}`
}
function fmtShort(v: number, currency: string) {
  const abs = Math.abs(v)
  const sign = v < 0 ? '-' : ''
  if (abs >= 1_000_000) return `${sign}${currency} ${(abs / 1_000_000).toFixed(1)}M`
  if (abs >= 1_000)     return `${sign}${currency} ${(abs / 1_000).toFixed(0)}k`
  return `${sign}${currency} ${abs}`
}

// ─── Tooltip ──────────────────────────────────────────────────────────────────

function FlowTooltip({ active, payload, label, currency }: any) {
  if (!active || !payload?.length) return null
  return (
    <div className="rounded-lg border bg-background p-2.5 shadow-md text-xs space-y-0.5">
      <p className="font-semibold mb-1">{label}</p>
      {payload.map((p: any) => (
        <p key={p.name} style={{ color: p.color }}>{p.name}: {fmtCurrency(p.value, currency)}</p>
      ))}
    </div>
  )
}

// ─── KPI Card ─────────────────────────────────────────────────────────────────

function KpiCard({ label, value, sub, trend, color }: {
  label: string; value: string; sub?: string; trend?: 'up' | 'down'; color?: string
}) {
  return (
    <Card>
      <CardContent className="pt-5 pb-4 px-5">
        <p className="text-xs text-muted-foreground font-medium mb-1.5 leading-none">{label}</p>
        <p className="text-2xl font-bold tracking-tight" style={color ? { color } : undefined}>{value}</p>
        {sub && (
          <p className={`text-xs mt-1.5 flex items-center gap-1 ${trend === 'up' ? 'text-emerald-500' : trend === 'down' ? 'text-rose-400' : 'text-muted-foreground'}`}>
            {trend === 'up'   && <TrendingUp   className="h-3 w-3" />}
            {trend === 'down' && <TrendingDown className="h-3 w-3" />}
            {sub}
          </p>
        )}
      </CardContent>
    </Card>
  )
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function DashboardPage() {
  const [currency, setCurrency]       = useState('ARS')
  const [dispCurrency, setDispCurrency] = useState('ARS')
  const [settings, setSettings]       = useState<FamilySettingsDto | null>(null)
  const [data, setData]               = useState<DashboardSummaryDto | null>(null)
  const [disp, setDisp]               = useState<DisponibilidadesDto | null>(null)
  const [loading, setLoading]         = useState(true)
  const [dispLoading, setDispLoading] = useState(true)
  const [error, setError]             = useState<string | null>(null)

  // Load family settings once
  useEffect(() => {
    api.get<FamilySettingsDto>('/family-settings').then(({ data: s }) => {
      setSettings(s)
      setCurrency(s.primaryCurrencyCode)
      setDispCurrency(s.primaryCurrencyCode)
    }).catch(() => {})
  }, [])

  // Load dashboard data when currency changes
  useEffect(() => {
    setLoading(true)
    setError(null)
    api.get<DashboardSummaryDto>(`/reports/dashboard?currency=${currency}`)
      .then(({ data: d }) => setData(d))
      .catch(() => setError('No se pudo cargar el dashboard.'))
      .finally(() => setLoading(false))
  }, [currency])

  // Load disponibilidades when dispCurrency changes
  useEffect(() => {
    if (!dispCurrency) return
    setDispLoading(true)
    api.get<DisponibilidadesDto>(`/reports/disponibilidades?currency=${dispCurrency}`)
      .then(({ data: d }) => setDisp(d))
      .catch(() => setDisp(null))
      .finally(() => setDispLoading(false))
  }, [dispCurrency])

  const currencies = settings
    ? [
        { code: settings.primaryCurrencyCode,  label: settings.primaryCurrencyCode },
        { code: settings.secondaryCurrencyCode, label: settings.secondaryCurrencyCode },
      ]
    : []

  const monthIncome  = data?.monthIncome ?? 0
  const monthExpense = data?.monthExpense ?? 0
  const resultado    = data?.monthResult ?? 0
  const resultColor  = resultado >= 0 ? C.primary : C.expense

  const flowData = (data?.monthlyFlow ?? []).map(m => ({
    mes:       monthLabel(m.year, m.month),
    ingresos:  m.income,
    egresos:   m.expense,
    resultado: m.result,
  }))

  const pieData = (disp?.byCurrency ?? []).map(g => ({ currency: g.currencyCode, value: g.totalConverted ?? 0 }))

  const patrimonyData = (data?.patrimonyEvolution ?? []).map(m => ({
    mes:   monthLabel(m.year, m.month),
    saldo: m.balance,
  }))

  const incPctRaw = data?.monthIncomeChangePct
  const expPctRaw = data?.monthExpenseChangePct

  const patrimonyChangeSub = data
    ? `${data.patrimonyChange >= 0 ? '+' : ''}${fmtCurrency(data.patrimonyChange, currency)} vs mes anterior`
    : undefined

  return (
    <div className="space-y-5">

      {/* Header */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-bold">Dashboard</h1>
        <Select value={currency} onValueChange={setCurrency}>
          <SelectTrigger className="w-36 h-8 text-sm">
            <SelectValue>{currency}</SelectValue>
          </SelectTrigger>
          <SelectContent>
            {currencies.map((c) => (
              <SelectItem key={c.code} value={c.code}>{c.label}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {error && (
        <p className="text-sm text-destructive">{error}</p>
      )}

      {/* ── KPI Cards ── */}
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <KpiCard
          label="Patrimonio total"
          value={loading ? '…' : fmtCurrency(data?.patrimony ?? 0, currency)}
          sub={patrimonyChangeSub}
          trend={data && data.patrimonyChange >= 0 ? 'up' : 'down'}
        />
        <KpiCard
          label="Ingresos del mes"
          value={loading ? '…' : fmtCurrency(monthIncome, currency)}
          sub={incPctRaw != null ? `${incPctRaw >= 0 ? '+' : ''}${incPctRaw}% vs mes anterior` : undefined}
          trend={incPctRaw != null ? (incPctRaw >= 0 ? 'up' : 'down') : undefined}
          color={C.primary}
        />
        <KpiCard
          label="Egresos del mes"
          value={loading ? '…' : fmtCurrency(monthExpense, currency)}
          sub={expPctRaw != null ? `${expPctRaw >= 0 ? '+' : ''}${expPctRaw}% vs mes anterior` : undefined}
          trend={expPctRaw != null ? (expPctRaw <= 0 ? 'up' : 'down') : undefined}
          color={C.expense}
        />
        <KpiCard
          label="Resultado neto"
          value={loading ? '…' : `${resultado >= 0 ? '+' : ''}${fmtCurrency(resultado, currency)}`}
          sub={resultado >= 0 ? 'Superávit' : 'Déficit'}
          trend={resultado >= 0 ? 'up' : 'down'}
          color={resultColor}
        />
      </div>

      {/* ── Temporal ── */}
      <div className="grid gap-4 lg:grid-cols-2">

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-semibold">Ingresos vs Egresos — últimos 6 meses</CardTitle>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={210}>
              <ComposedChart data={flowData} barGap={3} barCategoryGap="30%">
                <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
                <XAxis dataKey="mes" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
                <YAxis tickFormatter={(v) => fmtShort(v, currency)} tick={{ fontSize: 11 }} width={56} axisLine={false} tickLine={false} />
                <Tooltip content={<FlowTooltip currency={currency} />} cursor={{ fill: 'hsl(var(--muted))' }} />
                <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 12 }} />
                <Bar dataKey="ingresos"  name="Ingresos" fill={C.income}  radius={[3, 3, 0, 0]} />
                <Bar dataKey="egresos"   name="Egresos"  fill={C.expense} radius={[3, 3, 0, 0]} />
                <Line dataKey="resultado" name="Resultado" type="monotone"
                  stroke={C.palette[1]} strokeWidth={2} dot={{ r: 3, fill: C.palette[1] }} />
              </ComposedChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-semibold">Evolución patrimonial — últimos 6 meses</CardTitle>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={210}>
              <AreaChart data={patrimonyData}>
                <defs>
                  <linearGradient id="gradPat" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%"  stopColor={C.primary} stopOpacity={0.2} />
                    <stop offset="95%" stopColor={C.primary} stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
                <XAxis dataKey="mes" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
                <YAxis tickFormatter={(v) => fmtShort(v, currency)} tick={{ fontSize: 11 }} width={56} axisLine={false} tickLine={false} />
                <Tooltip formatter={(v: number) => [fmtCurrency(v, currency), 'Patrimonio']} contentStyle={{ fontSize: 12, borderRadius: 8 }} />
                <Area type="monotone" dataKey="saldo" stroke={C.primary} strokeWidth={2}
                  fill="url(#gradPat)" dot={false} />
              </AreaChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
      </div>

      {/* ── Disponibilidades ── */}
      <div className="grid gap-4 lg:grid-cols-2">

        {/* Donut */}
        <Card>
          <CardHeader className="pb-2">
            <div className="flex items-center justify-between">
              <CardTitle className="text-sm font-semibold">Disponibilidades</CardTitle>
              {currencies.length > 1 && (
                <Select value={dispCurrency} onValueChange={setDispCurrency}>
                  <SelectTrigger className="w-28 h-7 text-xs">
                    <SelectValue>{dispCurrency}</SelectValue>
                  </SelectTrigger>
                  <SelectContent>
                    {currencies.map((c) => (
                      <SelectItem key={c.code} value={c.code}>{c.label}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            </div>
          </CardHeader>
          <CardContent className="flex flex-col items-center gap-4">
            <div className="relative w-[200px] h-[200px]">
              <PieChart width={200} height={200}>
                <Pie
                  data={pieData}
                  cx={100} cy={100}
                  innerRadius={58} outerRadius={88}
                  paddingAngle={2}
                  dataKey="value"
                  startAngle={90} endAngle={-270}
                >
                  {pieData.map((_, i) => (
                    <Cell key={i} fill={C.palette[i % C.palette.length]} />
                  ))}
                </Pie>
                <Tooltip
                  formatter={(v: number) => [fmtCurrency(v, dispCurrency), '']}
                  contentStyle={{ fontSize: 12, borderRadius: 8 }}
                />
              </PieChart>
              <div className="absolute inset-0 flex flex-col items-center justify-center pointer-events-none">
                <span className="text-[10px] text-muted-foreground uppercase tracking-wide">Total</span>
                <span className="text-base font-bold leading-tight">
                  {dispLoading ? '…' : fmtShort(disp?.totalConverted ?? 0, dispCurrency)}
                </span>
              </div>
            </div>

            <div className="w-full space-y-1.5">
              {pieData.map((entry, i) => (
                <div key={entry.currency} className="flex items-center gap-2 text-xs">
                  <div className="w-2.5 h-2.5 rounded-sm shrink-0" style={{ background: C.palette[i % C.palette.length] }} />
                  <span className="text-muted-foreground font-medium">{entry.currency}</span>
                  <span className="ml-auto font-medium">{fmtCurrency(entry.value, dispCurrency)}</span>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        {/* Listado */}
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-semibold">Detalle por cuenta</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="divide-y divide-border">
              {dispLoading && (
                <p className="text-sm text-muted-foreground py-4">Cargando…</p>
              )}
              {!dispLoading && (disp?.byCurrency ?? []).length === 0 && (
                <p className="text-sm text-muted-foreground py-4">Sin cuentas con saldo.</p>
              )}
              {!dispLoading && (disp?.byCurrency ?? []).map((group, gi) => (
                <div key={group.currencyCode} className="py-3 first:pt-0">
                  <div className="flex items-center justify-between mb-2">
                    <div className="flex items-center gap-1.5">
                      <div className="w-2 h-2 rounded-sm shrink-0" style={{ background: C.palette[gi % C.palette.length] }} />
                      <span className="text-xs font-semibold uppercase tracking-wide">{group.currencyCode}</span>
                    </div>
                    <span className="text-xs font-semibold">{fmtCurrency(group.totalNative, group.currencyCode)}</span>
                  </div>
                  <div className="space-y-0.5 pl-3.5">
                    {group.accounts.map((acc) => (
                      <div key={acc.name} className="flex items-center justify-between py-0.5 text-sm">
                        <span className="text-muted-foreground">
                          {acc.name}{' '}
                          <span className="text-xs">({acc.accountType === 'CashBox' ? 'Caja' : 'Banco'})</span>
                        </span>
                        <span className="font-medium">{fmtCurrency(acc.balance, acc.currencyCode)}</span>
                      </div>
                    ))}
                  </div>
                  {group.totalConverted == null && (
                    <p className="text-[11px] text-amber-500 pl-3.5 mt-1">TC no disponible — no incluido en total</p>
                  )}
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

      </div>

    </div>
  )
}
