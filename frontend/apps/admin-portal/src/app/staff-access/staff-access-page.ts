import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ExperienceEditor } from '../experience/experience-editor';
import { Grant, levels, permissions, StaffAccessApi } from './staff-access-api';

const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const scope = /^\/(?:[a-z0-9-]{1,32}(?:\/[a-z0-9-]{1,32}){0,7})?$/;

@Component({
  selector: 'app-staff-access',
  imports: [ReactiveFormsModule, ExperienceEditor],
  templateUrl: './staff-access-page.html',
  styleUrl: './staff-access-page.css',
})
export class StaffAccessPage {
  private readonly api = inject(StaffAccessApi);
  private readonly builder = inject(NonNullableFormBuilder);
  protected readonly token = signal('');
  readonly section = signal<'staff' | 'experience'>('staff');
  readonly signedIn = computed(() => this.token().length > 0);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  readonly grants = signal<Grant[]>([]);
  readonly page = signal(0);
  readonly pendingRevoke = signal<string | null>(null);
  readonly permissions = permissions;
  readonly levels = levels;

  readonly credentials = this.builder.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    password: ['', [Validators.required, Validators.maxLength(128)]],
    code: ['', [Validators.required, Validators.pattern(/^[0-9]{6}$/)]],
  });

  readonly assignment = this.builder.group({
    subjectId: ['', [Validators.required, Validators.pattern(uuid)]],
    permission: ['provider.review', Validators.required],
    scope: ['/', [Validators.required, Validators.maxLength(160), Validators.pattern(scope)]],
    level: ['Employee', Validators.required],
    canDelegate: false,
    parentGrantId: ['', Validators.pattern(uuid)],
  });

  readonly permissionCheck = this.builder.group({
    permission: ['provider.review', Validators.required],
    scope: ['/', [Validators.required, Validators.maxLength(160), Validators.pattern(scope)]],
  });

  async signIn() {
    if (this.credentials.invalid) {
      this.error.set('Enter your email, password, and six-digit authenticator code.');
      return;
    }

    await this.run(async () => {
      const credentials = this.credentials.getRawValue();
      const { accessToken } = await firstValueFrom(this.api.login(credentials));
      this.token.set(accessToken);
      this.credentials.reset();
      await this.fetchPage(0);
    });
    this.credentials.controls.code.reset();
  }

  signOut() {
    this.token.set('');
    this.section.set('staff');
    this.grants.set([]);
    this.pendingRevoke.set(null);
    this.assignment.reset();
    this.permissionCheck.reset();
    this.notice.set('');
    this.error.set('');
  }

  expireSession() {
    this.signOut();
    this.error.set('Your session expired. Sign in again.');
  }

  async loadPage(page: number) {
    await this.run(() => this.fetchPage(page));
  }

  async grantAccess() {
    if (this.assignment.invalid) {
      this.error.set('Enter a valid staff ID, scope, and optional parent grant ID.');
      return;
    }

    await this.run(async () => {
      const input = this.assignment.getRawValue();
      const created = await firstValueFrom(
        this.api.grant(this.token(), { ...input, parentGrantId: input.parentGrantId || null }),
      );
      if (this.page() === 0) {
        this.grants.update((grants) => [created, ...grants].slice(0, 50));
      }
      this.notice.set('Permission granted.');
      this.assignment.reset();
    });
  }

  async revoke(id: string) {
    await this.run(async () => {
      await firstValueFrom(this.api.revoke(this.token(), id));
      this.grants.update((grants) =>
        grants.map((grant) =>
          grant.id === id ? { ...grant, revokedAt: new Date().toISOString() } : grant,
        ),
      );
      this.pendingRevoke.set(null);
      this.notice.set('Grant revoked. Access that depended on this grant is now ineffective.');
    });
  }

  async checkAccess() {
    if (this.permissionCheck.invalid) {
      this.error.set('Enter a valid scope to check access.');
      return;
    }

    await this.run(async () => {
      const { allowed } = await firstValueFrom(
        this.api.check(this.token(), this.permissionCheck.getRawValue()),
      );
      this.notice.set(allowed ? 'You have access in this scope.' : 'You do not have access in this scope.');
    });
  }

  private async fetchPage(page: number) {
    this.grants.set(await firstValueFrom(this.api.list(this.token(), page)));
    this.page.set(page);
    this.pendingRevoke.set(null);
  }

  private async run(action: () => Promise<void>) {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.notice.set('');
    try {
      await action();
    } catch (error) {
      const wasSignedIn = this.signedIn();
      const status = error instanceof HttpErrorResponse ? error.status : 0;
      if (status === 401) {
        this.signOut();
        this.error.set(wasSignedIn ? 'Your session expired. Sign in again.' : 'Sign-in failed. Check your credentials and code.');
      } else if (status === 403) {
        this.error.set('Your account lacks authority for this change.');
      } else if (status === 409) {
        this.error.set('An unrevoked grant already exists for this staff member, permission, and scope.');
      } else if (status === 400) {
        this.error.set('Check the permission, scope, and staff level.');
      } else if (status === 429) {
        this.error.set('Too many attempts. Wait a minute and try again.');
      } else {
        this.error.set('The access service is unavailable. Try again shortly.');
      }
    } finally {
      this.busy.set(false);
    }
  }
}
