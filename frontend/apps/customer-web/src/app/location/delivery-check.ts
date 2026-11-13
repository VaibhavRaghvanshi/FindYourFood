import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Coordinates, LocationApi, Serviceability } from '../../../../../shared/location/location-api';

@Component({
  imports: [CurrencyPipe, FormsModule, RouterLink],
  templateUrl: './delivery-check.html',
  styleUrl: '../account/customer-account.css',
})
export class DeliveryCheck {
  private readonly api = inject(LocationApi);
  readonly result = signal<Serviceability | null>(null);
  readonly busy = signal(false);
  readonly message = signal('');
  latitude: number | null = null;
  longitude: number | null = null;

  useCurrentLocation() {
    if (this.busy()) return;
    if (!navigator.geolocation) { this.message.set('Enter coordinates to check your area.'); return; }
    this.busy.set(true);
    navigator.geolocation.getCurrentPosition(
      (position) => {
        this.latitude = position.coords.latitude;
        this.longitude = position.coords.longitude;
        this.busy.set(false);
        void this.check();
      },
      () => { this.message.set('Location could not be read. Enter coordinates to continue.'); this.busy.set(false); },
      { timeout: 10_000, maximumAge: 60_000 },
    );
  }

  async check() {
    if (this.busy() || this.latitude === null || this.longitude === null) return;
    this.busy.set(true);
    this.message.set('');
    this.result.set(null);
    const point: Coordinates = { latitude: this.latitude, longitude: this.longitude };
    try { this.result.set(await firstValueFrom(this.api.check(point))); }
    catch { this.message.set('Delivery coverage is unavailable. Try again.'); }
    finally { this.busy.set(false); }
  }
}
