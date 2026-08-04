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
export type StatusFunnelEntryDto = components['schemas']['StatusFunnelEntryDto'];
export type MonthlyTrendEntryDto = components['schemas']['MonthlyTrendEntryDto'];

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
  buyerPerformance: PagedResult<BuyerPerformanceDto>;
}

export interface AgingResponseDto {
  kpis: AgingKpisDto;
  agingBuckets: AgingBucketDto[];
  agedQuotations: PagedResult<AgedQuotationDto>;
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
