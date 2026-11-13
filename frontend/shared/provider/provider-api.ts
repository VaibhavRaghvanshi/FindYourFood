import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

export const providerKinds = [
  ['restaurant', 'Restaurant'], ['dhaba', 'Dhaba'], ['cloud-kitchen', 'Cloud kitchen'],
  ['cafe', 'Cafe'], ['bakery', 'Bakery'], ['home-kitchen', 'Home kitchen'], ['tiffin', 'Tiffin service'],
] as const;
export const documentKinds = [
  ['license', 'Business license'], ['address-proof', 'Address proof'],
] as const;

export interface ProviderDetails {
  name: string;
  kind: string;
  contactEmail: string;
  description: string;
}
export interface OpeningWindow { day: number; opens: string; closes: string }
export interface BranchDetails {
  name: string;
  address: string;
  city: string;
  timeZone: string;
  hours: OpeningWindow[];
  closedUntil: string | null;
  latitude?: number | null;
  longitude?: number | null;
}
export interface ProviderSummary { id: string; name: string; kind: string; updatedAt: string }
export interface ProviderView {
  id: string;
  revision: number;
  status: 'draft' | 'submitted' | 'approved' | 'rejected';
  details: ProviderDetails;
  reviewNote: string | null;
  documents: { kind: string; mediaType: string; size: number; uploadedAt: string }[];
  branches: { id: string; details: BranchDetails }[];
  reviews: { id: string; approved: boolean; note: string | null; reviewedAt: string }[];
}

@Injectable({ providedIn: 'root' })
export class ProviderApi {
  private readonly http = inject(HttpClient);
  private readonly root = '/api/v1/providers';
  private readonly own = this.root + '/me';
  private readonly staff = this.root + '/admin';

  account(email: string, password: string, register: boolean) {
    return this.http.post<{ accessToken: string }>(
      '/api/v1/identity/' + (register ? 'register' : 'login'), { email, password },
    );
  }

  profile(token: string) {
    return this.http.get<ProviderView>(this.own, this.auth(token));
  }

  saveApplication(token: string, details: ProviderDetails, expectedRevision: number | null) {
    const options = this.auth(token);
    return expectedRevision === null
      ? this.http.post<ProviderView>(this.own, details, options)
      : this.http.put<ProviderView>(this.own, { details, expectedRevision }, options);
  }

  upload(token: string, kind: string, document: File, revision: number) {
    const data = new FormData();
    data.append('document', document);
    data.append('expectedRevision', revision.toString());
    return this.http.put<ProviderView>(this.own + '/documents/' + kind, data, this.auth(token));
  }

  submit(token: string, expectedRevision: number) {
    return this.http.post<ProviderView>(this.own + '/submit', { expectedRevision }, this.auth(token));
  }

  saveBranch(token: string, branch: BranchDetails, expectedRevision: number, branchId: string | null) {
    const body = { expectedRevision, branch };
    return branchId === null
      ? this.http.post<ProviderView>(this.own + '/branches', body, this.auth(token))
      : this.http.put<ProviderView>(this.own + '/branches/' + branchId, body, this.auth(token));
  }

  pending(token: string) {
    return this.http.get<ProviderSummary[]>(this.staff, this.auth(token));
  }

  application(token: string, id: string) {
    return this.http.get<ProviderView>(this.staff + '/' + id, this.auth(token));
  }

  review(token: string, id: string, expectedRevision: number, approved: boolean, note: string | null) {
    return this.http.post<ProviderView>(
      this.staff + '/' + id + '/review', { expectedRevision, approved, note }, this.auth(token),
    );
  }

  download(token: string, kind: string, providerId?: string) {
    const path = providerId ? this.staff + '/' + providerId : this.own;
    return this.http.get(path + '/documents/' + kind, { ...this.auth(token), responseType: 'blob' });
  }

  private auth(token: string) {
    return { headers: { Authorization: `Bearer ${token}` } };
  }
}

export function saveProviderDocument(blob: Blob, kind: string) {
  const extension = blob.type === 'application/pdf' ? 'pdf' : blob.type === 'image/png' ? 'png' : 'jpg';
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = `${kind}.${extension}`;
  link.click();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
