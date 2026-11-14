import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { firstValueFrom, timeout } from 'rxjs';
import { RouterLink } from '@angular/router';
import { bundledExperience, isPublishedExperience } from '../../../../../shared/experience/experience-document';
import { ExperienceView } from '../../../../../shared/experience/experience-view';

const storageKey = 'findyourfood.customer-home.v1';

@Component({
  selector: 'app-customer-home',
  imports: [ExperienceView, RouterLink],
  template: `
    <nav aria-label="Customer tools">
      <a routerLink="/restaurants">Find food</a>
      <a routerLink="/cart">Your cart</a>
      <a routerLink="/orders">Your orders</a>
      <a routerLink="/delivery-area">Check delivery area</a>
      <a routerLink="/account">Account settings</a>
    </nav>
    <main><fyf-experience-view [configuration]="configuration()" /></main>
  `,
  styles: `
    main { min-height: 100dvh; display: grid; }
    nav { display: flex; flex-wrap: wrap; gap: 12px; padding: 12px 18px; background: #fff9ee; }
    nav a { color: #365431; }
  `,
})
export class CustomerHome implements OnInit {
  private readonly http = inject(HttpClient);
  readonly configuration = signal(this.readLastPublication() ?? bundledExperience);

  ngOnInit() {
    void this.refresh();
  }

  private async refresh() {
    try {
      const result = await firstValueFrom(
        this.http.get<unknown>('/api/v1/experience/customer-home').pipe(timeout(1800)),
      );
      if (result === null) {
        this.configuration.set(bundledExperience);
        this.persist(null);
      } else if (isPublishedExperience(result)) {
        this.configuration.set(result.configuration);
        this.persist(result);
      }
    } catch {
      // Keep the last validated publication or the bundled homepage.
    }
  }

  private readLastPublication() {
    try {
      const stored: unknown = JSON.parse(localStorage.getItem(storageKey) ?? 'null');
      return isPublishedExperience(stored) ? stored.configuration : null;
    } catch {
      return null;
    }
  }

  private persist(value: unknown) {
    try {
      if (value === null) {
        localStorage.removeItem(storageKey);
      } else {
        localStorage.setItem(storageKey, JSON.stringify(value));
      }
    } catch {
      // A storage policy or quota must not affect rendering.
    }
  }
}
