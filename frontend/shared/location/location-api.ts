import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

export interface Coordinates { latitude: number; longitude: number }
export interface ZoneSettings {
  name: string; city: string; center: Coordinates; radiusKm: number; deliveryFeeMinor: number; enabled: boolean;
}
export interface ZoneSnapshot { id: string; revision: number; settings: ZoneSettings }
export interface Serviceability {
  serviceable: boolean; zoneId: string | null; zone: string | null; city: string | null;
  deliveryFeeMinor: number | null; distanceKm: number | null; estimateMethod: string | null;
}

@Injectable({ providedIn: 'root' })
export class LocationApi {
  private readonly http = inject(HttpClient);
  check(point: Coordinates) { return this.http.post<Serviceability>('/api/v1/location/serviceability', point); }
  list(token: string, page: number) {
    return this.http.get<ZoneSnapshot[]>('/api/v1/location/zones', {
      headers: { Authorization: `Bearer ${token}` }, params: { page },
    });
  }
  save(token: string, zone: ZoneSnapshot) {
    return this.http.put<ZoneSnapshot>('/api/v1/location/zones/' + zone.id,
      { revision: zone.revision, settings: zone.settings }, { headers: { Authorization: `Bearer ${token}` } });
  }
}
