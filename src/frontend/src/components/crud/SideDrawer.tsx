import type { FormEvent, ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
  SheetFooter,
  SheetClose,
} from '@/components/ui/sheet'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Checkbox } from '@/components/ui/checkbox'
import { Switch } from '@/components/ui/switch'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import type { FieldDef } from './types'
import { ComboboxField } from './ComboboxField'
import { AmountInput } from '@/components/ui/amount-input'

interface SideDrawerProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  fields: FieldDef<Record<string, unknown>>[]
  values: Record<string, unknown>
  onChange: (key: string, value: unknown) => void
  onSubmit: () => void
  loading?: boolean
  children?: ReactNode
}

export function SideDrawer({
  open,
  onOpenChange,
  title,
  fields,
  values,
  onChange,
  onSubmit,
  loading = false,
  children,
}: SideDrawerProps) {
  const { t } = useTranslation()

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault()
    onSubmit()
  }

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right" className="flex flex-col sm:max-w-md">
        <SheetHeader>
          <SheetTitle>{title}</SheetTitle>
          <SheetDescription className="sr-only">{title}</SheetDescription>
        </SheetHeader>

        <form onSubmit={handleSubmit} className="flex flex-1 flex-col gap-5 overflow-y-auto px-4">
          {fields.map((field) => (
            <div key={field.key} className={field.type === 'checkbox' || field.type === 'switch' ? 'flex items-center gap-3' : 'space-y-1.5'}>
              {field.type === 'checkbox' ? (
                <>
                  <Checkbox
                    id={field.key}
                    checked={Boolean(values[field.key])}
                    onCheckedChange={(checked) => onChange(field.key, checked)}
                  />
                  <Label htmlFor={field.key} className="text-sm font-normal">
                    {field.label}
                  </Label>
                </>
              ) : field.type === 'switch' ? (
                <>
                  <Switch
                    id={field.key}
                    checked={Boolean(values[field.key])}
                    onCheckedChange={(checked) => onChange(field.key, checked)}
                  />
                  <Label htmlFor={field.key} className="text-sm font-normal">
                    {field.label}
                  </Label>
                </>
              ) : (
                <>
                  <Label htmlFor={field.key}>{field.label}</Label>
                  {field.type === 'combobox' ? (
                    <ComboboxField
                      id={field.key}
                      value={String(values[field.key] ?? '')}
                      onChange={(val) => onChange(field.key, val)}
                      options={field.options}
                      loadOptions={field.loadOptions}
                      placeholder={field.placeholder}
                      required={field.required}
                    />
                  ) : field.type === 'amount' ? (
                    <AmountInput
                      id={field.key}
                      value={(() => {
                        const v = values[field.key]
                        if (v === '' || v === undefined || v === null) return ''
                        if (typeof v === 'number') return v.toFixed(field.decimalPlaces ?? 2)
                        return String(v)
                      })()}
                      onChange={(val) => onChange(field.key, val)}
                      maxDecimals={field.decimalPlaces ?? 2}
                      allowNegative
                      placeholder={field.placeholder}
                    />
                  ) : field.type === 'select' ? (
                    <Select
                      value={String(values[field.key] ?? '')}
                      onValueChange={(val) => onChange(field.key, val)}
                    >
                      <SelectTrigger className="w-full">
                        <SelectValue placeholder={field.placeholder} />
                      </SelectTrigger>
                      <SelectContent>
                        {field.options?.map((opt) => (
                          <SelectItem key={opt.value} value={opt.value}>
                            {opt.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  ) : (
                    <Input
                      id={field.key}
                      type={field.type === 'number' ? 'text' : 'text'}
                      inputMode={field.type === 'number' ? 'decimal' : field.numericOnly ? 'numeric' : undefined}
                      value={String(values[field.key] ?? '')}
                      onChange={(e) => {
                        if (field.type === 'number') {
                          const raw = e.target.value
                          const dp = field.decimalPlaces
                          const pattern = dp !== undefined && dp > 0
                            ? new RegExp(`^-?\\d*\\.?\\d{0,${dp}}$`)
                            : dp === 0
                              ? /^-?\d*$/
                              : /^-?\d*\.?\d*$/
                          if (raw === '' || raw === '-' || pattern.test(raw)) {
                            onChange(field.key, raw === '' ? '' : raw)
                          }
                        } else if (field.numericOnly) {
                          const raw = e.target.value
                          if (raw === '' || /^\d*$/.test(raw)) {
                            onChange(field.key, raw)
                          }
                        } else {
                          onChange(field.key, e.target.value)
                        }
                      }}
                      onBlur={() => {
                        if (field.type === 'number') {
                          const n = Number(values[field.key])
                          onChange(field.key, isNaN(n) ? 0 : n)
                        }
                      }}
                      placeholder={field.placeholder}
                      required={field.required}
                      maxLength={field.maxLength}
                    />
                  )}
                </>
              )}
            </div>
          ))}
          {children}
        </form>

        <SheetFooter className="flex-row gap-2 border-t pt-4">
          <SheetClose render={<Button variant="outline" className="flex-1" />}>
            {t('common.cancel')}
          </SheetClose>
          <Button className="flex-1" disabled={loading} onClick={onSubmit}>
            {loading ? t('common.saving') : t('common.save')}
          </Button>
        </SheetFooter>
      </SheetContent>
    </Sheet>
  )
}
