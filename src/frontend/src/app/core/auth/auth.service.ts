import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CurrentUserService } from './current-user.service';

export interface LoginResponse {
  token: string;
  expiresAtUtc: string;
  displayName: string;
}

const LOGIN_ENDPOINT = `${environment.apiBaseUrl}/auth/login`;

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly currentUser = inject(CurrentUserService);

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(LOGIN_ENDPOINT, { email, password })
      .pipe(tap((response) => this.currentUser.setToken(response.token)));
  }
}
