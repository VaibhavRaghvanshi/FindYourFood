import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { IdentityPage } from './identity-page';

describe('Customer identity page', () => {
  const root = '/api/v1/identity';
  let page: IdentityPage;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [IdentityPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    page = TestBed.createComponent(IdentityPage).componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('blocks invalid registration before issuing an HTTP request', async () => {
    page.chooseMode('register');
    page.form.setValue({ email: 'test@example.test', password: 'short' });
    await page.submit();
    expect(page.form.controls.password.touched).toBe(true);
    expect(page.form.controls.password.invalid).toBe(true);
    http.expectNone(root + '/register');
  });

  it('shows pending and success states, then returns to sign-in', async () => {
    page.chooseMode('register');
    page.form.setValue({ email: 'test@example.test', password: 'test account passphrase' });
    const result = page.submit();
    expect(page.busy()).toBe(true);
    const request = http.expectOne(root + '/register');
    expect(request.request.method).toBe('POST');
    request.flush(null, { status: 201, statusText: 'Created' });
    await result;
    expect(page.mode()).toBe('login');
    expect(page.notice()).toContain('Account created');
    expect(page.form.controls.password.value).toBe('');
    expect(page.busy()).toBe(false);
  });

  it('uses the returned bearer token only for the protected profile request', async () => {
    page.form.setValue({ email: 'test@example.test', password: 'test account passphrase' });
    const result = page.submit();
    http.expectOne(root + '/login').flush({ accessToken: 'synthetic', expiresIn: 600, tokenType: 'Bearer' });
    const profile = http.expectOne(root + '/me');
    expect(profile.request.headers.get('Authorization')).toBe('Bearer synthetic');
    profile.flush({ id: 'example', email: 'test@example.test', emailVerified: false });
    await result;
    expect(page.profile()?.email).toBe('test@example.test');
    page.signOut();
    expect(page.profile()).toBeNull();
  });

  it('shows a generic error for denied login', async () => {
    page.form.setValue({ email: 'test@example.test', password: 'incorrect password' });
    const result = page.submit();
    http.expectOne(root + '/login').flush({}, { status: 401, statusText: 'Unauthorized' });
    await result;
    expect(page.error()).toContain('Sign-in failed');
    expect(page.busy()).toBe(false);
    http.expectNone(root + '/me');
  });
});
