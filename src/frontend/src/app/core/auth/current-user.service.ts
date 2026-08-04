import { Injectable, computed, signal } from '@angular/core';
import { JwtClaims, decodeJwtClaims, isJwtExpired } from './jwt';

const AUTH_TOKEN_STORAGE_KEY = 'sdbi_auth_token';

@Injectable({ providedIn: 'root' })
export class CurrentUserService {
  private readonly claims = signal<JwtClaims | null>(this.readStoredClaims());

  readonly sub = computed(() => this.claims()?.sub ?? null);
  readonly name = computed(() => this.claims()?.name ?? null);
  readonly role = computed(() => this.claims()?.role ?? null);
  readonly permissions = computed(() => this.claims()?.permissions ?? []);
  readonly userUnits = computed(() => this.claims()?.user_units ?? []);

  readonly isAuthenticated = computed(() => {
    const claims = this.claims();
    return claims !== null && !isJwtExpired(claims);
  });

  token(): string | null {
    return localStorage.getItem(AUTH_TOKEN_STORAGE_KEY);
  }

  hasPermission(code: string): boolean {
    return this.permissions().includes(code);
  }

  setToken(token: string): void {
    localStorage.setItem(AUTH_TOKEN_STORAGE_KEY, token);
    this.claims.set(decodeJwtClaims(token));
  }

  clearToken(): void {
    localStorage.removeItem(AUTH_TOKEN_STORAGE_KEY);
    this.claims.set(null);
  }

  private readStoredClaims(): JwtClaims | null {
    const token = localStorage.getItem(AUTH_TOKEN_STORAGE_KEY);
    return token ? decodeJwtClaims(token) : null;
  }
}
