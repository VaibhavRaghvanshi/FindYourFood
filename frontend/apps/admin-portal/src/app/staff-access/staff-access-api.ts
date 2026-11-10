import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

export const permissions = [
  'provider.review',
  'support.ticket.read',
  'support.ticket.assign',
  'finance.report.read',
  'experience.edit',
  'experience.publish',
] as const;

export const levels = ['Ceo', 'DepartmentHead', 'CityManager', 'TeamManager', 'Employee'] as const;

export interface NewGrant {
  subjectId: string;
  permission: string;
  scope: string;
  level: string;
  canDelegate: boolean;
  parentGrantId: string | null;
}

export interface Grant extends NewGrant {
  id: string;
  revokedAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class StaffAccessApi {
  private readonly http = inject(HttpClient);
  private readonly root = '/api/v1/organization';

  login(input: { email: string; password: string; code: string }) {
    return this.http.post<{ accessToken: string }>('/api/v1/identity/staff/login', input);
  }

  list(token: string, page: number) {
    return this.http.get<Grant[]>(this.root + '/grants', {
      headers: this.headers(token),
      params: { page },
    });
  }

  grant(token: string, input: NewGrant) {
    return this.http.post<Grant>(this.root + '/grants', input, { headers: this.headers(token) });
  }

  revoke(token: string, id: string) {
    return this.http.delete<void>(`${this.root}/grants/${id}`, { headers: this.headers(token) });
  }

  check(token: string, input: { permission: string; scope: string }) {
    return this.http.get<{ allowed: boolean }>(this.root + '/permissions/check', {
      headers: this.headers(token),
      params: input,
    });
  }

  private headers(token: string) {
    return { Authorization: `Bearer ${token}` };
  }
}
