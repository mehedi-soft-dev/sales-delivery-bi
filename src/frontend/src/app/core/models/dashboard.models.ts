import type { components } from './api-schema';

export interface DashboardResponse<T> {
  data: T | null;
  lastRefresh: string;
}

/** Mirrors backend Application/Common/PagedResult.cs — one server-side-paged grid slice. */
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export type QuotationDetailDto = components['schemas']['QuotationDetailDto'];
export type QuotationSummaryDto = components['schemas']['QuotationSummaryDto'];
export type UnitOptionDto = components['schemas']['UnitOptionDto'];

export type OpenQuotationDto = components['schemas']['OpenQuotationDto'];
export type AgedQuotationDto = components['schemas']['AgedQuotationDto'];
export type BuyerPerformanceDto = components['schemas']['BuyerPerformanceDto'];
export type AgingBucketDto = components['schemas']['AgingBucketDto'];
export type RiskLevelBucketDto = components['schemas']['RiskLevelBucketDto'];
export type StatusFunnelEntryDto = components['schemas']['StatusFunnelEntryDto'];
export type MonthlyTrendEntryDto = components['schemas']['MonthlyTrendEntryDto'];

/** Hand-typed like the *ResponseDto interfaces below — LostReasonBreakdownDto is new, not yet in api-schema.d.ts. */
export interface LostReasonBreakdownDto {
  reason: string;
  count: number | string;
  valueUsd: number | string;
}

export type PipelineKpisDto = components['schemas']['PipelineKpisDto'];
export type ConversionKpisDto = components['schemas']['ConversionKpisDto'];
export type AgingKpisDto = components['schemas']['AgingKpisDto'];

/**
 * Hand-typed to mirror the backend's *ResponseDto records (Application/Dtos/*.cs) — the row-level
 * property is now a PagedResult<T>, not a plain array. Not yet reflected in api-schema.d.ts (that
 * file is generated from a running backend via `npm run generate:api-types`); regenerate it once
 * the backend is up and replace these with `components['schemas'][...]` like the rest of this file.
 */
export interface QuotationPipelineResponseDto {
  kpis: PipelineKpisDto;
  statusFunnel: StatusFunnelEntryDto[];
  openQuotations: PagedResult<OpenQuotationDto>;
}

export interface ConversionResponseDto {
  kpis: ConversionKpisDto;
  monthlyTrend: MonthlyTrendEntryDto[];
  previousMonthlyTrend: MonthlyTrendEntryDto[];
  buyerPerformance: PagedResult<BuyerPerformanceDto>;
  lostReasons: LostReasonBreakdownDto[];
}

export interface AgingResponseDto {
  kpis: AgingKpisDto;
  agingBuckets: AgingBucketDto[];
  riskLevels: RiskLevelBucketDto[];
  agedQuotations: PagedResult<AgedQuotationDto>;
}

/**
 * Sales Order module — hand-typed like the *ResponseDto interfaces above, not yet in api-schema.d.ts
 * (that file is generated from a running backend via `npm run generate:api-types`); regenerate it once
 * the backend is up and replace these with `components['schemas'][...]` like the rest of this file.
 */
export interface SalesOrderKpisDto {
  openBacklogValueUsd: number | string;
  orderCount: number | string;
  avgOrderToPromisedDeliveryDays: number | string;
}

export interface SalesOrderStatusBucketDto {
  status: string;
  count: number | string;
  valueUsd: number | string;
}

export interface SalesOrderRowDto {
  soId: string;
  soNo: string;
  soDate: string;
  quotationId: string | null;
  buyerName: string;
  merchandiserName: string;
  unitName: string;
  orderValueUsd: number | string;
  deliveredValueUsd: number | string;
  pendingValueUsd: number | string;
  status: string;
  promisedDeliveryDate: string;
}

export interface SalesOrderResponseDto {
  kpis: SalesOrderKpisDto;
  statusBreakdown: SalesOrderStatusBucketDto[];
  orders: PagedResult<SalesOrderRowDto>;
}

/** Delivery module — hand-typed, not yet in api-schema.d.ts (same reason as Sales Order above). */
export interface DeliveryKpisDto {
  onTimeRatePct: number | string;
  delayedShipmentsCount: number | string;
  deliveredValueUsd: number | string;
}

export interface DeliveryStatusBucketDto {
  deliveryStatus: string;
  count: number | string;
  valueUsd: number | string;
}

export interface DeliveryRowDto {
  deliveryId: string;
  challanNo: string;
  deliveryDate: string;
  salesOrderId: string;
  soNo: string;
  buyerName: string;
  unitName: string;
  deliveredValueUsd: number | string;
  promisedDate: string;
  delayDays: number | string;
  deliveryStatus: string;
}

export interface DeliveryResponseDto {
  kpis: DeliveryKpisDto;
  statusBreakdown: DeliveryStatusBucketDto[];
  deliveries: PagedResult<DeliveryRowDto>;
}

/** Sales Invoice module — hand-typed, not yet in api-schema.d.ts. */
export interface InvoiceKpisDto {
  totalOutstandingUsd: number | string;
  overdueValueUsd: number | string;
  avgDaysSalesOutstanding: number | string;
}

export interface InvoiceAgingBucketDto {
  bucket: string;
  count: number | string;
  valueUsd: number | string;
}

export interface InvoiceRowDto {
  invoiceId: string;
  invoiceNo: string;
  invoiceDate: string;
  buyerName: string;
  unitName: string;
  invoiceValueUsd: number | string;
  paidAmountUsd: number | string;
  outstandingUsd: number | string;
  dueDate: string;
  daysOverdue: number | string;
  arStatus: string;
}

export interface InvoiceResponseDto {
  kpis: InvoiceKpisDto;
  agingBuckets: InvoiceAgingBucketDto[];
  invoices: PagedResult<InvoiceRowDto>;
}

/** Return/Credit Note module — hand-typed, not yet in api-schema.d.ts. */
export interface ReturnKpisDto {
  returnRatePct: number | string;
  returnValueUsd: number | string;
}

export interface ReturnReasonBreakdownDto {
  reasonCode: string;
  count: number | string;
  valueUsd: number | string;
}

export interface ReturnRowDto {
  returnId: string;
  returnNo: string;
  returnDate: string;
  buyerName: string;
  unitName: string;
  returnValueUsd: number | string;
  returnQty: number | string;
  reasonCode: string;
}

export interface ReturnResponseDto {
  kpis: ReturnKpisDto;
  reasonBreakdown: ReturnReasonBreakdownDto[];
  returns: PagedResult<ReturnRowDto>;
}

/** Admin > Users/Roles/Permissions (view-only) — mirrors Application/Dtos/AdminDto.cs. */
export interface AdminUserDto {
  userId: string;
  email: string;
  displayName: string;
  roleName: string;
  isActive: boolean;
  unitNames: string[];
}

export interface AdminRoleDto {
  roleId: string;
  roleName: string;
  userCount: number;
  permissionCodes: string[];
}

export interface AdminPermissionDto {
  permissionCode: string;
  roleNames: string[];
}
