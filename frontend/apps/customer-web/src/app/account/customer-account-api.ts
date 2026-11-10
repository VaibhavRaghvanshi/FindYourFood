import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, Observable, throwError } from 'rxjs';
import { CustomerSession } from '../identity/customer-session';

export interface DeliveryAddress {
  id: string;
  label: string;
  street: string;
  city: string;
  postalCode: string;
  latitude: number | null;
  longitude: number | null;
}

export interface CustomerSettings {
  displayName: string;
  addresses: DeliveryAddress[];
  primaryAddress: string | null;
  dietaryPreferences: string[];
  favoriteBranches: string[];
  personalizedRecommendations: boolean;
}

export interface CustomerSnapshot {
  revision: number;
  settings: CustomerSettings;
}

@Injectable({ providedIn: 'root' })
export class CustomerAccountApi {
  private readonly http = inject(HttpClient);
  private readonly session = inject(CustomerSession);
  private readonly path = '/api/v1/customer/me';

  get() {
    return this.request((headers) => this.http.get<CustomerSnapshot>(this.path, { headers }));
  }

  save(snapshot: CustomerSnapshot) {
    return this.request((headers) => this.http.put<CustomerSnapshot>(this.path, snapshot, { headers }));
  }

  private request(send: (headers: { Authorization: string }) => Observable<CustomerSnapshot>): Observable<CustomerSnapshot> {
    const headers = this.session.authorization();
    if (!headers) return throwError(() => new HttpErrorResponse({ status: 401 }));
    return send(headers).pipe(catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401) this.session.clear();
      return throwError(() => error);
    }));
  }
}
