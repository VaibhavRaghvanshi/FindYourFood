import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { ExperienceDocument } from '../../../../../shared/experience/experience-document';

export interface PublicationSummary {
  version: number;
  publishedAt: string;
  publishedBy: string;
  approvedBy: string | null;
  restoredFromVersion: number | null;
}

export interface Workspace {
  revision: number;
  draft: ExperienceDocument | null;
  draftStatus: 'empty' | 'draft' | 'approved' | 'published';
  publishedVersion: number | null;
  history: PublicationSummary[];
}

@Injectable({ providedIn: 'root' })
export class ExperienceAdminApi {
  private readonly http = inject(HttpClient);
  private readonly root = '/api/v1/experience/admin/customer-home';

  workspace(token: string) {
    return this.http.get<Workspace>(this.root, this.options(token));
  }

  preview(token: string, configuration: ExperienceDocument) {
    return this.http.post<ExperienceDocument>(this.root + '/preview', { configuration }, this.options(token));
  }

  saveDraft(token: string, expectedRevision: number, configuration: ExperienceDocument) {
    return this.http.put<Workspace>(this.root + '/draft', { expectedRevision, configuration }, this.options(token));
  }

  transition(token: string, action: 'approve' | 'publish', expectedRevision: number) {
    return this.http.post<Workspace>(this.root + '/' + action, { expectedRevision }, this.options(token));
  }

  rollback(token: string, expectedRevision: number, version: number) {
    return this.http.post<Workspace>(this.root + '/rollback', { expectedRevision, version }, this.options(token));
  }

  private options(token: string) {
    return { headers: { Authorization: `Bearer ${token}` } };
  }
}
