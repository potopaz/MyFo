import { forwardRef, type ChangeEvent, type FocusEvent, type InputHTMLAttributes } from 'react'
import { Input } from '@/components/ui/input'

interface AmountInputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'value' | 'onChange' | 'type'> {
  value: string
  onChange: (raw: string) => void
  /** Max integer digits (default 16) */
  maxIntegers?: number
  /** Max decimal digits (default 2) */
  maxDecimals?: number
  /** Allow negative values (default false) */
  allowNegative?: boolean
}

function formatAmountLive(raw: string): string {
  if (!raw || raw === '-') return raw
  const negative = raw.startsWith('-')
  const abs = negative ? raw.slice(1) : raw
  const parts = abs.split('.')
  const intPart = parts[0] || ''
  const formatted = intPart.replace(/\B(?=(\d{3})+(?!\d))/g, ',')
  let result = formatted
  if (parts.length > 1) result = formatted + '.' + parts[1]
  else if (abs.endsWith('.')) result = formatted + '.'
  return negative ? '-' + result : result
}

function padDecimals(raw: string, decimals: number): string {
  if (!raw || raw === '-') return raw
  const num = parseFloat(raw)
  if (isNaN(num)) return raw
  return num.toFixed(decimals)
}

export const AmountInput = forwardRef<HTMLInputElement, AmountInputProps>(
  ({ value, onChange, maxIntegers = 16, maxDecimals = 2, allowNegative = false, onBlur, ...rest }, ref) => {

    const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
      const raw = e.target.value.replace(/,/g, '')
      if (raw === '' || (allowNegative && raw === '-')) {
        onChange(raw)
        return
      }
      const negPrefix = allowNegative ? '-?' : ''
      const decimalPart = maxDecimals > 0 ? `(\\.\\d{0,${maxDecimals}})?` : ''
      const pattern = new RegExp(`^${negPrefix}\\d{0,${maxIntegers}}${decimalPart}$`)
      if (pattern.test(raw)) {
        onChange(raw)
      }
    }

    const handleBlur = (e: FocusEvent<HTMLInputElement>) => {
      if (value && value !== '-' && maxDecimals > 0) {
        onChange(padDecimals(value, maxDecimals))
      }
      onBlur?.(e)
    }

    return (
      <Input
        ref={ref}
        type="text"
        inputMode="decimal"
        value={formatAmountLive(value)}
        onChange={handleChange}
        onBlur={handleBlur}
        {...rest}
      />
    )
  }
)

AmountInput.displayName = 'AmountInput'
