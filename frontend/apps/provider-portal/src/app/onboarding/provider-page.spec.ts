import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { ProviderPage } from './provider-page';

describe('Provider portal', () => {
  let http: HttpTestingController;
  let page: ProviderPage;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProviderPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    page = TestBed.createComponent(ProviderPage).componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads only the signed-in account’s application and supports first-time registration', async () => {
    page.email = 'kitchen@example.test';
    page.password = 'synthetic passphrase';
    page.register.set(true);
    const signingIn = page.signIn();
    http.expectOne('/api/v1/identity/register').flush(null, { status: 201, statusText: 'Created' });
    await Promise.resolve();
    http.expectOne('/api/v1/identity/login').flush({ accessToken: 'test-token' });
    await Promise.resolve();
    const profile = http.expectOne('/api/v1/providers/me');
    expect(profile.request.headers.get('Authorization')).toBe('Bearer test-token');
    profile.flush(null, { status: 404, statusText: 'Not Found' });
    await signingIn;
    expect(page.loaded()).toBe(true);
    expect(page.profile()).toBeNull();
    expect(page.details().contactEmail).toBe('kitchen@example.test');
  });

  it('edits hours per weekday and removes closed days from the schedule', () => {
    page.toggleDay(1, false);
    page.toggleDay(6, true);
    page.editHours(6, 'closes', '19:15');
    expect(page.hours(1)).toBeUndefined();
    expect(page.hours(6)?.closes).toBe('19:15');
  });
});
