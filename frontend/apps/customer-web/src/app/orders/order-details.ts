import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom, interval } from 'rxjs';
import { OrdersApi, OrderView, orderStatusLabels } from './orders-api';

@Component({
  imports: [CurrencyPipe, DatePipe, RouterLink],
  templateUrl: './order-details.html',
  styleUrl: '../cart/cart-page.css',
})
export class OrderDetails implements OnInit {
  private readonly api = inject(OrdersApi);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private id = '';
  private requestVersion = 0;
  readonly order = signal<OrderView | null>(null);
  readonly busy = signal(false);
  readonly message = signal('');
  readonly labels = orderStatusLabels;

  ngOnInit() {
    this.destroyRef.onDestroy(() => this.requestVersion++);
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((parameters) => {
      this.id = parameters.get('id') ?? '';
      this.order.set(null);
      void this.load(true);
    });
    interval(5000).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      const status = this.order()?.status;
      if (document.visibilityState === 'visible' && status !== 'Cancelled' && status !== 'Delivered') void this.load();
    });
  }

  async load(force = false) {
    if (this.busy() && !force) return;
    const version = ++this.requestVersion;
    this.busy.set(true);
    this.message.set('');
    try {
      const order = await firstValueFrom(this.api.get(this.id));
      if (version === this.requestVersion) this.order.set(order);
    } catch (error) {
      if (version === this.requestVersion) this.showError(error);
    } finally {
      if (version === this.requestVersion) this.busy.set(false);
    }
  }

  async cancel() {
    if (this.busy()) return;
    const version = ++this.requestVersion;
    this.busy.set(true);
    this.message.set('');
    try {
      const order = await firstValueFrom(this.api.cancel(this.id));
      if (version === this.requestVersion) this.order.set(order);
    } catch (error) {
      if (version === this.requestVersion) this.showError(error);
    } finally {
      if (version === this.requestVersion) this.busy.set(false);
    }
  }

  private showError(error: unknown) {
    const status = error instanceof HttpErrorResponse ? error.status : 0;
    if (status === 401 || status === 404) this.order.set(null);
    this.message.set(status === 404 || status === 401 ? 'Sign in or use the tab where you placed this order to view it.'
      : status === 409 ? 'The order changed. Reload to see its latest status.' : 'Your order is temporarily unavailable.');
  }
}
