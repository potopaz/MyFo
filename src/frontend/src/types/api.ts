export interface AuthResponse {
  token: string
  userId: string
  email: string
  fullName: string
  isSuperAdmin: boolean
  families: UserFamily[]
}

export interface CheckEmailResponse {
  exists: boolean
}

export interface SelectFamilyResponse {
  token: string
  familyId: string
  familyName: string
  role: string
}

export interface UserFamily {
  familyId: string
  familyName: string
  role: string
}

export interface InvitationInfoDto {
  familyName: string
  invitedByDisplayName: string
  expiresAt: string
  isValid: boolean
  errorCode?: string | null
}

export interface CreateInvitationResponse {
  token: string
  expiresAt: string
}

export interface CurrencyDto {
  currencyId: string
  code: string
  name: string
  symbol: string
  decimalPlaces: number
}

export interface FamilyCurrencyDto {
  familyCurrencyId: string
  currencyId: string
  code: string
  name: string
  symbol: string
  decimalPlaces: number
  isActive: boolean
}

export interface CategoryDto {
  categoryId: string
  name: string
  icon: string | null
  subcategories: SubcategoryDto[]
}

export interface SubcategoryDto {
  subcategoryId: string
  name: string
  subcategoryType: string
  isActive: boolean
  suggestedAccountingType: string | null
  suggestedCostCenterId: string | null
  isOrdinary: boolean | null
}

export interface CostCenterDto {
  costCenterId: string
  name: string
  isActive: boolean
}

export interface CashBoxDto {
  cashBoxId: string
  name: string
  currencyCode: string
  initialBalance: number
  balance: number
  isActive: boolean
  canOperate: boolean
}

export interface CashBoxMemberPermissionDto {
  memberId: string
  displayName: string
  permission: 'Operate' | null
}

export interface BankAccountDto {
  bankAccountId: string
  name: string
  currencyCode: string
  initialBalance: number
  balance: number
  accountNumber: string | null
  cbu: string | null
  alias: string | null
  isActive: boolean
}

export interface CreditCardDto {
  creditCardId: string
  name: string
  currencyCode: string
  isActive: boolean
  members: CreditCardMemberDto[]
}

export interface CreditCardMemberDto {
  creditCardMemberId: string
  holderName: string
  lastFourDigits: string | null
  isPrimary: boolean
  isActive: boolean
  expirationMonth: number | null
  expirationYear: number | null
  memberId: string | null
  isCurrentUser: boolean
}

export interface FamilySettingsDto {
  name: string
  primaryCurrencyCode: string
  secondaryCurrencyCode: string
  language: string
  canChangeCurrencies: boolean
}

export interface MovementListItemDto {
  movementId: string
  date: string
  movementType: string
  amount: number
  currencyCode: string
  amountInPrimary: number
  description: string | null
  subcategoryName: string
  categoryName: string
  accountingType: string | null
  isOrdinary: boolean | null
  costCenterName: string | null
  hasAssignedInstallments: boolean
}

export interface MovementPaymentDto {
  movementPaymentId: string
  paymentMethodType: string
  amount: number
  cashBoxId: string | null
  bankAccountId: string | null
  creditCardId: string | null
  creditCardMemberId: string | null
  installments: number | null
  bonificationType: string | null
  bonificationValue: number | null
  bonificationAmount: number | null
  netAmount: number | null
  hasAssignedInstallments: boolean
}

// Statement Periods (Credit Card Settlements)
export interface StatementPeriodDto {
  statementPeriodId: string
  creditCardId: string
  creditCardName: string
  periodStart: string
  periodEnd: string
  dueDate: string
  paymentStatus: string
  previousBalance: number
  installmentsTotal: number
  chargesTotal: number
  bonificationsTotal: number
  statementTotal: number
  paymentsTotal: number
  pendingBalance: number
  closedAt: string | null
}

export interface StatementPeriodDetailDto extends StatementPeriodDto {
  installments: StatementInstallmentDto[]
  lineItems: StatementLineItemDto[]
}

export interface StatementInstallmentDto {
  creditCardInstallmentId: string
  movementPaymentId: string
  installmentNumber: number
  projectedAmount: number
  bonificationApplied: number
  effectiveAmount: number
  actualAmount: number | null
  estimatedDate: string
  movementDescription: string | null
  movementDate: string | null
  totalInstallments: number | null
  isIncluded: boolean
  actualBonificationAmount: number | null
  isBonificationIncluded: boolean
  creditCardMemberName: string | null
}

