import { PermissionCodes } from '../core/auth/permission-codes';

export type NavIconName =
  | 'home'
  | 'folder'
  | 'chart-line'
  | 'percentage'
  | 'clock'
  | 'shopping-cart'
  | 'truck'
  | 'receipt'
  | 'reply'
  | 'chart-pie'
  | 'shield'
  | 'users'
  | 'sitemap'
  | 'key';

interface NavNodeBase {
  readonly label: string;
  readonly iconName: NavIconName;
}

export interface NavLeaf extends NavNodeBase {
  readonly kind: 'leaf';
  readonly path: string;
  /** Routes to the generic "coming soon" page rather than a real feature — nav-only for now, per the user. */
  readonly placeholder?: boolean;
}

export interface NavGroup extends NavNodeBase {
  readonly kind: 'group';
  readonly children: readonly NavNode[];
  /** Hides the whole group unless CurrentUserService.hasPermission(permission) — checked in the sidebar. */
  readonly permission?: string;
}

export type NavNode = NavLeaf | NavGroup;

export const NAV_TREE: readonly NavNode[] = [
  { kind: 'leaf', label: 'Executive', iconName: 'chart-pie', path: '/overview' },
  {
    kind: 'group',
    label: 'Dashboard',
    iconName: 'home',
    children: [
      {
        kind: 'group',
        label: 'Quotation',
        iconName: 'folder',
        children: [
          { kind: 'leaf', label: 'Pipeline', iconName: 'chart-line', path: '/pipeline' },
          { kind: 'leaf', label: 'Conversion & Win/Loss', iconName: 'percentage', path: '/conversion' },
          { kind: 'leaf', label: 'Aging', iconName: 'clock', path: '/aging' },
        ],
      },
      { kind: 'leaf', label: 'Sales Orders', iconName: 'shopping-cart', path: '/dashboard/sales-orders' },
    ],
  },
  {
    kind: 'group',
    label: 'Admin',
    iconName: 'shield',
    permission: PermissionCodes.AdminView,
    children: [
      { kind: 'leaf', label: 'Users', iconName: 'users', path: '/admin/users' },
      { kind: 'leaf', label: 'Roles', iconName: 'sitemap', path: '/admin/roles' },
      { kind: 'leaf', label: 'Permissions', iconName: 'key', path: '/admin/permissions' },
    ],
  },
];

export interface NavBreadcrumb {
  readonly leaf: NavLeaf;
  /** Ancestor groups leading to the leaf, root first — excludes the leaf itself. */
  readonly ancestors: readonly NavGroup[];
}

/** Depth-first search for the leaf whose `path` the given URL starts with (longest match wins). */
export function findActiveBreadcrumb(url: string, nodes: readonly NavNode[] = NAV_TREE): NavBreadcrumb | null {
  let best: NavBreadcrumb | null = null;

  for (const node of nodes) {
    if (node.kind === 'leaf') {
      if (url.startsWith(node.path) && (!best || node.path.length > best.leaf.path.length)) {
        best = { leaf: node, ancestors: [] };
      }
      continue;
    }

    const found = findActiveBreadcrumb(url, node.children);
    if (found && (!best || found.leaf.path.length > best.leaf.path.length)) {
      best = { leaf: found.leaf, ancestors: [node, ...found.ancestors] };
    }
  }

  return best;
}

/** True if `nodes` (or any descendant) contains a leaf whose path the URL starts with — used to auto-expand the active branch. */
export function containsActiveRoute(url: string, node: NavNode): boolean {
  if (node.kind === 'leaf') {
    return url.startsWith(node.path);
  }
  return node.children.some((child) => containsActiveRoute(url, child));
}
