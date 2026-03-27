import { useState, useEffect } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  PieChart, Pie, Cell, ResponsiveContainer,
} from 'recharts'
import { TrendingUp, TrendingDown } from 'lucide-react'
import api from '@/lib/api'
import type { PeriodAnalysisDto, DimensionItemDto, FamilySettingsDto } from '@/types/api'

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

const PERIOD_LABELS: Record<string, string> = {
  'mes-actual':   'Este mes',
  'mes-anterior': 'Mes anterior',
  trimestre:      'Trimestre',
  anio:           'Este año',
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

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

// ─── KPI Summary ──────────────────────────────────────────────────────────────

function SummaryKpi({ label, value, color }: { label: string; value: string; color?: string }) {
  return (
    <div className="flex flex-col gap-0.5">
      <span className="text-xs text-muted-foreground font-medium">{label}</span>
      <span className="text-xl font-bold tracking-tight" style={color ? { color } : undefined}>{value}</span>
    </div>
  )
}

// ─── Dimension Card ───────────────────────────────────────────────────────────

function DimensionCard({ label, data, selector, currency }: {
  label: string; data: DimensionItemDto[]; selector?: React.ReactNode; currency: string
}) {
  const expenseItems = data.filter((d) => d.expense > 0).sort((a, b) => b.expense - a.expense)
  const incomeItems  = data.filter((d) => d.income  > 0).sort((a, b) => b.income  - a.income)
  const totalExp = expenseItems.reduce((s, d) => s + d.expense, 0)
  const totalInc = incomeItems.reduce((s, d)  => s + d.income,  0)

  return (
    <Card>
      <CardHeader className="pb-2 pt-4 px-5">
        <div className="flex items-center justify-between gap-2">
          <CardTitle className="text-sm font-semibold">{label}</CardTitle>
          {selector}
        </div>
      </CardHeader>
      <CardContent className="px-5 pb-5 space-y-4">

        {expenseItems.length === 0 && incomeItems.length === 0 && (
          <p className="text-xs text-muted-foreground">Sin datos en este período.</p>
        )}

        {/* Egresos */}
        {expenseItems.length > 0 && (
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <span className="text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">Egresos</span>
              <span className="text-xs font-semibold" style={{ color: C.expense }}>{fmtShort(totalExp, currency)}</span>
            </div>
            <div className="flex gap-3 items-center">
              <div className="shrink-0" style={{ width: 68, height: 68 }}>
                <ResponsiveContainer width="100%" height="100%">
                  <PieChart>
                    <Pie data={expenseItems.slice(0, 6)} dataKey="expense"
                      cx="50%" cy="50%" innerRadius={20} outerRadius={32} strokeWidth={0}>
                      {expenseItems.slice(0, 6).map((_, i) => (
                        <Cell key={i} fill={C.palette[i % C.palette.length]} />
                      ))}
                    </Pie>
                  </PieChart>
                </ResponsiveContainer>
              </div>
              <div className="flex-1 space-y-1.5 min-w-0">
                {expenseItems.slice(0, 5).map((d, i) => (
                  <div key={d.name} className="space-y-0.5">
                    <div className="flex justify-between text-[11px]">
                      <span className="text-muted-foreground truncate">{d.name}</span>
                      <span className="font-medium shrink-0 ml-1">{fmtShort(d.expense, currency)}</span>
                    </div>
                    <div className="h-1 rounded-full bg-muted overflow-hidden">
                      <div className="h-full rounded-full transition-all"
                        style={{ width: `${(d.expense / expenseItems[0].expense) * 100}%`, background: C.palette[i % C.palette.length] }} />
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}

        {incomeItems.length > 0 && (
          <>
            {expenseItems.length > 0 && <div className="border-t" />}
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <span className="text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">Ingresos</span>
                <span className="text-xs font-semibold" style={{ color: C.income }}>{fmtShort(totalInc, currency)}</span>
              </div>
              <div className="flex gap-3 items-center">
                <div className="shrink-0" style={{ width: 68, height: 68 }}>
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie data={incomeItems.slice(0, 5)} dataKey="income"
                        cx="50%" cy="50%" innerRadius={20} outerRadius={32} strokeWidth={0}>
                        {incomeItems.slice(0, 5).map((_, i) => (
                          <Cell key={i} fill={C.palette[i % C.palette.length]} />
                        ))}
                      </Pie>
                    </PieChart>
                  </ResponsiveContainer>
                </div>
                <div className="flex-1 space-y-1.5 min-w-0">
                  {incomeItems.slice(0, 4).map((d, i) => (
                    <div key={d.name} className="space-y-0.5">
                      <div className="flex justify-between text-[11px]">
                        <span className="text-muted-foreground truncate">{d.name}</span>
                        <span className="font-medium shrink-0 ml-1">{fmtShort(d.income, currency)}</span>
                      </div>
                      <div className="h-1 rounded-full bg-muted overflow-hidden">
                        <div className="h-full rounded-full transition-all"
                          style={{ width: `${(d.income / incomeItems[0].income) * 100}%`, background: C.palette[i % C.palette.length] }} />
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </>
        )}
      </CardContent>
    </Card>
  )
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function AnalysisPage() {
  const [period, setPeriod]     = useState('mes-actual')
  const [currency, setCurrency] = useState('ARS')
  const [catSub, setCatSub]     = useState<'categoria' | 'subcategoria'>('categoria')
  const [settings, setSettings] = useState<FamilySettingsDto | null>(null)
  const [data, setData]         = useState<PeriodAnalysisDto | null>(null)
  const [loading, setLoading]   = useState(true)
  const [error, setError]       = useState<string | null>(null)

  // Load family settings once
  useEffect(() => {
    api.get<FamilySettingsDto>('/family-settings').then(({ data: s }) => {
      setSettings(s)
      setCurrency(s.primaryCurrencyCode)
    }).catch(() => {})
  }, [])

  // Load analysis data when period or currency changes
  useEffect(() => {
    setLoading(true)
    setError(null)
    api.get<PeriodAnalysisDto>(`/reports/analysis?period=${period}&currency=${currency}`)
      .then(({ data: d }) => setData(d))
      .catch(() => setError('No se pudo cargar el análisis.'))
      .finally(() => setLoading(false))
  }, [period, currency])

  const currencies = settings
    ? [
        { code: settings.primaryCurrencyCode,  label: settings.primaryCurrencyCode },
        { code: settings.secondaryCurrencyCode, label: settings.secondaryCurrencyCode },
      ]
    : []

  const totalIncome  = data?.income ?? 0
  const totalExpense = data?.expense ?? 0
  const resultado    = data?.result ?? 0
  const resultColor  = resultado >= 0 ? C.primary : C.expense

  const catSubData = catSub === 'categoria'
    ? (data?.byCategory ?? [])
    : (data?.bySubcategory ?? [])

  const otherDimensions = [
    { id: 'centro-costo',  label: 'Por Centro de Costo',   data: data?.byCostCenter ?? [] },
    { id: 'caracter',      label: 'Por Carácter',           data: data?.byCharacter ?? [] },
    { id: 'tipo-contable', label: 'Por Tipo Contable',      data: data?.byAccountingType ?? [] },
  ]

  return (
    <div className="space-y-5">

      {/* Header */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-bold">Análisis por período</h1>
        <div className="flex items-center gap-2">
          <Select value={currency} onValueChange={(val) => val && setCurrency(val)}>
            <SelectTrigger className="w-36 h-8 text-sm">
              <SelectValue>{currency}</SelectValue>
            </SelectTrigger>
            <SelectContent>
              {currencies.map((c) => (
                <SelectItem key={c.code} value={c.code}>{c.label}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Select value={period} onValueChange={(val) => val && setPeriod(val)}>
            <SelectTrigger className="w-44 h-8 text-sm">
              <SelectValue>{PERIOD_LABELS[period]}</SelectValue>
            </SelectTrigger>
            <SelectContent>
              {Object.entries(PERIOD_LABELS).map(([k, v]) => (
                <SelectItem key={k} value={k}>{v}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      {error && (
        <p className="text-sm text-destructive">{error}</p>
      )}

      {/* ── Resumen del período ── */}
      <Card>
        <CardContent className="pt-4 pb-4 px-5">
          <div className="flex flex-wrap gap-6 items-center">
            <SummaryKpi label="Ingresos"  value={loading ? '…' : fmtCurrency(totalIncome, currency)}  color={C.income} />
            <div className="w-px h-8 bg-border" />
            <SummaryKpi label="Egresos"   value={loading ? '…' : fmtCurrency(totalExpense, currency)} color={C.expense} />
            <div className="w-px h-8 bg-border" />
            <SummaryKpi
              label="Resultado neto"
              value={loading ? '…' : `${resultado >= 0 ? '+' : ''}${fmtCurrency(resultado, currency)}`}
              color={resultColor}
            />
            {!loading && (
              <span className={`flex items-center gap-1 text-xs font-medium ml-auto`} style={{ color: resultColor }}>
                {resultado >= 0
                  ? <TrendingUp className="h-3.5 w-3.5" />
                  : <TrendingDown className="h-3.5 w-3.5" />}
                {resultado >= 0 ? 'Superávit' : 'Déficit'}
              </span>
            )}
          </div>
        </CardContent>
      </Card>

      {/* ── Cards de dimensiones ── */}
      <div className="grid gap-4 sm:grid-cols-2">

        <DimensionCard
          label={catSub === 'categoria' ? 'Por Categoría' : 'Por Subcategoría'}
          data={catSubData}
          currency={currency}
          selector={
            <Select value={catSub} onValueChange={(v) => setCatSub(v as typeof catSub)}>
              <SelectTrigger className="h-6 text-xs w-32 border-0 bg-muted px-2">
                <SelectValue>{catSub === 'categoria' ? 'Categoría' : 'Subcategoría'}</SelectValue>
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="categoria">Categoría</SelectItem>
                <SelectItem value="subcategoria">Subcategoría</SelectItem>
              </SelectContent>
            </Select>
          }
        />

        {otherDimensions.map((dim) => (
          <DimensionCard key={dim.id} label={dim.label} data={dim.data} currency={currency} />
        ))}

      </div>
    </div>
  )
}
