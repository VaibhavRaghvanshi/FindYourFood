import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, input, OnInit, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom, interval } from 'rxjs';
import { PaymentsApi, PaymentView } from './payments-api';

@Component({
  selector: 'fyf-payment-panel',
  imports: [CurrencyPipe, DatePipe],
  template: `
    <section class="payment" aria-labelledby="payment-title">
      <h2 id="payment-title">Payment</h2>
      @if (payment(); as current) {
        <p role="status">{{ labels[current.status] ?? current.status }}</p>
        @if (current.status === 'Pending' || current.status === 'Failed') {
          <p>Development payment simulation. No money will be charged.</p>
          <p>{{ current.totalMinor / 100 | currency:current.currency }} · Pay before {{ current.deadline | date:'shortTime' }}</p>
          <button type="button" [disabled]="busy() || disabled()" (click)="pay(true)">Simulate successful payment</button>
          <button type="button" [disabled]="busy() || disabled()" (click)="pay(false)">Simulate declined payment</button>
        }
      } @else { <p role="status">{{ unavailable() ? 'Payment service is unavailable. Reload to try again.' : 'Preparing your payment…' }}</p> }
      @if (message()) { <p role="alert">{{ message() }}</p> }
      <button type="button" [disabled]="busy()" (click)="load()">Reload payment</button>
    </section>
  `,
  styles: [`
    .payment { padding: 1.5rem; border: 1px solid #b9c8b3; border-radius: 18px; background: #f4f7ef; margin: 1rem 0; }
    button { font: inherit; padding: .7rem 1rem; border: 1px solid #264b3c; border-radius: 12px; background: #264b3c; color: white; margin: .25rem; cursor: pointer; }
    button:disabled { opacity: .5; cursor: default; } p { overflow-wrap: anywhere; }
  `],
})
export class PaymentPanel implements OnInit {
  readonly orderId = input.required<string>();
  readonly disabled = input(false);
  readonly orderStatus = input('AwaitingPayment');
  readonly changed = output<void>();
  private readonly api = inject(PaymentsApi);
  private readonly destroyRef = inject(DestroyRef);
  private destroyed = false;
  private key: string | null = null;
  private outcome: boolean | null = null;
  private loading = false;
  private version = 0;
  readonly payment = signal<PaymentView | null>(null);
  readonly busy = signal(false);
  readonly unavailable = signal(false);
  readonly message = signal('');
  readonly labels: Record<string, string> = {
    Pending: 'Awaiting payment', Processing: 'Confirming payment…', Failed: 'Payment declined. You can try again.',
    Succeeded: 'Payment confirmed', Cancelled: 'Payment cancelled', RefundPending: 'Refund in progress', Refunded: 'Refund completed',
  };
  ngOnInit() {
    this.destroyRef.onDestroy(() => { this.destroyed = true; this.version++; });
    void this.load();
    interval(2000).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      const status = this.payment()?.status;
      if (document.visibilityState === 'visible' && status !== 'Refunded' && status !== 'Cancelled' &&
          this.orderStatus() !== 'Delivered') void this.load();
    });
  }
  async load() {
    if (this.loading || this.busy()) return;
    this.loading = true;
    const version = ++this.version;
    try {
      const value = await firstValueFrom(this.api.get(this.orderId()));
      if (this.destroyed || version !== this.version) return;
      if (value.status !== this.payment()?.status) this.changed.emit();
      this.payment.set(value);
      if (value.status !== 'Processing') { this.key = null; this.outcome = null; }
      this.unavailable.set(false);
    } catch (error) {
      if (!this.destroyed && version === this.version) this.unavailable.set(!(error instanceof HttpErrorResponse && error.status === 404));
    } finally { this.loading = false; }
  }
  async pay(success: boolean) {
    if (this.busy() || this.disabled() || !this.payment()) return;
    if (this.outcome !== null && this.outcome !== success && this.key) {
      this.message.set('Reload payment before choosing a different outcome for this attempt.'); return;
    }
    this.key ??= crypto.randomUUID();
    this.outcome = success;
    this.busy.set(true);
    const version = ++this.version;
    this.message.set('');
    try {
      const value = await firstValueFrom(this.api.simulate(this.orderId(), this.key, this.payment()!.totalMinor, success));
      if (this.destroyed || version !== this.version) return;
      this.payment.set(value);
      if (value.status !== 'Processing') { this.key = null; this.outcome = null; }
      this.changed.emit();
    } catch (error) {
      if (this.destroyed || version !== this.version) return;
      const status = error instanceof HttpErrorResponse ? error.status : 0;
      this.message.set(status === 409 ? 'This payment changed. Reload its latest status.'
        : 'Confirmation is pending. Retry the same payment attempt.');
    } finally { this.busy.set(false); }
  }
}
