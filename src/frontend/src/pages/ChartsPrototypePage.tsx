// ═══════════════════════════════════════════════════════════════════════════════
// ChartsPrototypePage — Mock data for visual evaluation of chart proposals
// TEMPORARY — not for production
// ═══════════════════════════════════════════════════════════════════════════════

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  ResponsiveContainer, Tooltip,
  ComposedChart, BarChart, LineChart, AreaChart, PieChart,
  Bar, Line, Area, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Legend,
  Treemap,
} from 'recharts'
import { TrendingUp, TrendingDown, ArrowRight } from 'lucide-react'

// ─── Colors & Helpers ────────────────────────────────────────────────────────

const P = ['#3b82f6','#818cf8','#38bdf8','#2dd4bf','#a78bfa','#34d399','#fbbf24','#fb923c','#f472b6','#94a3b8']
const INCOME = '#3b82f6', EXPENSE = '#f87171', EMERALD = '#10b981'

const fmt = (v: number) => `ARS ${v.toLocaleString('es-AR', { maximumFractionDigits: 0 })}`
const fmtK = (v: number) => {
  const a = Math.abs(v), s = v < 0 ? '-' : ''
  if (a >= 1e6) return `${s}$${(a / 1e6).toFixed(1)}M`
  if (a >= 1e3) return `${s}$${(a / 1e3).toFixed(0)}k`
  return `${s}$${a}`
}
const fmtPct = (v: number) => `${(v * 100).toFixed(0)}%`

function Tip({ active, payload, label }: any) {
  if (!active || !payload?.length) return null
  return (
    <div className="rounded-lg border bg-background p-2 shadow-md text-xs space-y-0.5">
      {label && <p className="font-semibold">{label}</p>}
      {payload.map((p: any) => (
        <p key={p.dataKey || p.name} style={{ color: p.color || p.fill }}>{p.name}: {typeof p.value === 'number' && p.value <= 1 && p.value >= 0 ? fmtPct(p.value) : fmt(p.value)}</p>
      ))}
    </div>
  )
}

// ─── Layout helpers ──────────────────────────────────────────────────────────

function Section({ num, title, desc }: { num: number; title: string; desc: string }) {
  return (
    <div className="pt-6 first:pt-0">
      <div className="flex items-baseline gap-3">
        <span className="text-xs font-bold text-primary bg-primary/10 px-2.5 py-1 rounded-full shrink-0">{num}</span>
        <h2 className="text-lg font-bold">{title}</h2>
      </div>
      <p className="text-sm text-muted-foreground mt-1 ml-10">{desc}</p>
    </div>
  )
}

function Kpi({ label, value, sub, trend, color }: {
  label: string; value: string; sub?: string; trend?: 'up' | 'down'; color?: string
}) {
  return (
    <Card>
      <CardContent className="pt-5 pb-4 px-5">
        <p className="text-xs text-muted-foreground font-medium mb-1.5">{label}</p>
        <p className="text-2xl font-bold" style={color ? { color } : undefined}>{value}</p>
        {sub && (
          <p className={`text-xs mt-1.5 flex items-center gap-1 ${trend === 'up' ? 'text-emerald-500' : trend === 'down' ? 'text-rose-400' : 'text-muted-foreground'}`}>
            {trend === 'up' && <TrendingUp className="h-3 w-3" />}
            {trend === 'down' && <TrendingDown className="h-3 w-3" />}
            {sub}
          </p>
        )}
      </CardContent>
    </Card>
  )
}

function Ch({ title, children, className }: { title: string; children: React.ReactNode; className?: string }) {
  return (
    <Card className={className}>
      <CardHeader className="pb-2"><CardTitle className="text-sm font-semibold">{title}</CardTitle></CardHeader>
      <CardContent>{children}</CardContent>
    </Card>
  )
}

// ═══════════════════════════════════════════════════════════════════════════════
//                              MOCK DATA
// ═══════════════════════════════════════════════════════════════════════════════

// -- 1. Expense treemap (flat, current month) --
const expenseTreemap = [
  { name: 'Alquiler', size: 200000, color: P[0] },
  { name: 'Super', size: 130000, color: P[1] },
  { name: 'Servicios', size: 45000, color: P[2] },
  { name: 'Colegio', size: 45000, color: P[3] },
  { name: 'Nafta', size: 50000, color: P[4] },
  { name: 'Delivery', size: 40000, color: P[5] },
  { name: 'Prepaga', size: 35000, color: P[6] },
  { name: 'Expensas', size: 35000, color: P[7] },
  { name: 'Salidas', size: 32000, color: P[8] },
  { name: 'Vestimenta', size: 25000, color: P[9] },
  { name: 'Patente', size: 25000, color: P[0] },
  { name: 'Útiles', size: 20000, color: P[1] },
  { name: 'Gimnasio', size: 15000, color: P[2] },
  { name: 'ABL', size: 15000, color: P[3] },
  { name: 'Verdulería', size: 15000, color: P[4] },
  { name: 'Peajes', size: 12000, color: P[5] },
  { name: 'Farmacia', size: 10000, color: P[6] },
  { name: 'Streaming', size: 8000, color: P[7] },
  { name: 'Otros', size: 23000, color: P[9] },
]

// -- 2. Top 10 subcategories --
const topSubcats = [
  { name: 'Alquiler', value: 200000 },
  { name: 'Supermercado', value: 130000 },
  { name: 'Nafta', value: 50000 },
  { name: 'Colegio', value: 45000 },
  { name: 'Servicios', value: 45000 },
  { name: 'Delivery', value: 40000 },
  { name: 'Prepaga', value: 35000 },
  { name: 'Expensas', value: 35000 },
  { name: 'Salidas', value: 32000 },
  { name: 'Vestimenta', value: 25000 },
].reverse()

