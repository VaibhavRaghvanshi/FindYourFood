import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { of, tap } from 'rxjs';
import { CartSession } from '../cart/cart-session';
import { CustomerSession } from '../identity/customer-session';

export interface DeliveryContact {
  name: string; phone: string; street: string; city: string; postalCode: string; latitude: number; longitude: number;
}
export interface OrderItem {
  lineId: string; itemId: string; name: string; quantity: number; variantId: string | null; variantName: string | null;
  addOnIds: string[]; addOnNames: string[]; unitPriceMinor: number; totalMinor: number;
}
export interface OrderSnapshot {
  branchId: string; menuName: string; menuVersion: number; currency: string; items: OrderItem[];
  itemsMinor: number; deliveryFeeMinor: number; totalMinor: number; zoneId: string;
  delivery: DeliveryContact; priceCheckedAt: string;
}
export interface OrderView {
  id: string; status: string; revision: number; createdAt: string; updatedAt: string; guestOrder: boolean;
  snapshot: OrderSnapshot;
}
export interface OrderSummary { id: string; status: string; createdAt: string; menuName: string; totalMinor: number; currency: string }
export interface CheckoutRequest {
  idempotencyKey: string; cartId: string; expectedCartRevision: number; expectedTotalMinor: number; delivery: DeliveryContact;
}
interface GuestAccess { id: string; key: string }
const storageKey = 'findyourfood.order-access.v1';
const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export const orderStatusLabels: Record<string, string> = {
  AwaitingPayment: 'Awaiting payment', Placed: 'Order placed', Accepted: 'Restaurant accepted',
  Preparing: 'Preparing your food', ReadyForPickup: 'Ready for pickup', OutForDelivery: 'Out for delivery',
  Delivered: 'Delivered', Cancelled: 'Cancelled',
};

@Injectable({ providedIn: 'root' })
export class OrdersApi {
  private readonly http = inject(HttpClient);
  private readonly cart = inject(CartSession);
  private readonly customer = inject(CustomerSession);
  private guestAccess = this.readAccess();

  create(input: CheckoutRequest) {
    const headers = this.cart.headers();
    return this.http.post<OrderView>('/api/v1/orders', input, { headers }).pipe(
      tap((order) => this.remember(order, headers['X-Cart-Key'])),
    );
  }

  get(id: string) {
    const headers = this.headers(id);
    return this.http.get<OrderView>('/api/v1/orders/' + encodeURIComponent(id), { headers }).pipe(
      tap((order) => this.remember(order, headers['X-Cart-Key'])),
    );
  }

  cancel(id: string) {
    return this.http.post<OrderView>(`/api/v1/orders/${encodeURIComponent(id)}/cancel`, null, { headers: this.headers(id) });
  }

  history(page: number) {
    const headers = this.customer.authorization();
    return headers ? this.http.get<OrderSummary[]>('/api/v1/orders', { headers, params: { page } }) : of([]);
  }

  guestOrders() { return this.guestAccess.map((entry) => entry.id); }

  headers(id: string): Record<string, string> {
    const key = this.guestAccess.find((entry) => entry.id === id)?.key;
    return { ...this.cart.headers(), ...(key ? { 'X-Cart-Key': key } : {}) };
  }

  private remember(order: OrderView, key: string | undefined) {
    if (!order.guestOrder || !key) return;
    this.guestAccess = [{ id: order.id, key }, ...this.guestAccess.filter((entry) => entry.id !== order.id)].slice(0, 20);
    try { sessionStorage.setItem(storageKey, JSON.stringify(this.guestAccess)); } catch { /* Retain access in memory. */ }
  }

  private readAccess(): GuestAccess[] {
    try {
      const entries: unknown = JSON.parse(sessionStorage.getItem(storageKey) ?? '[]');
      return Array.isArray(entries) ? entries.filter((entry): entry is GuestAccess =>
        !!entry && typeof entry === 'object' && typeof entry.id === 'string' && uuid.test(entry.id) &&
        typeof entry.key === 'string' && /^[0-9a-f]{64}$/i.test(entry.key)).slice(0, 20) : [];
    } catch { return []; }
  }
}
