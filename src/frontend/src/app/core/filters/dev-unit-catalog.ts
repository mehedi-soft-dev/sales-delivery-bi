/**
 * Dev-only unit GUID → display name stand-in, matching `docs/plans/database/seed-data.md`.
 * There is no units-lookup endpoint anywhere in the API contract — the JWT only carries
 * unit GUIDs, never names — same category of gap as the backend's dev-only JWT signing
 * key (`Program.cs`) until the Identity service exists. Falls back to the raw GUID for
 * any unit not in this map, so an unrecognized unit never renders blank.
 */
export const DEV_UNIT_DISPLAY_NAMES: Record<string, string> = {
  '11111111-1111-1111-1111-111111111101': 'Unit-1 (Knit)',
  '11111111-1111-1111-1111-111111111102': 'Unit-2 (Woven)',
  '11111111-1111-1111-1111-111111111103': 'Unit-3 (Sweater)',
};

export function resolveUnitDisplayName(unitId: string): string {
  return DEV_UNIT_DISPLAY_NAMES[unitId] ?? unitId;
}
