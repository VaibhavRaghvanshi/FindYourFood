import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

export interface MenuOption { id: string; name: string; extraMinor: number }
export interface MenuItem {
  id: string; category: string; name: string; description: string; priceMinor: number; available: boolean;
  ingredients: string[]; tags: string[]; allergens: string[]; variants: MenuOption[]; addOns: MenuOption[];
}
export interface MenuContent { name: string; items: MenuItem[] }
export interface MenuWorkspace { revision: number; publishedVersion: number; hasUnpublishedChanges: boolean; draft: MenuContent | null }
export interface PublishedMenu { branchId: string; version: number; currency: string; menu: MenuContent }

@Injectable({ providedIn: 'root' })
export class CatalogApi {
  private readonly http = inject(HttpClient);

  menu(branch: string) { return this.http.get<PublishedMenu>(this.path(branch)); }

  workspace(branch: string, token: string) {
    return this.http.get<MenuWorkspace>(this.path(branch) + '/workspace', this.auth(token));
  }

  save(branch: string, token: string, expectedRevision: number, menu: MenuContent) {
    return this.http.put<MenuWorkspace>(this.path(branch) + '/draft', { expectedRevision, menu }, this.auth(token));
  }

  publish(branch: string, token: string, expectedRevision: number) {
    return this.http.post<MenuWorkspace>(this.path(branch) + '/publish', { expectedRevision }, this.auth(token));
  }

  private path(branch: string) { return '/api/v1/catalog/branches/' + encodeURIComponent(branch); }
  private auth(token: string) { return { headers: { Authorization: `Bearer ${token}` } }; }
}
