import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, input, OnInit, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { LocationApi, ZoneSnapshot } from '../../../../../shared/location/location-api';

@Component({
  selector: 'app-zone-editor',
  imports: [FormsModule],
  templateUrl: './zone-editor.html',
  styleUrl: './zone-editor.css',
})
export class ZoneEditor implements OnInit {
  private readonly api = inject(LocationApi);
  readonly token = input.required<string>();
  readonly sessionExpired = output<void>();
  readonly zones = signal<ZoneSnapshot[]>([]);
  readonly draft = signal<ZoneSnapshot | null>(null);
  readonly busy = signal(false);
  readonly message = signal('');
  readonly page = signal(1);
  feeRupees = 0;

  ngOnInit() { void this.load(1); }

  async load(page: number) {
    await this.run(async () => {
      this.zones.set(await firstValueFrom(this.api.list(this.token(), page)));
      this.page.set(page);
      this.draft.set(null);
    });
  }

  edit(zone?: ZoneSnapshot) {
    const draft = zone ? structuredClone(zone) : {
      id: crypto.randomUUID(), revision: 0,
      settings: { name: '', city: '', center: { latitude: 0, longitude: 0 }, radiusKm: 5, deliveryFeeMinor: 0, enabled: false },
    };
    this.draft.set(draft);
    this.feeRupees = draft.settings.deliveryFeeMinor / 100;
  }

  async save(zone: ZoneSnapshot) {
    await this.run(async () => {
      zone.settings.name = zone.settings.name.trim();
      zone.settings.city = zone.settings.city.trim();
      const saved = await firstValueFrom(this.api.save(this.token(), {
        ...zone, settings: { ...zone.settings, deliveryFeeMinor: Math.round(this.feeRupees * 100) },
      }));
      this.draft.set(saved);
      this.zones.update((zones) => [saved, ...zones.filter((row) => row.id !== saved.id)].slice(0, 25));
      this.message.set('Delivery area saved.');
    });
  }

  private async run(action: () => Promise<void>) {
    if (this.busy()) return;
    this.busy.set(true);
    this.message.set('');
    try { await action(); }
    catch (error) {
      const status = error instanceof HttpErrorResponse ? error.status : 0;
      if (status === 401) this.sessionExpired.emit();
      this.message.set(status === 403 ? 'Delivery areas require the owner account.'
        : status === 409 ? 'This area changed. Reload before editing.'
        : status === 400 ? 'Check coordinates, radius and delivery fee.' : 'Delivery areas are unavailable.');
    } finally { this.busy.set(false); }
  }
}
