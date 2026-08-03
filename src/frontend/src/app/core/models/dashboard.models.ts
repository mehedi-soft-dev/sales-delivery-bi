import type { components } from './api-schema';

export interface DashboardResponse<T> {
  data: T | null;
  lastRefresh: string;
}

export type QuotationPipelineDto = components['schemas']['QuotationPipelineDto'];
export type ConversionDto = components['schemas']['ConversionDto'];
export type AgingDto = components['schemas']['AgingDto'];
export type QuotationDetailDto = components['schemas']['QuotationDetailDto'];
export type QuotationSummaryDto = components['schemas']['QuotationSummaryDto'];

export type OpenQuotationDto = components['schemas']['OpenQuotationDto'];
export type AgedQuotationDto = components['schemas']['AgedQuotationDto'];
export type BuyerPerformanceDto = components['schemas']['BuyerPerformanceDto'];
export type AgingBucketDto = components['schemas']['AgingBucketDto'];
export type StatusFunnelEntryDto = components['schemas']['StatusFunnelEntryDto'];
export type MonthlyTrendEntryDto = components['schemas']['MonthlyTrendEntryDto'];