// -- 3. Category evolution (6 months) --
const catEvolution = [
  { mes: 'Oct', Hogar: 260000, Aliment: 170000, Transp: 70000, Educ: 60000, Salud: 42000, Ocio: 48000, Otros: 30000 },
  { mes: 'Nov', Hogar: 265000, Aliment: 175000, Transp: 68000, Educ: 62000, Salud: 40000, Ocio: 55000, Otros: 45000 },
  { mes: 'Dic', Hogar: 270000, Aliment: 195000, Transp: 75000, Educ: 65000, Salud: 44000, Ocio: 85000, Otros: 116000 },
  { mes: 'Ene', Hogar: 275000, Aliment: 185000, Transp: 82000, Educ: 0, Salud: 43000, Ocio: 95000, Otros: 110000 },
  { mes: 'Feb', Hogar: 278000, Aliment: 178000, Transp: 72000, Educ: 63000, Salud: 41000, Ocio: 52000, Otros: 46000 },
  { mes: 'Mar', Hogar: 280000, Aliment: 185000, Transp: 75000, Educ: 65000, Salud: 45000, Ocio: 55000, Otros: 75000 },
]
const CAT_KEYS = ['Hogar', 'Aliment', 'Transp', 'Educ', 'Salud', 'Ocio', 'Otros']

// -- 4. Ordinary vs extraordinary --
const ordVsExt = [
  { name: 'Ordinarios', value: 650000 },
  { name: 'Extraordinarios', value: 130000 },
]

// -- 5. Income sources --
const incomeSources = [
  { name: 'Sueldo Juan', value: 550000 },
  { name: 'Sueldo María', value: 350000 },
  { name: 'Freelance', value: 50000 },
]
const incomeEvolution = [
  { mes: 'Oct', 'Sueldo J.': 500000, 'Sueldo M.': 320000, Freelance: 0 },
  { mes: 'Nov', 'Sueldo J.': 500000, 'Sueldo M.': 320000, Freelance: 30000 },
  { mes: 'Dic', 'Sueldo J.': 500000, 'Sueldo M.': 320000, Freelance: 0, Aguinaldo: 410000 },
  { mes: 'Ene', 'Sueldo J.': 530000, 'Sueldo M.': 340000, Freelance: 0 },
  { mes: 'Feb', 'Sueldo J.': 530000, 'Sueldo M.': 340000, Freelance: 30000 },
  { mes: 'Mar', 'Sueldo J.': 550000, 'Sueldo M.': 350000, Freelance: 50000 },
]
const INC_KEYS = ['Sueldo J.', 'Sueldo M.', 'Freelance', 'Aguinaldo']

// -- 6. Daily cash flow (March, 28 days shown) --
const dailyCashFlow = Array.from({ length: 28 }, (_, i) => {
  const d = i + 1
  let ing = 0, eg = 0
  if (d === 1) ing = 550000  // sueldo Juan
  if (d === 5) ing = 350000  // sueldo María
  if (d === 15) ing = 50000  // freelance
  // Spread expenses
  if (d % 3 === 0) eg = 35000 + Math.round(Math.random() * 20000)
  if (d % 7 === 0) eg += 45000
  if (d === 10) eg = 200000  // alquiler
  if (d === 12) eg = 35000   // expensas
  if (d === 20) eg = 45000   // servicios
  return { dia: d, ingresos: ing, egresos: eg }
})
// Calculate cumulative
let acum = 1200000 // starting balance
for (const d of dailyCashFlow) {
  acum += d.ingresos - d.egresos
  ;(d as any).acumulado = acum
}

// -- 7. CC installments future (12 months from Apr 2026) --
const ccFuture = [
  { mes: 'Abr', Visa: 125000, Master: 88000 },
  { mes: 'May', Visa: 118000, Master: 85000 },
  { mes: 'Jun', Visa: 110000, Master: 78000 },
  { mes: 'Jul', Visa: 95000, Master: 72000 },
  { mes: 'Ago', Visa: 88000, Master: 65000 },
  { mes: 'Sep', Visa: 75000, Master: 55000 },
  { mes: 'Oct', Visa: 60000, Master: 45000 },
  { mes: 'Nov', Visa: 48000, Master: 35000 },
  { mes: 'Dic', Visa: 35000, Master: 22000 },
  { mes: 'Ene', Visa: 22000, Master: 15000 },
  { mes: 'Feb', Visa: 12000, Master: 8000 },
  { mes: 'Mar', Visa: 5000, Master: 3000 },
]

// -- 8. Cost centers --
const costCenters = [
  { name: 'Casa', value: 310000 },
  { name: 'Auto', value: 75000 },
  { name: 'Hijos', value: 85000 },
  { name: 'Personal', value: 120000 },
  { name: 'Sin asignar', value: 190000 },
]
const ccEvol = [
  { mes: 'Oct', Casa: 280000, Auto: 65000, Hijos: 75000, Personal: 110000, 'Sin asignar': 150000 },
  { mes: 'Nov', Casa: 285000, Auto: 68000, Hijos: 78000, Personal: 115000, 'Sin asignar': 164000 },
  { mes: 'Dic', Casa: 290000, Auto: 90000, Hijos: 120000, Personal: 140000, 'Sin asignar': 210000 },
  { mes: 'Ene', Casa: 295000, Auto: 85000, Hijos: 90000, Personal: 160000, 'Sin asignar': 160000 },
  { mes: 'Feb', Casa: 290000, Auto: 72000, Hijos: 80000, Personal: 118000, 'Sin asignar': 170000 },
  { mes: 'Mar', Casa: 310000, Auto: 75000, Hijos: 85000, Personal: 120000, 'Sin asignar': 190000 },
]
const CC_KEYS = ['Casa', 'Auto', 'Hijos', 'Personal', 'Sin asignar']

