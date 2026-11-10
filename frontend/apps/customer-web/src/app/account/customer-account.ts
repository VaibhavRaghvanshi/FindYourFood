import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { CustomerAccountApi, CustomerSnapshot, DeliveryAddress } from './customer-account-api';

@Component({
  imports: [FormsModule, RouterLink],
  templateUrl: './customer-account.html',
  styleUrl: './customer-account.css',
})
export class CustomerAccount implements OnInit {
  private readonly api = inject(CustomerAccountApi);
  readonly draft = signal<CustomerSnapshot | null>(null);
  readonly busy = signal(false);
  readonly signInRequired = signal(false);
  readonly message = signal('');
  dietary = '';

  ngOnInit() { void this.load(); }

  async load() {
    await this.run(async () => {
      const profile = await firstValueFrom(this.api.get());
      this.draft.set(profile);
      this.dietary = profile.settings.dietaryPreferences.join(', ');
    });
  }

  async save() {
    const draft = this.draft();
    if (!draft || this.busy()) return;
    await this.run(async () => {
      const settings = draft.settings;
      const addresses = settings.addresses.map((address) => ({
        ...address, label: address.label.trim(), street: address.street.trim(),
        city: address.city.trim(), postalCode: address.postalCode.trim(),
      }));
      this.draft.set(await firstValueFrom(this.api.save({
        revision: draft.revision,
        settings: {
          ...settings, displayName: settings.displayName.trim(), addresses,
          dietaryPreferences: [...new Set(this.dietary.split(',').map((entry) => entry.trim()).filter(Boolean))],
        },
      })));
      this.message.set('Your account settings were saved.');
    });
  }

  addAddress(profile: CustomerSnapshot) {
    const address: DeliveryAddress = {
      id: crypto.randomUUID(), label: '', street: '', city: '', postalCode: '', latitude: null, longitude: null,
    };
    profile.settings.addresses.push(address);
    profile.settings.primaryAddress ??= address.id;
  }

  removeAddress(profile: CustomerSnapshot, index: number) {
    const [removed] = profile.settings.addresses.splice(index, 1);
    if (removed.id === profile.settings.primaryAddress) {
      profile.settings.primaryAddress = profile.settings.addresses[0]?.id ?? null;
    }
  }

  private async run(action: () => Promise<void>) {
    if (this.busy()) return;
    this.busy.set(true);
    this.message.set('');
    this.signInRequired.set(false);
    try {
      await action();
    } catch (error) {
      const status = error instanceof HttpErrorResponse ? error.status : 0;
      this.signInRequired.set(status === 401);
      if (status === 401) this.draft.set(null);
      this.message.set(status === 409 ? 'Your settings changed. Reload to get the latest version.'
        : status === 400 ? 'Check address fields and use distinct dietary preferences.'
        : status === 401 ? 'Sign in to manage your settings.' : 'Account settings are unavailable.');
    } finally {
      this.busy.set(false);
    }
  }
}
