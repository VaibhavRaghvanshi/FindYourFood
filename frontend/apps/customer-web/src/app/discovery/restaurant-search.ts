import { HttpClient } from '@angular/common/http';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

interface SearchHit {
  branchId: string; providerName: string; branchName: string; kind: string; city: string; address: string;
  menuName: string; foods: string[]; tags: string[]; openNow: boolean; distanceKm: number | null;
  serviceable: boolean | null; deliveryFeeMinor: number | null;
}
interface SearchResults { items: SearchHit[]; page: number; total: number; indexedAt: string }

@Component({
  imports: [FormsModule, RouterLink, CurrencyPipe, DecimalPipe],
  templateUrl: './restaurant-search.html',
  styleUrl: './restaurant-search.css',
})
export class RestaurantSearch {
  private readonly http = inject(HttpClient);
  readonly results = signal<SearchResults | null>(null);
  readonly busy = signal(false);
  readonly message = signal('');
  city = '';
  query = '';
  tag = '';
  openNow = false;
  latitude: number | null = null;
  longitude: number | null = null;

  async search(page = 1) {
    if (this.busy() || !this.city.trim()) return;
    this.busy.set(true);
    this.message.set('');
    try {
      this.results.set(await firstValueFrom(this.http.post<SearchResults>('/api/v1/discovery/search', {
        city: this.city.trim(), query: this.query.trim(), tag: this.tag.trim(), openNow: this.openNow, page,
        latitude: this.latitude, longitude: this.longitude,
      })));
    } catch {
      this.results.set(null);
      this.message.set('Restaurant search is unavailable. Check your location and try again.');
    } finally { this.busy.set(false); }
  }

  useLocation() {
    if (this.busy()) return;
    if (!navigator.geolocation) { this.message.set('Enter coordinates to sort by delivery coverage.'); return; }
    this.busy.set(true);
    navigator.geolocation.getCurrentPosition((position) => {
      this.latitude = position.coords.latitude;
      this.longitude = position.coords.longitude;
      this.busy.set(false);
      if (this.city.trim()) void this.search();
    }, () => {
      this.message.set('Your location could not be read. You can enter it below or search by city.');
      this.busy.set(false);
    }, { timeout: 10_000, maximumAge: 60_000 });
  }

  clearLocation() { this.latitude = null; this.longitude = null; }
}
