import { useState, useRef, useEffect, useMemo } from 'react'
import { Input } from '@/components/ui/input'
import {
  Home, ShoppingCart, UtensilsCrossed, Car, Bus, Plane,
  Heart, GraduationCap, Gamepad2, Music, Tv, Film,
  Shirt, Gift, Baby, Dog, Dumbbell, Stethoscope,
  Pill, Scissors, Wrench, Hammer, Lightbulb, Droplets,
  Flame, Wifi, Phone, Smartphone, Laptop, Monitor,
  Printer, HardDrive, CreditCard, Banknote, PiggyBank, TrendingUp,
  Receipt, FileText, BookOpen, Newspaper, Building2, Church,
  TreePine, Flower2, Sun, Umbrella, CloudRain, Snowflake,
  Coffee, Wine, Beer, Pizza, Apple, Egg,
  Cigarette, Fuel, ParkingCircle, Train, Ship, Bike,
  Key, Lock, Shield, Scale, Gavel, Users,
  UserPlus, Briefcase, HandCoins, Coins, CircleDollarSign, Wallet,
  Landmark, Store, Package, Truck, MapPin, Globe,
  Calendar, Clock, AlertTriangle, Star, Zap,
  // Additional icons
  Trophy, Medal, Volleyball, Dumbbell as Dumbbell2,
  Armchair, Sofa, Bath, WashingMachine, Refrigerator,
  Paintbrush, PaintBucket, Ruler, Plug, Fan,
  Cat, Fish, Bug, Bird, Leaf,
  Palette, Camera, Headphones, Radio, Theater,
  Tent, Mountain, Waves, Anchor,
  Syringe, Thermometer, Ambulance,
  School, Library, PenLine, Languages,
  Handshake, HeartHandshake, PartyPopper, Cake,
  BabyIcon, Glasses, Watch, Gem, Crown,
  type LucideIcon,
} from 'lucide-react'

