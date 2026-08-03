export interface JwtClaims {
  sub: string;
  permissions?: string[];
  user_units?: string[];
  exp?: number;
}

export function decodeJwtClaims(token: string): JwtClaims | null {
  const parts = token.split('.');
  if (parts.length !== 3) {
    return null;
  }

  try {
    const base64 = parts[1]!.replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
    return JSON.parse(atob(padded)) as JwtClaims;
  } catch {
    return null;
  }
}

export function isJwtExpired(claims: JwtClaims): boolean {
  if (!claims.exp) {
    return false;
  }
  return claims.exp * 1000 <= Date.now();
}
