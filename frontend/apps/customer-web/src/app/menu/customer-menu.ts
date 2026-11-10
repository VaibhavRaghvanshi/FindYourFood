import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { CatalogApi, PublishedMenu } from '../../../../../shared/catalog/catalog-api';

@Component({
  selector: 'app-customer-menu',
  imports: [CurrencyPipe, RouterLink],
  templateUrl: './customer-menu.html',
  styleUrl: './customer-menu.css',
})
export class CustomerMenu implements OnInit {
  private readonly catalog = inject(CatalogApi);
  private readonly route = inject(ActivatedRoute);
  readonly publication = signal<PublishedMenu | null>(null);
  readonly loading = signal(true);
  readonly categories = computed(() => [...new Set(this.publication()?.menu.items.map((item) => item.category) ?? [])]);

  async ngOnInit() {
    const branch = this.route.snapshot.paramMap.get('branch')!;
    try {
      this.publication.set(await firstValueFrom(this.catalog.menu(branch)));
    } catch {
      this.publication.set(null);
    } finally {
      this.loading.set(false);
    }
  }
}
