import { CHART_COLORS, CHART_INK } from '../../charts/apex-chart-theme';

/**
 * Terminal good/bad outcomes (Converted/Rejected/Expired) wear the reserved status
 * tokens, per the dataviz skill's collision rule; in-progress workflow stages use
 * categorical/neutral tones instead, since they aren't a good/bad outcome. Color is
 * never the only signal — the template's `@switch` also renders a distinct icon
 * per status (with a visible text label), for colorblind/low-vision accessibility.
 */
export const STATUS_BADGE_COLOR: Record<string, string> = {
  Draft: CHART_INK.muted,
  Submitted: CHART_COLORS.trend,
  Negotiation: CHART_COLORS.statusWarning,
  PendingApproval: '#4a3aa7',
  Approved: '#1baf7a',
  Converted: CHART_COLORS.statusGood,
  Rejected: CHART_COLORS.statusCritical,
  Expired: CHART_COLORS.statusSerious,
};

export const DEFAULT_STATUS_BADGE_COLOR = CHART_INK.muted;

/** "PendingApproval" -> "Pending Approval" — the API sends PascalCase status codes, never a display label. */
export function formatStatusLabel(status: string): string {
  return status.replace(/([a-z])([A-Z])/g, '$1 $2');
}