const ICONS: { name: string; icon: LucideIcon; tags: string }[] = [
  // Hogar
  { name: 'home', icon: Home, tags: 'hogar casa vivienda' },
  { name: 'lightbulb', icon: Lightbulb, tags: 'luz electricidad energia' },
  { name: 'droplets', icon: Droplets, tags: 'agua servicio' },
  { name: 'flame', icon: Flame, tags: 'gas calefaccion' },
  { name: 'wifi', icon: Wifi, tags: 'internet conexion' },
  { name: 'phone', icon: Phone, tags: 'telefono comunicacion' },
  { name: 'wrench', icon: Wrench, tags: 'reparacion mantenimiento' },
  { name: 'hammer', icon: Hammer, tags: 'construccion obra' },
  { name: 'key', icon: Key, tags: 'alquiler llave' },
  // Compras
  { name: 'shopping-cart', icon: ShoppingCart, tags: 'compras supermercado' },
  { name: 'store', icon: Store, tags: 'tienda negocio comercio' },
  { name: 'package', icon: Package, tags: 'paquete envio' },
  { name: 'shirt', icon: Shirt, tags: 'ropa vestimenta' },
  { name: 'gift', icon: Gift, tags: 'regalo obsequio' },
  // Alimentacion
  { name: 'utensils-crossed', icon: UtensilsCrossed, tags: 'comida restaurante alimentacion' },
  { name: 'coffee', icon: Coffee, tags: 'cafe bar' },
  { name: 'pizza', icon: Pizza, tags: 'comida rapida delivery' },
  { name: 'apple', icon: Apple, tags: 'fruta verdura mercado' },
  { name: 'egg', icon: Egg, tags: 'huevo almacen' },
  { name: 'wine', icon: Wine, tags: 'vino bebida alcohol' },
  { name: 'beer', icon: Beer, tags: 'cerveza bebida' },
  // Transporte
  { name: 'car', icon: Car, tags: 'auto vehiculo' },
  { name: 'bus', icon: Bus, tags: 'colectivo transporte publico' },
  { name: 'train', icon: Train, tags: 'tren subte' },
  { name: 'plane', icon: Plane, tags: 'avion viaje vuelo' },
  { name: 'bike', icon: Bike, tags: 'bicicleta' },
  { name: 'ship', icon: Ship, tags: 'barco crucero' },
  { name: 'fuel', icon: Fuel, tags: 'nafta combustible gasolina' },
  { name: 'parking-circle', icon: ParkingCircle, tags: 'estacionamiento parking' },
  { name: 'truck', icon: Truck, tags: 'camion mudanza flete' },
  // Salud
  { name: 'heart', icon: Heart, tags: 'salud corazon' },
  { name: 'stethoscope', icon: Stethoscope, tags: 'medico doctor consulta' },
  { name: 'pill', icon: Pill, tags: 'medicamento farmacia' },
  { name: 'dumbbell', icon: Dumbbell, tags: 'gimnasio deporte ejercicio' },
  // Educacion
  { name: 'graduation-cap', icon: GraduationCap, tags: 'educacion universidad escuela' },
  { name: 'book-open', icon: BookOpen, tags: 'libro estudio lectura' },
  // Entretenimiento
  { name: 'gamepad-2', icon: Gamepad2, tags: 'juegos videojuegos' },
  { name: 'music', icon: Music, tags: 'musica spotify' },
  { name: 'tv', icon: Tv, tags: 'television streaming netflix' },
  { name: 'film', icon: Film, tags: 'cine pelicula' },
  // Tecnologia
  { name: 'smartphone', icon: Smartphone, tags: 'celular movil' },
  { name: 'laptop', icon: Laptop, tags: 'computadora notebook' },
  { name: 'monitor', icon: Monitor, tags: 'pantalla pc' },
  { name: 'printer', icon: Printer, tags: 'impresora oficina' },
  { name: 'hard-drive', icon: HardDrive, tags: 'almacenamiento disco' },
  // Finanzas
  { name: 'credit-card', icon: CreditCard, tags: 'tarjeta credito debito' },
  { name: 'banknote', icon: Banknote, tags: 'billete efectivo' },
  { name: 'piggy-bank', icon: PiggyBank, tags: 'ahorro alcancia' },
  { name: 'trending-up', icon: TrendingUp, tags: 'inversion rendimiento' },
  { name: 'hand-coins', icon: HandCoins, tags: 'prestamo donacion' },
  { name: 'coins', icon: Coins, tags: 'monedas cambio' },
  { name: 'circle-dollar-sign', icon: CircleDollarSign, tags: 'dinero dolar' },
  { name: 'wallet', icon: Wallet, tags: 'billetera' },
  { name: 'landmark', icon: Landmark, tags: 'banco institucion' },
  { name: 'receipt', icon: Receipt, tags: 'factura recibo' },
  // Familia
  { name: 'baby', icon: Baby, tags: 'bebe hijo nino' },
  { name: 'dog', icon: Dog, tags: 'mascota perro veterinario' },
  { name: 'users', icon: Users, tags: 'familia personas grupo' },
  { name: 'user-plus', icon: UserPlus, tags: 'empleado servicio domestico' },
  // Trabajo
  { name: 'briefcase', icon: Briefcase, tags: 'trabajo empleo oficina' },
  { name: 'building-2', icon: Building2, tags: 'empresa oficina corporativo' },
  { name: 'file-text', icon: FileText, tags: 'documento impuesto tramite' },
  { name: 'newspaper', icon: Newspaper, tags: 'diario revista suscripcion' },
  // Varios
  { name: 'scissors', icon: Scissors, tags: 'peluqueria estetica' },
  { name: 'cigarette', icon: Cigarette, tags: 'tabaco cigarrillo' },
  { name: 'church', icon: Church, tags: 'religion templo donacion' },
  { name: 'tree-pine', icon: TreePine, tags: 'jardin naturaleza' },
  { name: 'flower-2', icon: Flower2, tags: 'flores jardin' },
  { name: 'sun', icon: Sun, tags: 'vacaciones verano' },
  { name: 'umbrella', icon: Umbrella, tags: 'seguro proteccion' },
  { name: 'cloud-rain', icon: CloudRain, tags: 'clima emergencia' },
  { name: 'snowflake', icon: Snowflake, tags: 'aire acondicionado frio' },
  { name: 'shield', icon: Shield, tags: 'seguridad seguro proteccion' },
  { name: 'scale', icon: Scale, tags: 'legal justicia abogado' },
  { name: 'gavel', icon: Gavel, tags: 'legal juicio abogado' },
  { name: 'lock', icon: Lock, tags: 'seguridad cerradura' },
  { name: 'map-pin', icon: MapPin, tags: 'ubicacion lugar' },
  { name: 'globe', icon: Globe, tags: 'internacional viaje exterior' },
  { name: 'calendar', icon: Calendar, tags: 'evento fecha agenda' },
  { name: 'clock', icon: Clock, tags: 'tiempo hora' },
  { name: 'alert-triangle', icon: AlertTriangle, tags: 'emergencia urgencia' },
  { name: 'star', icon: Star, tags: 'favorito especial' },
  { name: 'zap', icon: Zap, tags: 'energia rapido' },
  // Deportes y recreacion
  { name: 'trophy', icon: Trophy, tags: 'trofeo premio deporte' },
  { name: 'medal', icon: Medal, tags: 'medalla premio deporte' },
  { name: 'volleyball', icon: Volleyball, tags: 'voley deporte pelota' },
  // Hogar extra
  { name: 'armchair', icon: Armchair, tags: 'sillon mueble hogar' },
  { name: 'sofa', icon: Sofa, tags: 'sofa mueble living' },
  { name: 'bath', icon: Bath, tags: 'baño bañera' },
  { name: 'washing-machine', icon: WashingMachine, tags: 'lavarropas lavadora ropa' },
  { name: 'refrigerator', icon: Refrigerator, tags: 'heladera refrigerador electrodomestico' },
  { name: 'paintbrush', icon: Paintbrush, tags: 'pintura decoracion' },
  { name: 'paint-bucket', icon: PaintBucket, tags: 'pintura obra' },
  { name: 'ruler', icon: Ruler, tags: 'medida obra construccion' },
  { name: 'plug', icon: Plug, tags: 'enchufe electricidad' },
  { name: 'fan', icon: Fan, tags: 'ventilador aire' },
  // Mascotas
  { name: 'cat', icon: Cat, tags: 'gato mascota' },
  { name: 'fish', icon: Fish, tags: 'pez pescado acuario' },
  { name: 'bug', icon: Bug, tags: 'insecto fumigacion' },
  { name: 'bird', icon: Bird, tags: 'pajaro ave mascota' },
  { name: 'leaf', icon: Leaf, tags: 'hoja planta jardin ecologia' },
  // Arte y cultura
  { name: 'palette', icon: Palette, tags: 'arte pintura dibujo' },
  { name: 'camera', icon: Camera, tags: 'foto fotografia' },
  { name: 'headphones', icon: Headphones, tags: 'auriculares musica audio' },
  { name: 'radio', icon: Radio, tags: 'radio musica' },
  { name: 'theater', icon: Theater, tags: 'teatro espectaculo obra' },
  // Aire libre
  { name: 'tent', icon: Tent, tags: 'camping carpa aire libre' },
  { name: 'mountain', icon: Mountain, tags: 'montaña excursion trekking' },
  { name: 'waves', icon: Waves, tags: 'mar playa piscina pileta' },
  { name: 'anchor', icon: Anchor, tags: 'nautica barco puerto' },
  // Salud extra
  { name: 'syringe', icon: Syringe, tags: 'vacuna inyeccion laboratorio' },
  { name: 'thermometer', icon: Thermometer, tags: 'fiebre temperatura' },
  { name: 'ambulance', icon: Ambulance, tags: 'emergencia hospital' },
  // Educacion extra
  { name: 'school', icon: School, tags: 'colegio escuela primaria secundaria' },
  { name: 'library', icon: Library, tags: 'biblioteca estudio' },
  { name: 'pen-line', icon: PenLine, tags: 'escribir utiles lapiz' },
  { name: 'languages', icon: Languages, tags: 'idioma curso clase' },
  // Social
  { name: 'handshake', icon: Handshake, tags: 'acuerdo negocio trato' },
  { name: 'heart-handshake', icon: HeartHandshake, tags: 'caridad donacion solidaridad' },
  { name: 'party-popper', icon: PartyPopper, tags: 'fiesta cumpleaños celebracion' },
  { name: 'cake', icon: Cake, tags: 'torta cumpleaños pasteleria' },
  // Personal
  { name: 'glasses', icon: Glasses, tags: 'lentes optica vision' },
  { name: 'watch', icon: Watch, tags: 'reloj accesorio' },
  { name: 'gem', icon: Gem, tags: 'joya joyeria' },
  { name: 'crown', icon: Crown, tags: 'corona lujo premium' },
]

