import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { switchMap } from 'rxjs';

export interface Credentials {
  email: string;
  password: string;
}

export interface CustomerProfile {
  id: string;
  email: string;
  emailVerified: boolean;
}

interface LoginResponse {
  accessToken: string;
  expiresIn: number;
  tokenType: 'Bearer';
}

@Injectable({ providedIn: 'root' })
export class IdentityApi {
  private readonly http = inject(HttpClient);
  private readonly root = '/api/v1/identity';

  register(credentials: Credentials) {
    return this.http.post<void>(`${this.root}/register`, credentials);
  }

  signIn(credentials: Credentials) {
    return this.http.post<LoginResponse>(`${this.root}/login`, credentials).pipe(
      switchMap(({ accessToken }) =>
        this.http.get<CustomerProfile>(`${this.root}/me`, {
          headers: { Authorization: `Bearer ${accessToken}` },
        }),
      ),
    );
  }
}
