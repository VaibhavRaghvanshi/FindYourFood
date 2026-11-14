import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { OrdersApi } from './orders-api';

export interface PaymentView {
  id: string; orderId: string; status: string; totalMinor: number; currency: string;
  paymentId: string | null; deadline: string;
}
@Injectable({ providedIn: 'root' })
export class PaymentsApi {
  private readonly http = inject(HttpClient);
  private readonly orders = inject(OrdersApi);
  get(order: string) {
    return this.http.get<PaymentView>(this.url(order), { headers: this.orders.headers(order) });
  }
  simulate(order: string, key: string, total: number, success: boolean) {
    return this.http.post<PaymentView>(this.url(order) + '/simulate',
      { idempotencyKey: key, expectedTotalMinor: total, success }, { headers: this.orders.headers(order) });
  }
  private url(order: string) { return '/api/v1/payments/orders/' + encodeURIComponent(order); }
}
