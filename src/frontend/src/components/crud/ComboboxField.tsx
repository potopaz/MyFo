import { useEffect, useMemo, useRef, useState } from 'react'
import { Input } from '@/components/ui/input'
import { Check } from 'lucide-react'

interface ComboboxFieldProps {
  id: string
  value: string
  onChange: (value: string) => void
  options?: { value: string; label: string }[]
  loadOptions?: () => Promise<{ value: string; label: string }[]>
  placeholder?: string
  required?: boolean
  disabled?: boolean
  className?: string
}

export function ComboboxField({
  id,
  value,
  onChange,
  options: staticOptions,
  loadOptions,
  placeholder,
  required,
  disabled,
  className,
}: ComboboxFieldProps) {
  // For async options (loadOptions): internal state
  // For static options: use the prop directly — no copying, always fresh
  const [asyncOptions, setAsyncOptions] = useState<{ value: string; label: string }[]>([])
  const options = staticOptions ?? asyncOptions

  const [search, setSearch] = useState('')
  const [open, setOpen] = useState(false)
  const [highlightIndex, setHighlightIndex] = useState(0)
  const containerRef = useRef<HTMLDivElement>(null)
  const listRef = useRef<HTMLUListElement>(null)

  // Load async options
  useEffect(() => {
    if (loadOptions) {
      loadOptions().then(setAsyncOptions)
    }
  }, [loadOptions])

  // Sync display text when value or options change (only when dropdown is closed)
  useEffect(() => {
    if (open) return
    if (value) {
      const opt = options.find((o) => o.value === value)
      setSearch(opt ? opt.label : value)
    } else {
      setSearch('')
    }
  }, [value, options, open])

  const filtered = useMemo(() => {
    if (!search.trim()) return options
    const q = search.toLowerCase()
    return options.filter((o) => o.label.toLowerCase().includes(q) || o.value.toLowerCase().includes(q))
  }, [options, search])

  // Reset highlight when filtered changes
  useEffect(() => {
    setHighlightIndex(0)
  }, [filtered])

  // Scroll highlighted item into view
  useEffect(() => {
    if (open && listRef.current) {
      const item = listRef.current.children[highlightIndex] as HTMLElement | undefined
      item?.scrollIntoView({ block: 'nearest' })
    }
  }, [highlightIndex, open])

  // Close on outside click
  useEffect(() => {
    const handle = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', handle)
    return () => document.removeEventListener('mousedown', handle)
  }, [])

  const select = (opt: { value: string; label: string }) => {
    onChange(opt.value)
    setSearch(opt.label)
    setOpen(false)
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (!open) {
      if (e.key === 'ArrowDown' || e.key === 'Enter') {
        e.preventDefault()
        setSearch('')
        setOpen(true)
      }
      return
    }
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setHighlightIndex((i) => Math.min(i + 1, filtered.length - 1))
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      setHighlightIndex((i) => Math.max(i - 1, 0))
    } else if (e.key === 'Enter') {
      e.preventDefault()
      if (filtered[highlightIndex]) select(filtered[highlightIndex])
    } else if (e.key === 'Escape') {
      setOpen(false)
    }
  }

  return (
    <div ref={containerRef} className="relative">
      <Input
        id={id}
        type="text"
        autoComplete="off"
        className={className}
        value={search}
        onChange={(e) => {
          if (disabled) return
          setSearch(e.target.value)
          setOpen(true)
          // Clear selection if user edits text
          if (value) onChange('')
        }}
        onFocus={() => { if (!disabled) { setSearch(''); setOpen(true) } }}
        onKeyDown={handleKeyDown}
        placeholder={placeholder}
        required={required && !value}
        disabled={disabled}
      />
      {open && filtered.length > 0 && (
        <ul
          ref={listRef}
          className="absolute z-50 mt-1 max-h-52 w-full overflow-y-auto rounded-md border bg-popover p-1 shadow-md"
        >
          {filtered.map((opt, i) => (
            <li
              key={opt.value}
              className={`flex cursor-pointer items-center gap-2 rounded-sm px-2 py-1.5 text-sm ${
                i === highlightIndex ? 'bg-accent text-accent-foreground' : ''
              }`}
              onMouseEnter={() => setHighlightIndex(i)}
              onMouseDown={(e) => {
                e.preventDefault()
                select(opt)
              }}
            >
              <Check className={`h-3.5 w-3.5 ${value === opt.value ? 'opacity-100' : 'opacity-0'}`} />
              {opt.label}
            </li>
          ))}
        </ul>
      )}
      {open && filtered.length === 0 && search && (
        <div className="absolute z-50 mt-1 w-full rounded-md border bg-popover p-3 text-center text-sm text-muted-foreground shadow-md">
          Sin resultados
        </div>
      )}
    </div>
  )
}
