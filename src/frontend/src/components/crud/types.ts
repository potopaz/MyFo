import type { ReactNode } from 'react'

/** Column definition for DataTable */
export interface ColumnDef<T> {
  key: string
  header: string
  render: (item: T) => ReactNode
  className?: string
}

/** Field definition for the side drawer form */
export interface FieldDef<T> {
  key: keyof T & string
  label: string
  type: 'text' | 'select' | 'checkbox' | 'switch' | 'number' | 'amount' | 'combobox'
  placeholder?: string
  required?: boolean
  options?: { value: string; label: string }[]
  /** For combobox: async function to load options */
  loadOptions?: () => Promise<{ value: string; label: string }[]>
  maxLength?: number
  /** Only allow numeric characters (for text fields like card digits) */
  numericOnly?: boolean
  /** Max decimal places for number fields (e.g. 2 for currency amounts) */
  decimalPlaces?: number
  /** Field only shown when creating, hidden when editing */
  createOnly?: boolean
}

/** Props for the generic ConfigPage */
export interface ConfigPageProps<T> {
  title: string | ReactNode
  columns: ColumnDef<T>[]
  fields: FieldDef<Record<string, unknown>>[]
  /** Unique key to identify each row */
  rowKey: keyof T & string
  /** Fetch all items */
  fetchItems: () => Promise<T[]>
  /** Create a new item; return the created item or void */
  onCreate: (data: Record<string, unknown>) => Promise<unknown>
  /** Update an existing item */
  onUpdate: (id: string, data: Record<string, unknown>) => Promise<unknown>
  /** Delete an item */
  onDelete: (id: string) => Promise<unknown>
  /** Default values for the create form */
  defaultValues: Record<string, unknown>
  /** Label for the "new" button, e.g. "Nuevo centro" */
  newItemLabel: string
  /** Drawer title for create mode */
  createTitle: string
  /** Drawer title for edit mode */
  editTitle: string
  /** Map a DTO item to form values (for DTOs with different structure than form fields) */
  mapItemToForm?: (item: T) => Record<string, unknown>
  /** Extra action buttons per row, rendered before edit/delete */
  extraRowActions?: (item: T) => ReactNode
  /** Hide the edit (pencil) button in each row */
  hideEdit?: boolean
}