// -- 9. Payment methods --
const paymentMethods = [
  { name: 'Efectivo (cajas)', value: 180000 },
  { name: 'Banco', value: 350000 },
  { name: 'Tarjeta crédito', value: 250000 },
]
const pmEvolution = [
  { mes: 'Oct', Efectivo: 210000, Banco: 310000, Tarjeta: 160000 },
  { mes: 'Nov', Efectivo: 200000, Banco: 320000, Tarjeta: 190000 },
  { mes: 'Dic', Efectivo: 220000, Banco: 380000, Tarjeta: 250000 },
  { mes: 'Ene', Efectivo: 195000, Banco: 340000, Tarjeta: 255000 },
  { mes: 'Feb', Efectivo: 185000, Banco: 330000, Tarjeta: 215000 },
  { mes: 'Mar', Efectivo: 180000, Banco: 350000, Tarjeta: 250000 },
]

// -- 10. CC member spending --
const ccMembers = [
  { name: 'Juan', Visa: 120000, Master: 30000 },
  { name: 'María', Visa: 45000, Master: 110000 },
  { name: 'Hijo 1', Visa: 0, Master: 25000 },
]

// -- 11. CC charges vs bonuses --
const ccCharges = [
  { mes: 'Oct', Cargos: 8500, Bonificaciones: 3200 },
  { mes: 'Nov', Cargos: 9200, Bonificaciones: 4100 },
  { mes: 'Dic', Cargos: 12000, Bonificaciones: 5500 },
  { mes: 'Ene', Cargos: 10800, Bonificaciones: 3800 },
  { mes: 'Feb', Cargos: 9500, Bonificaciones: 4200 },
  { mes: 'Mar', Cargos: 9800, Bonificaciones: 4500 },
]

// -- 12. Exchange rates ARS/USD --
const tcData = [
  { mes: 'Oct', 'TC efectivo': 1020, 'TC mercado': 1000 },
  { mes: 'Nov', 'TC efectivo': 1055, 'TC mercado': 1040 },
  { mes: 'Dic', 'TC efectivo': 1095, 'TC mercado': 1080 },
  { mes: 'Ene', 'TC efectivo': 1140, 'TC mercado': 1130 },
  { mes: 'Feb', 'TC efectivo': 1195, 'TC mercado': 1180 },
  { mes: 'Mar', 'TC efectivo': 1260, 'TC mercado': 1250 },
]

// -- 13. Currency composition (% of patrimony) --
const currComp = [
  { mes: 'Oct', ARS: 52, USD: 48 },
  { mes: 'Nov', ARS: 48, USD: 52 },
  { mes: 'Dic', ARS: 45, USD: 55 },
  { mes: 'Ene', ARS: 42, USD: 58 },
  { mes: 'Feb', ARS: 38, USD: 62 },
  { mes: 'Mar', ARS: 35, USD: 65 },
]

// -- 14. Savings ratio --
const savingsRatio = [
  { mes: 'Oct', ratio: 17.1 },
  { mes: 'Nov', ratio: 16.5 },
  { mes: 'Dic', ratio: 7.6 },
  { mes: 'Ene', ratio: 9.2 },
  { mes: 'Feb', ratio: 18.9 },
  { mes: 'Mar', ratio: 17.9 },
]

// -- 15. Transfer volumes --
const transferVol = [
  { mes: 'Oct', cantidad: 12, monto: 450000 },
  { mes: 'Nov', cantidad: 14, monto: 520000 },
  { mes: 'Dic', cantidad: 18, monto: 680000 },
  { mes: 'Ene', cantidad: 15, monto: 550000 },
  { mes: 'Feb', cantidad: 11, monto: 480000 },
  { mes: 'Mar', cantidad: 13, monto: 510000 },
]

// -- 16. Top transfer flows --
const topFlows = [
  { from: 'Banco Nación', to: 'Billetera Juan', monto: 180000, count: 8 },
  { from: 'Banco Nación', to: 'Billetera María', monto: 120000, count: 6 },
  { from: 'Banco Nación', to: 'Brubank', monto: 350000, count: 3 },
  { from: 'Brubank', to: 'Billetera Juan', monto: 45000, count: 2 },
  { from: 'Wise USD', to: 'Caja fuerte USD', monto: 1500, count: 2 },
]

// ─── Treemap custom cell ─────────────────────────────────────────────────────

function TreemapCell(props: any) {
  const { x, y, width, height, name, size, color } = props
  if (!width || !height) return null
  return (
    <g>
      <rect x={x} y={y} width={width} height={height} fill={color || '#94a3b8'} stroke="hsl(var(--background))" strokeWidth={2} rx={4} />
      {width > 55 && height > 28 && (
        <>
          <text x={x + 6} y={y + 16} fill="#fff" fontSize={11} fontWeight={600}>{name}</text>
          <text x={x + 6} y={y + 30} fill="rgba(255,255,255,0.7)" fontSize={9}>{fmtK(size)}</text>
        </>
      )}
      {width > 35 && width <= 55 && height > 18 && (
        <text x={x + 4} y={y + 14} fill="#fff" fontSize={8} fontWeight={500}>{name}</text>
      )}
    </g>
  )
}

