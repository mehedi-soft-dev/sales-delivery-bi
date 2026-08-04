/** Mirrors the backend's PermissionCodes (SalesDeliveryBI.Infrastructure.Security) — used only for frontend UI gating, never trusted as the source of truth (the API re-checks on every request). */
export const PermissionCodes = {
  AdminView: 'admin.access.view',
  AdminManage: 'admin.access.manage',
} as const;
