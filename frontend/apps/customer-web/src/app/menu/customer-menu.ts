import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { CatalogApi, PublishedMenu } from '../../../../../shared/catalog/catalog-api';
import { CustomerAccountApi } from '../account/customer-account-api';
import { CustomerSession } from '../identity/customer-session';

@Component({
  selector: 'app-customer-menu',
  imports: [CurrencyPipe, RouterLink],
  templateUrl: './customer-menu.html',
  styleUrl: './customer-menu.css',
})
export class CustomerMenu implements OnInit {
  private readonly catalog = inject(CatalogApi);
  private readonly route = inject(ActivatedRoute);
  private readonly account = inject(CustomerAccountApi);
  readonly session = inject(CustomerSession);
  readonly publication = signal<PublishedMenu | null>(null);
  readonly loading = signal(true);
  readonly savingFavorite = signal(false);
  readonly favoriteMessage = signal('');
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

  async saveFavorite(branch: string) {
    if (this.savingFavorite()) return;
    this.savingFavorite.set(true);
    this.favoriteMessage.set('');
    try {
      const profile = await firstValueFrom(this.account.get());
      if (!profile.settings.favoriteBranches.includes(branch)) {
        profile.settings.favoriteBranches.push(branch);
        await firstValueFrom(this.account.save(profile));
      }
      this.favoriteMessage.set('Saved to your favorites.');
    } catch (error) {
      this.favoriteMessage.set(error instanceof HttpErrorResponse && error.status === 409
        ? 'Your settings changed. Try again.' : 'We couldn’t save this favorite.');
    } finally {
      this.savingFavorite.set(false);
    }
  }
}
