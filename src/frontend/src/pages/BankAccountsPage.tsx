import { useCallback, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Badge } from '@/components/ui/badge'
import { ConfigPage } from '@/components/crud'
import type { ColumnDef, FieldDef } from '@/components/crud'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import api from '@/lib/api'
import { loadFamilyCurrencyOptions } from '@/lib/currency-options'
import { HelpCircle } from 'lucide-react'
import type { BankAccountDto } from '@/types/api'

export default function BankAccountsPage() {
  const { t } = useTranslation()
  const fetchItems = useCallback(() => api.get<BankAccountDto[]>('/bankaccounts').then((r) => r.data), [])

  const columns: ColumnDef<BankAccountDto>[] = useMemo(() => [
    { key: 'name', header: t('bankAccounts.columns.name'), render: (item) => <span className="font-medium">{item.name}</span> },
    { key: 'currencyCode', header: t('bankAccounts.columns.currency'), render: (item) => item.currencyCode },
    { key: 'initialBalance', header: t('bankAccounts.columns.initialBalance'), className: 'text-right', render: (item) => <span className="text-muted-foreground">{item.initialBalance.toLocaleString()}</span> },
    { key: 'balance', header: t('bankAccounts.columns.balance'), className: 'text-right', render: (item) => <span className="font-medium">{item.balance.toLocaleString()}</span> },
    { key: 'alias', header: t('bankAccounts.columns.alias'), render: (item) => <span className="text-muted-foreground">{item.alias || '-'}</span> },
    {
      key: 'isActive',
      header: t('bankAccounts.columns.status'),
      render: (item) => (
        <Badge variant={item.isActive ? 'default' : 'secondary'}>
          {item.isActive ? t('bankAccounts.active') : t('bankAccounts.inactive')}
        </Badge>
      ),
    },
  ], [t])

  const fields: FieldDef<Record<string, unknown>>[] = useMemo(() => [
    { key: 'name', label: t('bankAccounts.fields.name'), type: 'text', required: true, placeholder: t('bankAccounts.fields.namePlaceholder'), maxLength: 100 },
    { key: 'currencyCode', label: t('bankAccounts.fields.currency'), type: 'combobox', required: true, placeholder: t('bankAccounts.fields.searchCurrency'), loadOptions: loadFamilyCurrencyOptions },
    { key: 'initialBalance', label: t('bankAccounts.fields.initialBalance'), type: 'amount', decimalPlaces: 2 },
    { key: 'cbu', label: t('bankAccounts.fields.cbu'), type: 'text', placeholder: t('common.optional'), maxLength: 30 },
    { key: 'alias', label: t('bankAccounts.fields.alias'), type: 'text', placeholder: t('common.optional'), maxLength: 50 },
    { key: 'isActive', label: t('bankAccounts.fields.isActive'), type: 'switch' },
  ], [t])

  const title = (
    <div className="flex items-center">
      <span>{t('bankAccounts.title')}
        <Tooltip>
          <TooltipTrigger>
            <HelpCircle className="inline h-3.5 w-3.5 ml-1.5 align-super text-muted-foreground cursor-help hover:text-foreground transition-colors" />
          </TooltipTrigger>
          <TooltipContent>
            {t('bankAccounts.tooltip')}
          </TooltipContent>
        </Tooltip>
      </span>
    </div>
  )

  return (
    <ConfigPage<BankAccountDto>
      title={title}
      columns={columns}
      fields={fields}
      rowKey="bankAccountId"
      fetchItems={fetchItems}
      mapItemToForm={(item) => ({
        name: item.name,
        currencyCode: item.currencyCode,
        initialBalance: item.initialBalance,
        cbu: item.cbu ?? '',
        alias: item.alias ?? '',
        isActive: item.isActive,
      })}
      onCreate={(data) => api.post('/bankaccounts', {
        name: data.name,
        currencyCode: data.currencyCode,
        initialBalance: Number(data.initialBalance) || 0,
        cbu: data.cbu || null,
        alias: data.alias || null,
      })}
      onUpdate={(id, data) => api.put(`/bankaccounts/${id}`, {
        name: data.name,
        currencyCode: data.currencyCode,
        initialBalance: Number(data.initialBalance) || 0,
        cbu: data.cbu || null,
        alias: data.alias || null,
        isActive: data.isActive,
      })}
      onDelete={(id) => api.delete(`/bankaccounts/${id}`)}
      defaultValues={{ name: '', currencyCode: 'ARS', initialBalance: 0, cbu: '', alias: '', isActive: true }}
      newItemLabel={t('bankAccounts.new')}
      createTitle={t('bankAccounts.createTitle')}
      editTitle={t('bankAccounts.editTitle')}
    />
  )
}
