import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import {
  BranchDetails, documentKinds, ProviderApi, ProviderDetails, providerKinds, ProviderView, saveProviderDocument,
} from '../../../../../shared/provider/provider-api';

const emptyApplication: ProviderDetails = { name: '', kind: 'restaurant', contactEmail: '', description: '' };
const defaultBranch: BranchDetails = {
  name: '', address: '', city: '', timeZone: 'Asia/Kolkata', closedUntil: null,
  hours: [1, 2, 3, 4, 5].map((day) => ({ day, opens: '09:00', closes: '17:00' })),
};

@Component({
  selector: 'app-provider-page',
  imports: [FormsModule],
  templateUrl: './provider-page.html',
  styleUrl: './provider-page.css',
})
export class ProviderPage {
  private readonly api = inject(ProviderApi);
  private readonly token = signal('');
  readonly signedIn = computed(() => !!this.token());
  readonly register = signal(false);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  readonly loaded = signal(false);
  readonly profile = signal<ProviderView | null>(null);
  readonly details = signal<ProviderDetails>(emptyApplication);
  readonly branch = signal<BranchDetails>(defaultBranch);
  readonly editingBranch = signal<string | null>(null);
  readonly closure = signal('');
  readonly kinds = providerKinds;
  readonly documentKinds = documentKinds;
  readonly weekdays = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
  readonly canEdit = computed(() => !this.profile() || ['draft', 'rejected'].includes(this.profile()!.status));
  readonly currentStatusIsSubmitted = computed(() => this.profile()?.status === 'submitted');
  email = '';
  password = '';

  async signIn() {
    await this.run(async () => {
      if (this.register()) await firstValueFrom(this.api.account(this.email.trim(), this.password, true));
      const credentials = await firstValueFrom(this.api.account(this.email.trim(), this.password, false));
      this.token.set(credentials.accessToken);
      this.password = '';
      await this.loadProfile();
    });
  }

  signOut() {
    this.token.set('');
    this.profile.set(null);
    this.details.set(emptyApplication);
    this.loaded.set(false);
    this.editBranch(null);
    this.password = '';
    this.error.set('');
    this.notice.set('');
  }

  refresh() { return this.run(() => this.loadProfile()); }

  editDetails(key: keyof ProviderDetails, value: string) {
    this.details.update((details) => ({ ...details, [key]: value }));
  }

  saveApplication() {
    return this.run(async () => {
      this.accept(await firstValueFrom(
        this.api.saveApplication(this.token(), this.details(), this.profile()?.revision ?? null),
      ));
      this.notice.set('Application saved.');
    });
  }

  upload(kind: string, input: HTMLInputElement) {
    const file = input.files?.item(0);
    const profile = this.profile();
    input.value = '';
    if (!file || !profile) return;
    if (file.size > 2_000_000) {
      this.error.set('Choose a document up to 2 MB.');
      return;
    }

    return this.run(async () => {
      this.accept(await firstValueFrom(this.api.upload(this.token(), kind, file, profile.revision)));
      this.notice.set('Verification document uploaded.');
    });
  }

  download(kind: string) {
    return this.run(async () => saveProviderDocument(await firstValueFrom(this.api.download(this.token(), kind)), kind));
  }

  submit() {
    const profile = this.profile();
    if (!profile) return;
    return this.run(async () => {
      this.accept(await firstValueFrom(this.api.submit(this.token(), profile.revision)));
      this.notice.set('Application submitted for review.');
    });
  }

  editBranch(id: string | null) {
    const existing = this.profile()?.branches.find((item) => item.id === id);
    this.editingBranch.set(existing?.id ?? null);
    this.branch.set(existing?.details ?? defaultBranch);
    const until = existing?.details.closedUntil;
    if (until) {
      const date = new Date(until);
      date.setMinutes(date.getMinutes() - date.getTimezoneOffset());
      this.closure.set(date.toISOString().slice(0, 16));
    } else {
      this.closure.set('');
    }
  }

  editBranchField(key: 'name' | 'address' | 'city' | 'timeZone', value: string) {
    this.branch.update((branch) => ({ ...branch, [key]: value }));
  }

  hours(day: number) { return this.branch().hours.find((entry) => entry.day === day); }

  toggleDay(day: number, enabled: boolean) {
    this.branch.update((branch) => ({
      ...branch,
      hours: enabled ? [...branch.hours, { day, opens: '09:00', closes: '17:00' }]
        : branch.hours.filter((entry) => entry.day !== day),
    }));
  }

  editHours(day: number, key: 'opens' | 'closes', value: string) {
    this.branch.update((branch) => ({
      ...branch, hours: branch.hours.map((entry) => entry.day === day ? { ...entry, [key]: value } : entry),
    }));
  }

  saveBranch() {
    const profile = this.profile();
    if (!profile) return;
    return this.run(async () => {
      const closedUntil = this.closure() ? new Date(this.closure()).toISOString() : null;
      this.accept(await firstValueFrom(this.api.saveBranch(this.token(), { ...this.branch(), closedUntil },
        profile.revision, this.editingBranch())));
      this.editBranch(null);
      this.notice.set('Branch saved.');
    });
  }

  private async loadProfile() {
    try {
      this.accept(await firstValueFrom(this.api.profile(this.token())));
    } catch (error) {
      if (!(error instanceof HttpErrorResponse) || error.status !== 404) throw error;
      this.profile.set(null);
      this.details.set({ ...emptyApplication, contactEmail: this.email.trim() });
    }
    this.loaded.set(true);
  }

  private accept(profile: ProviderView) {
    this.profile.set(profile);
    this.details.set(profile.details);
  }

  private async run(action: () => Promise<void>) {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.notice.set('');
    try {
      await action();
    } catch (error) {
      const status = error instanceof HttpErrorResponse ? error.status : 0;
      const title = error instanceof HttpErrorResponse ? error.error?.title : undefined;
      if (status === 401) {
        const signedIn = this.signedIn();
        this.signOut();
        this.error.set(signedIn ? 'Your session expired. Sign in again.' : 'Sign-in failed. Check your credentials.');
      } else if (status === 409) {
        this.error.set(this.signedIn() ? 'Refresh the application before retrying.' : 'This account may already exist. Try signing in.');
      } else if (status === 400 || status === 413) {
        this.error.set(typeof title === 'string' ? title : 'Check the fields and file size, then try again.');
      } else {
        this.error.set('The provider service is unavailable. Try again shortly.');
      }
    } finally {
      this.busy.set(false);
    }
  }
}
