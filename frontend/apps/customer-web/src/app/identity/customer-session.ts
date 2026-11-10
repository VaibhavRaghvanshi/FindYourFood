import { computed, Injectable, signal } from '@angular/core';
import type { CustomerProfile } from './identity-api';

@Injectable({ providedIn: 'root' })
export class CustomerSession {
  private readonly session = signal<{ profile: CustomerProfile; token: string; expiresAt: number } | null>(null);
  private expirationTimer: ReturnType<typeof setTimeout> | undefined;
  readonly profile = computed(() => this.session()?.profile ?? null);

  establish(profile: CustomerProfile, token: string, expiresIn: number) {
    this.clear();
    this.session.set({ profile, token, expiresAt: Date.now() + expiresIn * 1000 });
    this.expirationTimer = setTimeout(() => this.clear(), expiresIn * 1000);
  }

  authorization() {
    const session = this.session();
    if (!session || session.expiresAt <= Date.now()) {
      this.clear();
      return null;
    }
    return { Authorization: `Bearer ${session.token}` };
  }

  clear() {
    clearTimeout(this.expirationTimer);
    this.expirationTimer = undefined;
    this.session.set(null);
  }
}