export interface StatementLineItemDto {
  statementLineItemId: string
  lineType: string
  description: string
  amount: number
}

export interface CreditCardPaymentDto {
  creditCardPaymentId: string
  creditCardId: string
  creditCardName: string
  paymentDate: string
  amount: number
  description: string | null
  cashBoxId: string | null
  cashBoxName: string | null
  bankAccountId: string | null
  bankAccountName: string | null
  isTotalPayment: boolean
  statementPeriodId: string | null
  isPeriodClosed: boolean
  primaryExchangeRate: number
  secondaryExchangeRate: number
  amountInPrimary: number
  amountInSecondary: number
}

export interface MovementDto {
  movementId: string
  date: string
  movementType: string
  amount: number
  currencyCode: string
  primaryExchangeRate: number
  secondaryExchangeRate: number
  amountInPrimary: number
  amountInSecondary: number
  description: string | null
  subcategoryId: string
  accountingType: string | null
  isOrdinary: boolean | null
  costCenterId: string | null
  rowVersion: number
  payments: MovementPaymentDto[]
  createdAt: string
  createdByName: string | null
  modifiedAt: string | null
  modifiedByName: string | null
}

export interface FrequentMovementListItemDto {
  frequentMovementId: string
  name: string
  movementType: string
  amount: number
  currencyCode: string
  description: string | null
  subcategoryName: string
  categoryName: string
  paymentMethodType: string
  paymentEntityName: string | null
  frequencyMonths: number | null
  lastAppliedAt: string | null
  nextDueDate: string | null
  isActive: boolean
}

export interface FrequentMovementDto {
  frequentMovementId: string
  name: string
  movementType: string
  amount: number
  currencyCode: string
  description: string | null
  subcategoryId: string
  accountingType: string | null
  isOrdinary: boolean | null
  costCenterId: string | null
  paymentMethodType: string
  cashBoxId: string | null
  bankAccountId: string | null
  creditCardId: string | null
  creditCardMemberId: string | null
  frequencyMonths: number | null
  lastAppliedAt: string | null
  nextDueDate: string | null
  isActive: boolean
  rowVersion: number
  createdAt: string
  createdByName: string | null
  modifiedAt: string | null
  modifiedByName: string | null
}

export type TransferStatus = 'Confirmed' | 'PendingConfirmation' | 'Rejected'

export interface TransferListItemDto {
  transferId: string
  date: string
  fromCashBoxId: string | null
  fromCashBoxName: string | null
  fromBankAccountId: string | null
  fromBankAccountName: string | null
  toCashBoxId: string | null
  toCashBoxName: string | null
  toBankAccountId: string | null
  toBankAccountName: string | null
  fromCurrencyCode: string
  toCurrencyCode: string
  amount: number
  exchangeRate: number
  fromPrimaryExchangeRate: number
  fromSecondaryExchangeRate: number
  toPrimaryExchangeRate: number
  toSecondaryExchangeRate: number
  amountTo: number
  amountToInPrimary: number
  amountToInSecondary: number
  amountInPrimary: number
  amountInSecondary: number
  description: string | null
  rowVersion: number
  createdAt: string
  createdByName: string | null
  modifiedAt: string | null
  modifiedByName: string | null
  status: TransferStatus
  isAutoConfirmed: boolean
  rejectionComment: string | null
  creatorUserId: string | null
}

export interface TransferDto extends TransferListItemDto {}

export interface MonthlyFlowDto {
  year: number
  month: number
  income: number
  expense: number
  result: number
}

export interface MonthlyPatrimonyDto {
  year: number
  month: number
  balance: number
}

export interface DashboardSummaryDto {
  patrimony: number
  patrimonyChange: number
  monthIncome: number
  monthExpense: number
  monthResult: number
  monthIncomeChangePct: number | null
  monthExpenseChangePct: number | null
  monthlyFlow: MonthlyFlowDto[]
  patrimonyEvolution: MonthlyPatrimonyDto[]
}

export interface DimensionItemDto {
  name: string
  income: number
  expense: number
}

export interface AccountBalanceDto {
  accountType: 'CashBox' | 'BankAccount'
  name: string
  balance: number
  currencyCode: string
  balanceConverted: number | null
}

export interface CurrencyGroupDto {
  currencyCode: string
  totalNative: number
  totalConverted: number | null
  accounts: AccountBalanceDto[]
}

