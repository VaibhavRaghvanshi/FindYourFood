import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { CatalogApi, MenuItem, PublishedMenu } from '../../../../../shared/catalog/catalog-api';
import { CartSession } from '../cart/cart-session';
import { CustomerAccountApi } from '../account/customer-account-api';
import { CustomerSession } from '../identity/customer-session';

@Component({
  selector: 'app-customer-menu',
  imports: [CurrencyPipe, RouterLink, FormsModule],
  templateUrl: './customer-menu.html',
  styleUrl: './customer-menu.css',
})
export class CustomerMenu implements OnInit {
  private readonly catalog = inject(CatalogApi);
  private readonly route = inject(ActivatedRoute);
  private readonly account = inject(CustomerAccountApi);
  readonly session = inject(CustomerSession);
  readonly cart = inject(CartSession);
  readonly chosenVariants: Record<string, string> = {};
  readonly chosenAddOns: Record<string, string[]> = {};
  readonly quantities: Record<string, number> = {};
  readonly selectionMessage = signal('');
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

  chooseAddOn(item: string, option: string, checked: boolean) {
    const choices = this.chosenAddOns[item] ?? [];
    this.chosenAddOns[item] = checked ? [...choices, option] : choices.filter((id) => id !== option);
  }

  async addToCart(branch: string, item: MenuItem) {
    this.selectionMessage.set('');
    const variant = this.chosenVariants[item.id] || null;
    if (item.variants.length && !variant) {
      this.selectionMessage.set('Choose a variant before adding this item.');
      return;
    }
    await this.cart.add(branch, item.id, this.quantities[item.id] ?? 1, variant, this.chosenAddOns[item.id] ?? []);
  }
}
