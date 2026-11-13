import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { CustomerSession } from '../identity/customer-session';

export interface CartSelection {
  lineId: string; itemId: string; quantity: number; variantId: string | null; addOnIds: string[];
}
export interface PricedLine {
  lineId: string; itemId: string; name: string; quantity: number; variant: string | null;
  addOns: string[]; unitPriceMinor: number; totalMinor: number; error: string | null;
}
export interface CartView {
  id: string; branchId: string; revision: number; expiresAt: string; currency: string; menuVersion: number;
  branchOpen: boolean; selections: CartSelection[]; prices: { lines: PricedLine[]; subtotalMinor: number | null };
}
export interface CartQuote {
  cart: CartView; destination: { latitude: number; longitude: number }; zoneId: string;
  deliveryFeeMinor: number; totalMinor: number; checkedAt: string;
}
interface CartMetadata { id: string; branchId: string; guestKey: string | null }

const storageKey = 'findyourfood.cart.v1';
const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

@Injectable({ providedIn: 'root' })
export class CartSession {
  private readonly http = inject(HttpClient);
  private readonly customer = inject(CustomerSession);
  private metadata = this.readMetadata();
  readonly view = signal<CartView | null>(null);
  readonly quote = signal<CartQuote | null>(null);
  readonly busy = signal(false);
  readonly message = signal('');

  async load() {
    await this.run(async () => {
      const cart = await this.current();
      this.view.set(cart);
      this.quote.set(null);
    });
  }

  async add(branchId: string, itemId: string, quantity: number, variantId: string | null, addOnIds: string[]) {
    if (!Number.isInteger(quantity) || quantity < 1 || quantity > 20) {
      this.message.set('Choose a whole quantity between 1 and 20.');
      return;
    }
    await this.run(async () => {
      let cart = await this.current();
      if (cart && cart.branchId !== branchId) {
        this.message.set('Your cart is from another restaurant. Open your cart to review it or start again.');
        return;
      }
      if (!cart) {
        const created = await firstValueFrom(this.http.post<{ cart: CartView; guestKey: string | null }>(
          '/api/v1/carts', { branchId }, { headers: this.headers() }));
        cart = created.cart;
        this.remember({ id: cart.id, branchId: cart.branchId, guestKey: created.guestKey });
      }
      const sorted = [...addOnIds].sort();
      const lines = structuredClone(cart.selections);
      const existing = lines.find((line) => line.itemId === itemId && line.variantId === variantId &&
        [...line.addOnIds].sort().join(',') === sorted.join(','));
      if (existing) existing.quantity += quantity;
      else lines.push({ lineId: crypto.randomUUID(), itemId, quantity, variantId, addOnIds: sorted });
      if (lines.length > 20 || lines.some((line) => line.quantity > 20)) {
        this.message.set('Your cart allows up to 20 different selections and 20 of each selection.');
        return;
      }
      await this.replace(cart, lines);
      this.message.set('Added to your cart.');
    });
  }

  async quantity(lineId: string, quantity: number | null) {
    const cart = this.view();
    if (!cart || quantity === null || !Number.isInteger(quantity) || quantity < 1 || quantity > 20) return;
    await this.run(() => this.replace(cart, cart.selections.map((line) =>
      line.lineId === lineId ? { ...line, quantity } : line)));
  }

  async remove(lineId: string) {
    const cart = this.view();
    if (!cart) return;
    await this.run(() => this.replace(cart, cart.selections.filter((line) => line.lineId !== lineId)));
  }

  async discard() {
    await this.run(async () => {
      if (this.metadata) {
        try {
          await firstValueFrom(this.http.delete('/api/v1/carts/' + this.metadata.id, { headers: this.headers() }));
        } catch (error) {
          if (!(error instanceof HttpErrorResponse) || error.status !== 404) throw error;
        }
      }
      this.remember(null);
      this.view.set(null);
      this.quote.set(null);
      this.message.set('Your cart is empty.');
    });
  }

  async deliveryQuote(latitude: number | null, longitude: number | null) {
    const cart = this.view();
    if (!cart || latitude === null || longitude === null) return;
    await this.run(async () => {
      const quote = await firstValueFrom(this.http.post<CartQuote>(`/api/v1/carts/${cart.id}/quote`, {
        expectedRevision: cart.revision, destination: { latitude, longitude },
      }, { headers: this.headers() }));
      this.view.set(quote.cart);
      this.quote.set(quote);
    });
  }

  invalidateQuote() { this.quote.set(null); }

  headers(): Record<string, string> {
    const authorization = this.customer.authorization();
    return { ...(authorization ?? {}), ...(this.metadata?.guestKey ? { 'X-Cart-Key': this.metadata.guestKey } : {}) };
  }

  private async current() {
    if (this.metadata) {
      const cart = await firstValueFrom(this.http.get<CartView>('/api/v1/carts/' + this.metadata.id, { headers: this.headers() }));
      this.view.set(cart);
      return cart;
    }
    const authorization = this.customer.authorization();
    if (!authorization) return null;
    try {
      const cart = await firstValueFrom(this.http.get<CartView>('/api/v1/carts/mine', { headers: authorization }));
      this.remember({ id: cart.id, branchId: cart.branchId, guestKey: null });
      this.view.set(cart);
      return cart;
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 404) return null;
      throw error;
    }
  }

  private async replace(cart: CartView, lines: CartSelection[]) {
    this.view.set(await firstValueFrom(this.http.put<CartView>('/api/v1/carts/' + cart.id,
      { expectedRevision: cart.revision, lines }, { headers: this.headers() })));
    this.quote.set(null);
  }

  private async run(action: () => Promise<void>) {
    if (this.busy()) return;
    this.busy.set(true);
    this.message.set('');
    try { await action(); }
    catch (error) {
      this.quote.set(null);
      const status = error instanceof HttpErrorResponse ? error.status : 0;
      this.message.set(status === 409 ? 'Your cart, menu or delivery availability changed. Reload and review your selections.'
        : status === 404 ? 'This cart expired or belongs to another session. Sign in or start a new cart.'
        : status === 401 ? 'Sign in again to open your saved cart.'
        : status === 400 ? 'Check your selections and delivery location.' : 'Your cart is unavailable. Try again.');
    } finally { this.busy.set(false); }
  }

  private readMetadata(): CartMetadata | null {
    try {
      const data = JSON.parse(sessionStorage.getItem(storageKey) ?? 'null') as CartMetadata | null;
      return data && uuid.test(data.id) && uuid.test(data.branchId) &&
        (data.guestKey === null || /^[0-9a-f]{64}$/i.test(data.guestKey)) ? data : null;
    } catch { return null; }
  }

  private remember(metadata: CartMetadata | null) {
    this.metadata = metadata;
    try {
      if (metadata) sessionStorage.setItem(storageKey, JSON.stringify(metadata));
      else sessionStorage.removeItem(storageKey);
    } catch { /* Keep the current cart in memory when browser storage is unavailable. */ }
  }
}