export interface DisponibilidadesDto {
  requestedCurrency: string
  totalConverted: number
  byCurrency: CurrencyGroupDto[]
}

export interface AdminFamilyMemberDto {
  memberId: string
  displayName: string
  role: string
  isActive: boolean
}

export interface AdminFamilyListItemDto {
  familyId: string
  name: string
  memberCount: number
  isEnabled: boolean
  maxMembers: number | null
  notes: string | null
  disabledAt: string | null
  disabledReason: string | null
  subcategoryCount: number
  costCenterCount: number
  movementCount: number
  transferCount: number
}

export interface AdminFamilyDetailDto extends AdminFamilyListItemDto {
  primaryCurrencyCode: string
  secondaryCurrencyCode: string
  language: string
  createdAt: string
  members: AdminFamilyMemberDto[]
}

export interface PeriodAnalysisDto {
  income: number
  expense: number
  result: number
  byCategory: DimensionItemDto[]
  bySubcategory: DimensionItemDto[]
  byCostCenter: DimensionItemDto[]
  byCharacter: DimensionItemDto[]
  byAccountingType: DimensionItemDto[]
}

// ── Report DTOs ──────────────────────────────────────────────────────────────

export interface NameAmountDto {
  name: string
  id?: string
  amount: number
}

export interface TimePointDto {
  label: string
  amount: number
}

export interface TimeSeriesMultiDto {
  label: string
  values: Record<string, number>
}

export interface OrdVsExtraDto {
  ordinary: number
  extraordinary: number
  unspecified: number
}

export interface CategoryExpenseDto {
  categoryName: string
  categoryId?: string
  amount: number
  subcategories: NameAmountDto[]
}

export interface IncomeExpenseReportDto {
  granularity: string
  totalExpense: number
  totalIncome: number
  expenseBySubcategory: NameAmountDto[]
  expenseByCategory: CategoryExpenseDto[]
  ordVsExtra: OrdVsExtraDto
  categoryEvolution: TimeSeriesMultiDto[]
  incomeBySource: NameAmountDto[]
  incomeEvolution: TimePointDto[]
  expenseByCostCenter: NameAmountDto[]
}

export interface CashFlowPointDto {
  label: string
  income: number
  expense: number
  net: number
}

export interface FutureInstallmentDto {
  label: string
  month: string
  amount: number
  cardName: string
}

export interface CashFlowReportDto {
  granularity: string
  cashFlow: CashFlowPointDto[]
  futureInstallments: FutureInstallmentDto[]
  paymentMethods: NameAmountDto[]
  paymentMethodEvolution: TimeSeriesMultiDto[]
}

export interface MonthlyDebtEvolutionDto {
  label: string
  newDebt: number
  paid: number
  net: number
}

export interface CardInstallmentsSummaryDto {
  cardId: string
  cardName: string
  totalDebt: number
  totalPaid: number
  pendingInstallments: number
}

export interface ChargesVsBonificationsDto {
  totalCharges: number
  totalBonifications: number
  net: number
}

export interface CardsCCReportDto {
  totalDebt: number
  totalPaid: number
  installmentsByCard: CardInstallmentsSummaryDto[]
  futureInstallments: FutureInstallmentDto[]
  byCostCenter: NameAmountDto[]
  costCenterEvolution: TimeSeriesMultiDto[]
  chargesVsBonifications: ChargesVsBonificationsDto
  granularity: string
  monthlyDebtEvolution: MonthlyDebtEvolutionDto[]
}

export interface AccountBalanceItemDto {
  name: string
  accountType: string
  currencyCode: string
  balance: number
  balanceInReportCurrency: number
}

export interface PatrimonyReportDto {
  totalAssets: number
  totalLiabilities: number
  netPatrimony: number
  periodIncome: number
  periodExpense: number
  periodSavings: number
  savingsRatio: number | null
  patrimonyEvolution: TimePointDto[]
  balanceByCurrency: NameAmountDto[]
  balanceByAccountType: NameAmountDto[]
  topAccounts: AccountBalanceItemDto[]
}

export interface DrilldownMovementDto {
  movementId: string
  date: string
  description: string | null
  subcategoryName: string
  categoryName: string
  costCenterName: string | null
  isOrdinary: boolean | null
  amount: number
  currencyCode: string
  movementType: string
}

export interface DrilldownResultDto {
  totalCount: number
  totalAmount: number
  netAmount: number
  items: DrilldownMovementDto[]
}
