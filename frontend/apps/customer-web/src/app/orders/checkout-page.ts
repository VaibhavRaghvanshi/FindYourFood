import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { CustomerAccountApi, DeliveryAddress } from '../account/customer-account-api';
import { CartSession } from '../cart/cart-session';
import { CustomerSession } from '../identity/customer-session';
import { CheckoutRequest, OrdersApi } from './orders-api';

@Component({
  imports: [CurrencyPipe, FormsModule, RouterLink],
  templateUrl: './checkout-page.html',
  styleUrl: '../cart/cart-page.css',
})
export class CheckoutPage implements OnInit {
  readonly cart = inject(CartSession);
  private readonly orders = inject(OrdersApi);
  private readonly account = inject(CustomerAccountApi);
  private readonly customer = inject(CustomerSession);
  private readonly router = inject(Router);
  readonly addresses = signal<DeliveryAddress[]>([]);
  readonly busy = signal(false);
  readonly message = signal('');
  readonly existingOrder = signal<string | null>(null);
  private pending: { fingerprint: string; key: string } | null = null;
  name = '';
  phone = '';
  street = '';
  city = '';
  postalCode = '';
  latitude: number | null = null;
  longitude: number | null = null;

  async ngOnInit() {
    const quote = this.cart.quote();
    this.latitude = quote?.destination.latitude ?? null;
    this.longitude = quote?.destination.longitude ?? null;
    if (!this.cart.view()) await this.cart.load();
    if (!this.customer.authorization()) return;
    try {
      const profile = await firstValueFrom(this.account.get());
      this.name = profile.settings.displayName;
      this.addresses.set(profile.settings.addresses);
    } catch { /* Manual delivery details remain available. */ }
  }

  selectAddress(address: DeliveryAddress) {
    this.street = address.street;
    this.city = address.city;
    this.postalCode = address.postalCode;
    this.latitude = address.latitude;
    this.longitude = address.longitude;
    this.cart.invalidateQuote();
  }

  locationChanged(key: 'latitude' | 'longitude', value: number | null) {
    this[key] = value;
    this.cart.invalidateQuote();
  }

  async place() {
    const quote = this.cart.quote();
    if (!quote || this.busy() || this.cart.busy()) return;
    this.busy.set(true);
    this.message.set('');
    this.existingOrder.set(null);
    const body = {
      cartId: quote.cart.id, expectedCartRevision: quote.cart.revision, expectedTotalMinor: quote.totalMinor,
      delivery: {
        name: this.name.trim(), phone: this.phone.trim(), street: this.street.trim(), city: this.city.trim(),
        postalCode: this.postalCode.trim(), latitude: quote.destination.latitude, longitude: quote.destination.longitude,
      },
    };
    const fingerprint = JSON.stringify(body);
    if (this.pending?.fingerprint !== fingerprint) this.pending = { fingerprint, key: crypto.randomUUID() };
    const input: CheckoutRequest = { ...body, idempotencyKey: this.pending.key };
    try {
      const order = await firstValueFrom(this.orders.create(input));
      await this.cart.discard();
      await this.router.navigate(['/orders', order.id]);
    } catch (error) {
      const status = error instanceof HttpErrorResponse ? error.status : 0;
      const existing = error instanceof HttpErrorResponse ? error.error?.existingOrderId : null;
      this.existingOrder.set(typeof existing === 'string' ? existing : null);
      this.message.set(status === 409 ? 'Review your latest total or open the order already created for this cart.'
        : status === 400 ? 'Check your delivery details and phone number.'
        : status === 404 ? 'Your cart expired or your session changed. Return to your cart.'
        : 'Checkout is unavailable. You can safely retry this checkout.');
      if (status === 409 && !existing) await this.cart.deliveryQuote(this.latitude, this.longitude);
    } finally { this.busy.set(false); }
  }
}