/** Resolves an icon name to the Lucide component */
export function getIconComponent(name: string | null | undefined): LucideIcon | null {
  if (!name) return null
  return ICONS.find((i) => i.name === name)?.icon ?? null
}

interface IconPickerProps {
  value: string
  onChange: (value: string) => void
}

export function IconPicker({ value, onChange }: IconPickerProps) {
  const [open, setOpen] = useState(false)
  const [search, setSearch] = useState('')
  const containerRef = useRef<HTMLDivElement>(null)

  const filtered = useMemo(() => {
    if (!search.trim()) return ICONS
    const q = search.toLowerCase()
    return ICONS.filter(
      (i) => i.name.includes(q) || i.tags.includes(q)
    )
  }, [search])

  useEffect(() => {
    const handle = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', handle)
    return () => document.removeEventListener('mousedown', handle)
  }, [])

  const SelectedIcon = getIconComponent(value)

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setOpen(!open)}
        className="flex h-9 w-full items-center gap-2 rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors hover:bg-accent"
      >
        {SelectedIcon ? (
          <>
            <SelectedIcon className="h-4 w-4" />
            <span className="text-muted-foreground">{value}</span>
          </>
        ) : (
          <span className="text-muted-foreground">Seleccionar icono...</span>
        )}
      </button>

      {open && (
        <div className="absolute z-50 mt-1 w-72 rounded-md border bg-popover p-2 shadow-md">
          <Input
            type="text"
            placeholder="Buscar icono..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="mb-2 h-8 text-sm"
            autoFocus
          />
          <div className="grid max-h-52 grid-cols-8 gap-1 overflow-y-auto">
            {filtered.map((item) => (
              <button
                key={item.name}
                type="button"
                title={item.name}
                onClick={() => {
                  onChange(item.name)
                  setOpen(false)
                  setSearch('')
                }}
                className={`flex h-8 w-8 items-center justify-center rounded transition-colors hover:bg-accent ${
                  value === item.name ? 'bg-primary text-primary-foreground' : ''
                }`}
              >
                <item.icon className="h-4 w-4" />
              </button>
            ))}
          </div>
          {filtered.length === 0 && (
            <p className="py-2 text-center text-sm text-muted-foreground">Sin resultados</p>
          )}
          {value && (
            <button
              type="button"
              onClick={() => {
                onChange('')
                setOpen(false)
              }}
              className="mt-1 w-full rounded py-1 text-center text-xs text-muted-foreground hover:bg-accent"
            >
              Quitar icono
            </button>
          )}
        </div>
      )}
    </div>
  )
}
