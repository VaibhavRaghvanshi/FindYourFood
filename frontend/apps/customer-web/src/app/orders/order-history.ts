import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { CustomerSession } from '../identity/customer-session';
import { OrdersApi, OrderSummary, orderStatusLabels } from './orders-api';

@Component({
  imports: [CurrencyPipe, DatePipe, RouterLink],
  templateUrl: './order-history.html',
  styleUrl: '../cart/cart-page.css',
})
export class OrderHistory implements OnInit {
  private readonly api = inject(OrdersApi);
  readonly customer = inject(CustomerSession);
  readonly items = signal<OrderSummary[]>([]);
  readonly busy = signal(false);
  readonly message = signal('');
  readonly page = signal(1);
  readonly labels = orderStatusLabels;
  readonly guestOrders = this.api.guestOrders();
  ngOnInit() { void this.load(1); }

  async load(page: number) {
    if (this.busy()) return;
    this.busy.set(true);
    this.message.set('');
    try {
      this.items.set(await firstValueFrom(this.api.history(page)));
      this.page.set(page);
    } catch (error) {
      this.message.set(error instanceof HttpErrorResponse && error.status === 401
        ? 'Sign in again to see your order history.' : 'Order history is unavailable.');
    } finally { this.busy.set(false); }
  }
}
