import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { CustomerProfile, IdentityApi } from './identity-api';

@Component({
  selector: 'app-identity-page',
  imports: [ReactiveFormsModule],
  templateUrl: './identity-page.html',
  styleUrl: './identity-page.css',
})
export class IdentityPage {
  private readonly identity = inject(IdentityApi);
  private readonly builder = inject(NonNullableFormBuilder);
  readonly mode = signal<'login' | 'register'>('login');
  readonly busy = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  readonly profile = signal<CustomerProfile | null>(null);

  readonly form = this.builder.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    password: ['', [Validators.required, Validators.maxLength(128)]],
  });

  chooseMode(mode: 'login' | 'register') {
    if (this.busy() || this.mode() === mode) return;
    this.setMode(mode);
  }

  async submit() {
    if (this.busy()) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const credentials = this.form.getRawValue();
    credentials.email = credentials.email.trim();
    const registering = this.mode() === 'register';
    this.busy.set(true);
    this.error.set('');
    this.notice.set('');

    try {
      if (registering) {
        await firstValueFrom(this.identity.register(credentials));
        this.setMode('login');
        this.notice.set('Account created. Sign in to continue.');
      } else {
        this.profile.set(await firstValueFrom(this.identity.signIn(credentials)));
        this.form.controls.password.reset();
      }
    } catch (error) {
      this.error.set(this.errorMessage(error, registering));
    } finally {
      this.busy.set(false);
    }
  }

  signOut() {
    this.profile.set(null);
    this.form.reset();
    this.notice.set('You’re signed out.');
  }

  private setMode(mode: 'login' | 'register') {
    this.mode.set(mode);
    this.error.set('');
    this.notice.set('');
    this.form.controls.password.setValidators([
      Validators.required,
      Validators.maxLength(128),
      ...(mode === 'register' ? [Validators.minLength(12)] : []),
    ]);
    this.form.controls.password.reset();
  }

  private errorMessage(error: unknown, registering: boolean) {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 429) return 'Too many attempts. Wait a minute and try again.';
      if (error.status === 401) return 'Sign-in failed. Check your credentials and try again.';
      if (registering && error.status === 409) {
        return 'We couldn’t create this account. Try signing in if you’ve already registered.';
      }
      if (registering && error.status === 400) return 'Check your email and password and try again.';
    }
    return 'The account service is unavailable. Try again in a moment.';
  }
}