// ═══════════════════════════════════════════════════════════════════════════════
//                                PAGE
// ═══════════════════════════════════════════════════════════════════════════════

export default function ChartsPrototypePage() {
  const H = 230 // default chart height

  return (
    <div className="space-y-5 pb-24">
      <div>
        <h1 className="text-2xl font-bold">Prototipo de gráficos</h1>
        <p className="text-sm text-muted-foreground mt-1">
          Datos ficticios — familia tipo, ARS/USD bimonetario, Oct 2025 – Mar 2026
        </p>
      </div>

      {/* ══════════════════════════════════════════════════════════════════════ */}
      {/*  1. ANÁLISIS DE GASTOS                                               */}
      {/* ══════════════════════════════════════════════════════════════════════ */}

      <Section num={1} title="Análisis de Gastos" desc="¿En qué gasta la familia? Responde la pregunta #1 del usuario." />

      <div className="grid gap-4 lg:grid-cols-2">
        {/* 1a. Treemap */}
        <Ch title="Gastos por subcategoría — Treemap" className="lg:col-span-2">
          <ResponsiveContainer width="100%" height={280}>
            <Treemap data={expenseTreemap} dataKey="size" content={<TreemapCell />}>
              <Tooltip formatter={(v: number) => [fmt(v), 'Gasto']} contentStyle={{ fontSize: 12, borderRadius: 8 }} />
            </Treemap>
          </ResponsiveContainer>
          <p className="text-[11px] text-muted-foreground mt-2">El tamaño de cada rectángulo representa la proporción del gasto. Hover para ver el monto.</p>
        </Ch>

        {/* 1b. Top 10 horizontal bars */}
        <Ch title="Top 10 subcategorías de gasto">
          <ResponsiveContainer width="100%" height={H + 30}>
            <BarChart data={topSubcats} layout="vertical" margin={{ left: 10, right: 10 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" horizontal={false} />
              <XAxis type="number" tickFormatter={fmtK} tick={{ fontSize: 11 }} axisLine={false} />
              <YAxis type="category" dataKey="name" width={85} tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
              <Tooltip content={<Tip />} cursor={{ fill: 'hsl(var(--muted))' }} />
              <Bar dataKey="value" name="Gasto" fill={EXPENSE} radius={[0, 4, 4, 0]} barSize={18} />
            </BarChart>
          </ResponsiveContainer>
        </Ch>

        {/* 1c. Ordinarios vs Extraordinarios */}
        <Ch title="Ordinarios vs Extraordinarios">
          <div className="flex items-center gap-6">
            <ResponsiveContainer width={180} height={180}>
              <PieChart>
                <Pie data={ordVsExt} dataKey="value" cx="50%" cy="50%" innerRadius={50} outerRadius={80} paddingAngle={3} strokeWidth={0}>
                  <Cell fill={P[0]} />
                  <Cell fill={P[6]} />
                </Pie>
                <Tooltip formatter={(v: number) => [fmt(v), '']} contentStyle={{ fontSize: 12, borderRadius: 8 }} />
              </PieChart>
            </ResponsiveContainer>
            <div className="space-y-3 text-sm">
              <div className="flex items-center gap-2">
                <div className="w-3 h-3 rounded-sm" style={{ background: P[0] }} />
                <span className="text-muted-foreground">Ordinarios</span>
                <span className="font-semibold ml-auto">{fmt(650000)}</span>
              </div>
              <div className="flex items-center gap-2">
                <div className="w-3 h-3 rounded-sm" style={{ background: P[6] }} />
                <span className="text-muted-foreground">Extraordinarios</span>
                <span className="font-semibold ml-auto">{fmt(130000)}</span>
              </div>
              <p className="text-[11px] text-muted-foreground pt-1">Separa lo recurrente (alquiler, servicios) de lo excepcional (arreglo auto, compra única).</p>
            </div>
          </div>
        </Ch>
      </div>

      {/* 1d. Category evolution stacked */}
      <Ch title="Evolución de gastos por categoría — últimos 6 meses">
        <ResponsiveContainer width="100%" height={H + 20}>
          <BarChart data={catEvolution} barCategoryGap="20%">
            <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
            <XAxis dataKey="mes" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
            <YAxis tickFormatter={fmtK} tick={{ fontSize: 11 }} width={50} axisLine={false} tickLine={false} />
            <Tooltip content={<Tip />} cursor={{ fill: 'hsl(var(--muted))' }} />
            <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
            {CAT_KEYS.map((k, i) => (
              <Bar key={k} dataKey={k} stackId="a" fill={P[i % P.length]} />
            ))}
          </BarChart>
        </ResponsiveContainer>
      </Ch>

      {/* ══════════════════════════════════════════════════════════════════════ */}
      {/*  2. ANÁLISIS DE INGRESOS                                             */}
      {/* ══════════════════════════════════════════════════════════════════════ */}

      <Section num={2} title="Análisis de Ingresos" desc="¿De dónde viene la plata?" />

      <div className="grid gap-4 lg:grid-cols-2">
        {/* 2a. Income donut */}
        <Ch title="Ingresos por fuente — mes actual">
          <div className="flex items-center gap-6">
            <ResponsiveContainer width={180} height={180}>
              <PieChart>
                <Pie data={incomeSources} dataKey="value" cx="50%" cy="50%" innerRadius={50} outerRadius={80} paddingAngle={3} strokeWidth={0}>
                  {incomeSources.map((_, i) => <Cell key={i} fill={P[i]} />)}
                </Pie>
                <Tooltip formatter={(v: number) => [fmt(v), '']} contentStyle={{ fontSize: 12, borderRadius: 8 }} />
              </PieChart>
            </ResponsiveContainer>
            <div className="space-y-2 text-sm">
              {incomeSources.map((s, i) => (
                <div key={s.name} className="flex items-center gap-2">
                  <div className="w-3 h-3 rounded-sm shrink-0" style={{ background: P[i] }} />
                  <span className="text-muted-foreground">{s.name}</span>
                  <span className="font-semibold ml-auto">{fmt(s.value)}</span>
                </div>
              ))}
            </div>
          </div>
        </Ch>

        {/* 2b. Income evolution */}
        <Ch title="Evolución de ingresos por fuente">
          <ResponsiveContainer width="100%" height={H}>
            <BarChart data={incomeEvolution} barCategoryGap="25%">
              <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
              <XAxis dataKey="mes" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
              <YAxis tickFormatter={fmtK} tick={{ fontSize: 11 }} width={50} axisLine={false} tickLine={false} />
              <Tooltip content={<Tip />} cursor={{ fill: 'hsl(var(--muted))' }} />
              <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
              {INC_KEYS.map((k, i) => (
                <Bar key={k} dataKey={k} stackId="a" fill={P[i % P.length]} />
              ))}
            </BarChart>
          </ResponsiveContainer>
        </Ch>
      </div>

      {/* ══════════════════════════════════════════════════════════════════════ */}
      {/*  3. FLUJO DE CAJA                                                    */}
      {/* ══════════════════════════════════════════════════════════════════════ */}

      <Section num={3} title="Flujo de Caja" desc="¿Cómo se mueve el dinero día a día y qué compromisos futuros hay?" />

      <div className="grid gap-4 lg:grid-cols-2">
        {/* 3a. Daily cash flow */}
        <Ch title="Cash flow diario — Marzo 2026" className="lg:col-span-2">
          <ResponsiveContainer width="100%" height={H + 20}>
            <ComposedChart data={dailyCashFlow} barGap={0}>
              <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
              <XAxis dataKey="dia" tick={{ fontSize: 10 }} axisLine={false} tickLine={false} />
              <YAxis yAxisId="bars" tickFormatter={fmtK} tick={{ fontSize: 11 }} width={50} axisLine={false} tickLine={false} />
              <YAxis yAxisId="line" orientation="right" tickFormatter={fmtK} tick={{ fontSize: 11 }} width={55} axisLine={false} tickLine={false} />
              <Tooltip content={<Tip />} cursor={{ fill: 'hsl(var(--muted))' }} />
              <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
              <Bar yAxisId="bars" dataKey="ingresos" name="Ingresos" fill={INCOME} radius={[3, 3, 0, 0]} />
              <Bar yAxisId="bars" dataKey="egresos" name="Egresos" fill={EXPENSE} radius={[3, 3, 0, 0]} />
              <Line yAxisId="line" dataKey="acumulado" name="Saldo acumulado" type="monotone"
                stroke={EMERALD} strokeWidth={2} dot={false} />
            </ComposedChart>
          </ResponsiveContainer>
          <p className="text-[11px] text-muted-foreground mt-1">Barras = flujo diario. Línea verde = saldo acumulado (eje derecho).</p>
        </Ch>

        {/* 3b. CC future installments */}
        <Ch title="Cuotas comprometidas — próximos 12 meses" className="lg:col-span-2">
          <ResponsiveContainer width="100%" height={H}>
            <BarChart data={ccFuture} barCategoryGap="20%">
              <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
              <XAxis dataKey="mes" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
              <YAxis tickFormatter={fmtK} tick={{ fontSize: 11 }} width={50} axisLine={false} tickLine={false} />
              <Tooltip content={<Tip />} cursor={{ fill: 'hsl(var(--muted))' }} />
              <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
              <Bar dataKey="Visa" stackId="a" fill={P[0]} radius={[0, 0, 0, 0]} />
              <Bar dataKey="Master" stackId="a" fill={P[1]} radius={[3, 3, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
          <p className="text-[11px] text-muted-foreground mt-1">Proyección de cuotas de tarjeta pendientes. Crucial para planificación financiera.</p>
        </Ch>
      </div>

      {/* ══════════════════════════════════════════════════════════════════════ */}
      {/*  4. CENTROS DE COSTO                                                 */}
      {/* ══════════════════════════════════════════════════════════════════════ */}

      <Section num={4} title="Centros de Costo" desc="¿Cuánto le dedicamos a cada área de la vida?" />

      <div className="grid gap-4 lg:grid-cols-2">
        {/* 4a. Cost center donut */}
        <Ch title="Gastos por centro de costo — mes actual">
          <div className="flex items-center gap-6">
            <ResponsiveContainer width={180} height={180}>
              <PieChart>
                <Pie data={costCenters} dataKey="value" cx="50%" cy="50%" innerRadius={50} outerRadius={80} paddingAngle={3} strokeWidth={0}>
                  {costCenters.map((_, i) => <Cell key={i} fill={P[i]} />)}
                </Pie>
                <Tooltip formatter={(v: number) => [fmt(v), '']} contentStyle={{ fontSize: 12, borderRadius: 8 }} />
              </PieChart>
            </ResponsiveContainer>
            <div className="space-y-2 text-sm">
              {costCenters.map((cc, i) => (
                <div key={cc.name} className="flex items-center gap-2">
                  <div className="w-3 h-3 rounded-sm shrink-0" style={{ background: P[i] }} />
                  <span className="text-muted-foreground">{cc.name}</span>
                  <span className="font-semibold ml-auto">{fmtK(cc.value)}</span>
                </div>
              ))}
            </div>
          </div>
        </Ch>

        {/* 4b. Cost center evolution */}
        <Ch title="Evolución por centro de costo">
          <ResponsiveContainer width="100%" height={H}>
            <LineChart data={ccEvol}>
              <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
              <XAxis dataKey="mes" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
              <YAxis tickFormatter={fmtK} tick={{ fontSize: 11 }} width={50} axisLine={false} tickLine={false} />
              <Tooltip content={<Tip />} />
              <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
              {CC_KEYS.map((k, i) => (
                <Line key={k} dataKey={k} type="monotone" stroke={P[i % P.length]} strokeWidth={2} dot={{ r: 3 }} />
              ))}
            </LineChart>
          </ResponsiveContainer>
        </Ch>
      </div>

      {/* ══════════════════════════════════════════════════════════════════════ */}
      {/*  5. MEDIOS DE PAGO                                                   */}
      {/* ══════════════════════════════════════════════════════════════════════ */}

      <Section num={5} title="Medios de Pago" desc="¿Cómo paga la familia? ¿Está migrando a digital?" />

      <div className="grid gap-4 lg:grid-cols-2">
        {/* 5a. Payment method donut */}
        <Ch title="Distribución por medio de pago — mes actual">
          <div className="flex items-center gap-6">
            <ResponsiveContainer width={180} height={180}>
              <PieChart>
                <Pie data={paymentMethods} dataKey="value" cx="50%" cy="50%" innerRadius={50} outerRadius={80} paddingAngle={3} strokeWidth={0}>
                  <Cell fill={P[5]} /> {/* Efectivo = verde */}
                  <Cell fill={P[0]} /> {/* Banco = azul */}
                  <Cell fill={P[6]} /> {/* Tarjeta = amarillo */}
                </Pie>
                <Tooltip formatter={(v: number) => [fmt(v), '']} contentStyle={{ fontSize: 12, borderRadius: 8 }} />
              </PieChart>
            </ResponsiveContainer>
            <div className="space-y-2 text-sm">
              {paymentMethods.map((pm, i) => (
                <div key={pm.name} className="flex items-center gap-2">
                  <div className="w-3 h-3 rounded-sm shrink-0" style={{ background: [P[5], P[0], P[6]][i] }} />
                  <span className="text-muted-foreground">{pm.name}</span>
                  <span className="font-semibold ml-auto">{fmtK(pm.value)}</span>
                </div>
              ))}
            </div>
          </div>
        </Ch>

        {/* 5b. Payment method evolution 100% stacked */}
        <Ch title="Evolución de medios de pago — % del total">
          <ResponsiveContainer width="100%" height={H}>
            <BarChart data={pmEvolution} stackOffset="expand" barCategoryGap="20%">
              <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
              <XAxis dataKey="mes" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
              <YAxis tickFormatter={fmtPct} tick={{ fontSize: 11 }} width={40} axisLine={false} tickLine={false} />
              <Tooltip content={<Tip />} />
              <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
              <Bar dataKey="Efectivo" stackId="a" fill={P[5]} />
              <Bar dataKey="Banco" stackId="a" fill={P[0]} />
              <Bar dataKey="Tarjeta" stackId="a" fill={P[6]} />
            </BarChart>
          </ResponsiveContainer>
        </Ch>
      </div>

      {/* ══════════════════════════════════════════════════════════════════════ */}
      {/*  6. TARJETAS DE CRÉDITO                                              */}
      {/* ══════════════════════════════════════════════════════════════════════ */}

      <Section num={6} title="Tarjetas de Crédito" desc="Deuda, proyección de cuotas, uso por miembro y costo de las tarjetas." />

      {/* 6a. KPI cards */}
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Kpi label="Deuda total TC" value={fmt(480000)} sub="-5.2% vs mes anterior" trend="up" color={EXPENSE} />
        <Kpi label="Cuotas próx. mes" value={fmt(213000)} sub="Visa $125k + Master $88k" />
        <Kpi label="Cuotas a 6 meses" value={fmt(1041000)} sub="Compromiso acumulado" color={P[6]} />
        <Kpi label="Cargos netos (mes)" value={fmt(5300)} sub="$9.8k cargos - $4.5k bonif." trend="down" color={EXPENSE} />
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        {/* 6b. Member spending */}
        <Ch title="Gasto por miembro de tarjeta — mes actual">
          <ResponsiveContainer width="100%" height={H}>
            <BarChart data={ccMembers} barCategoryGap="30%">
              <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
              <XAxis dataKey="name" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
              <YAxis tickFormatter={fmtK} tick={{ fontSize: 11 }} width={50} axisLine={false} tickLine={false} />
              <Tooltip content={<Tip />} cursor={{ fill: 'hsl(var(--muted))' }} />
              <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
              <Bar dataKey="Visa" fill={P[0]} radius={[3, 3, 0, 0]} />
              <Bar dataKey="Master" fill={P[1]} radius={[3, 3, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </Ch>

        {/* 6c. Charges vs bonuses */}
        <Ch title="Cargos vs Bonificaciones por mes">
          <ResponsiveContainer width="100%" height={H}>
            <ComposedChart data={ccCharges} barCategoryGap="25%">
              <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
              <XAxis dataKey="mes" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
              <YAxis tickFormatter={fmtK} tick={{ fontSize: 11 }} width={50} axisLine={false} tickLine={false} />
              <Tooltip content={<Tip />} cursor={{ fill: 'hsl(var(--muted))' }} />
              <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
              <Bar dataKey="Cargos" fill={EXPENSE} radius={[3, 3, 0, 0]} />
              <Bar dataKey="Bonificaciones" fill={EMERALD} radius={[3, 3, 0, 0]} />
              <Line dataKey="Cargos" type="monotone" stroke={EXPENSE} strokeWidth={1.5} dot={false} strokeDasharray="4 2" legendType="none" />
            </ComposedChart>
          </ResponsiveContainer>
        </Ch>
      </div>

      {/* ══════════════════════════════════════════════════════════════════════ */}
      {/*  7. ANÁLISIS BIMONETARIO                                             */}
      {/* ══════════════════════════════════════════════════════════════════════ */}

      <Section num={7} title="Análisis Bimonetario" desc="TC efectivo, composición por moneda, y ganancia/pérdida cambiaria." />

      <div className="grid gap-4 lg:grid-cols-2">
        {/* 7a. Exchange rate */}
        <Ch title="Tipo de cambio — efectivo vs mercado (ARS/USD)">
          <ResponsiveContainer width="100%" height={H}>
            <LineChart data={tcData}>
              <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
              <XAxis dataKey="mes" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fontSize: 11 }} width={50} axisLine={false} tickLine={false} domain={['dataMin - 20', 'dataMax + 20']} />
              <Tooltip contentStyle={{ fontSize: 12, borderRadius: 8 }} />
              <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
              <Line dataKey="TC efectivo" type="monotone" stroke={P[6]} strokeWidth={2.5} dot={{ r: 4, fill: P[6] }} />
              <Line dataKey="TC mercado" type="monotone" stroke={P[9]} strokeWidth={2} dot={{ r: 3 }} strokeDasharray="5 3" />
            </LineChart>
          </ResponsiveContainer>
          <p className="text-[11px] text-muted-foreground mt-1">TC efectivo = promedio ponderado de los TC usados en movimientos. Si está por encima del mercado, estás pagando un spread.</p>
        </Ch>

        {/* 7b. Currency composition */}
        <Ch title="Composición patrimonial por moneda">
          <ResponsiveContainer width="100%" height={H}>
            <AreaChart data={currComp}>
              <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
              <XAxis dataKey="mes" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
              <YAxis tickFormatter={(v) => `${v}%`} tick={{ fontSize: 11 }} width={40} axisLine={false} tickLine={false} domain={[0, 100]} />
              <Tooltip formatter={(v: number) => [`${v}%`, '']} contentStyle={{ fontSize: 12, borderRadius: 8 }} />
              <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
              <Area dataKey="USD" type="monotone" stackId="1" fill={P[0]} stroke={P[0]} fillOpacity={0.7} />
              <Area dataKey="ARS" type="monotone" stackId="1" fill={P[6]} stroke={P[6]} fillOpacity={0.7} />
            </AreaChart>
          </ResponsiveContainer>
          <p className="text-[11px] text-muted-foreground mt-1">Tendencia clara: la familia está migrando patrimonio a USD (hedging contra devaluación).</p>
        </Ch>
      </div>

      {/* 7c. Cambiaria KPIs */}
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Kpi label="Ganancia cambiaria TC" value="USD 45" sub="Cuotas pagadas más baratas en USD" trend="up" color={EMERALD} />
        <Kpi label="Pérdida cambiaria TC" value="USD 12" sub="Cuotas que salieron más caras" trend="down" color={EXPENSE} />
        <Kpi label="Neto cambiario" value="USD +33" sub="Beneficio neto por devaluación" trend="up" color={EMERALD} />
        <Kpi label="TC promedio ponderado" value="$1.260" sub="+26% desde Octubre" />
      </div>

      {/* ══════════════════════════════════════════════════════════════════════ */}
      {/*  8. BALANCE Y AHORRO                                                 */}
      {/* ══════════════════════════════════════════════════════════════════════ */}

      <Section num={8} title="Balance General y Ahorro" desc="Activos vs pasivos, patrimonio neto real, y ratio de ahorro." />

      {/* 8a. KPIs */}
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Kpi label="Activos totales" value={fmt(4325000)} sub="Cajas + Bancos (en ARS)" trend="up" color={INCOME} />
        <Kpi label="Pasivos (deuda TC)" value={fmt(480000)} sub="Cuotas pendientes" trend="up" color={EXPENSE} />
        <Kpi label="Patrimonio neto real" value={fmt(3845000)} sub="Activos - Pasivos" trend="up" color={EMERALD} />
        <Kpi label="Ratio de ahorro (mes)" value="17.9%" sub="De cada $100, ahorrás $18" trend="up" color={EMERALD} />
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        {/* 8b. Assets vs liabilities visual */}
        <Ch title="Activos vs Pasivos">
          <div className="space-y-4 pt-2">
            {/* Activos bar */}
            <div>
              <div className="flex justify-between text-xs mb-1">
                <span className="font-semibold text-muted-foreground">ACTIVOS</span>
                <span className="font-semibold">{fmt(4325000)}</span>
              </div>
              <div className="flex h-8 rounded-md overflow-hidden">
                <div className="flex items-center justify-center text-[10px] text-white font-medium" style={{ width: '3%', background: P[5] }} title="Cajas ARS $40k" />
                <div className="flex items-center justify-center text-[10px] text-white font-medium" style={{ width: '27%', background: P[0] }}>Banco Nac.</div>
                <div className="flex items-center justify-center text-[10px] text-white font-medium" style={{ width: '9%', background: P[1] }}>Brubank</div>
                <div className="flex items-center justify-center text-[10px] text-white font-medium" style={{ width: '43%', background: P[2] }}>Caja USD (equiv.)</div>
                <div className="flex items-center justify-center text-[10px] text-white font-medium" style={{ width: '18%', background: P[3] }}>Wise USD</div>
              </div>
            </div>
            {/* Pasivos bar */}
            <div>
              <div className="flex justify-between text-xs mb-1">
                <span className="font-semibold text-muted-foreground">PASIVOS</span>
                <span className="font-semibold" style={{ color: EXPENSE }}>{fmt(480000)}</span>
              </div>
              <div className="flex h-8 rounded-md overflow-hidden">
                <div className="flex items-center justify-center text-[10px] text-white font-medium" style={{ width: '58%', background: EXPENSE }}>Visa $280k</div>
                <div className="flex items-center justify-center text-[10px] text-white font-medium" style={{ width: '42%', background: '#fca5a5' }}>Master $200k</div>
              </div>
              <div className="h-8" /> {/* spacer to show proportion */}
            </div>
            {/* Net */}
            <div className="border-t pt-3 flex justify-between items-center">
              <span className="text-sm font-semibold">Patrimonio neto</span>
              <span className="text-lg font-bold" style={{ color: EMERALD }}>{fmt(3845000)}</span>
            </div>
          </div>
        </Ch>

        {/* 8c. Savings ratio */}
        <Ch title="Ratio de ahorro mensual (%)">
          <ResponsiveContainer width="100%" height={H}>
            <ComposedChart data={savingsRatio}>
              <defs>
                <linearGradient id="gradSav" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor={EMERALD} stopOpacity={0.3} />
                  <stop offset="95%" stopColor={EMERALD} stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
              <XAxis dataKey="mes" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
              <YAxis tickFormatter={(v) => `${v}%`} tick={{ fontSize: 11 }} width={40} axisLine={false} tickLine={false} domain={[0, 25]} />
              <Tooltip formatter={(v: number) => [`${v}%`, 'Ahorro']} contentStyle={{ fontSize: 12, borderRadius: 8 }} />
              <Area dataKey="ratio" type="monotone" fill="url(#gradSav)" stroke={EMERALD} strokeWidth={2.5} dot={{ r: 4, fill: EMERALD }} />
              {/* Reference line at average */}
              <Line dataKey={() => 14.5} name="Promedio" type="monotone" stroke={P[9]} strokeWidth={1} strokeDasharray="6 3" dot={false} />
            </ComposedChart>
          </ResponsiveContainer>
          <p className="text-[11px] text-muted-foreground mt-1">Diciembre baja por gastos de fiestas. Febrero se recupera fuerte.</p>
        </Ch>
      </div>

      {/* ══════════════════════════════════════════════════════════════════════ */}
      {/*  9. TRANSFERENCIAS                                                   */}
      {/* ══════════════════════════════════════════════════════════════════════ */}

      <Section num={9} title="Transferencias" desc="Volumen de movimientos entre cuentas propias." />

      <div className="grid gap-4 lg:grid-cols-2">
        {/* 9a. Volume */}
        <Ch title="Volumen de transferencias mensuales">
          <ResponsiveContainer width="100%" height={H}>
            <ComposedChart data={transferVol}>
              <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
              <XAxis dataKey="mes" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
              <YAxis yAxisId="monto" tickFormatter={fmtK} tick={{ fontSize: 11 }} width={50} axisLine={false} tickLine={false} />
              <YAxis yAxisId="cant" orientation="right" tick={{ fontSize: 11 }} width={30} axisLine={false} tickLine={false} />
              <Tooltip content={<Tip />} cursor={{ fill: 'hsl(var(--muted))' }} />
              <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
              <Bar yAxisId="monto" dataKey="monto" name="Monto" fill={P[2]} radius={[3, 3, 0, 0]} />
              <Line yAxisId="cant" dataKey="cantidad" name="Cantidad" type="monotone"
                stroke={P[4]} strokeWidth={2} dot={{ r: 3, fill: P[4] }} />
            </ComposedChart>
          </ResponsiveContainer>
        </Ch>

        {/* 9b. Top flows table */}
        <Ch title="Principales flujos entre cuentas (6 meses)">
          <div className="space-y-2 pt-1">
            {topFlows.map((f, i) => (
              <div key={i} className="flex items-center gap-2 text-sm py-2 border-b last:border-0">
                <div className="w-6 h-6 rounded-full flex items-center justify-center text-[10px] font-bold text-white shrink-0" style={{ background: P[i % P.length] }}>{i + 1}</div>
                <span className="text-muted-foreground truncate">{f.from}</span>
                <ArrowRight className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
                <span className="text-muted-foreground truncate">{f.to}</span>
                <span className="ml-auto font-semibold shrink-0">{fmtK(f.monto)}</span>
                <span className="text-xs text-muted-foreground shrink-0">({f.count}x)</span>
              </div>
            ))}
          </div>
          <p className="text-[11px] text-muted-foreground mt-3">Un diagrama Sankey mostraría estos flujos visualmente, pero requiere librería adicional.</p>
        </Ch>
      </div>

      {/* ══════════════════════════════════════════════════════════════════════ */}

      <div className="text-center text-sm text-muted-foreground pt-8 border-t">
        Fin del prototipo — 24 gráficos en 9 secciones. Todos con datos ficticios.
      </div>
    </div>
  )
}
