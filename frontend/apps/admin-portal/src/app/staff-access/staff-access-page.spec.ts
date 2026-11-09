import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { StaffAccessPage } from './staff-access-page';

describe('Staff access portal', () => {
  const root = '/api/v1/organization';
  const login = '/api/v1/identity/staff/login';
  const subjectId = 'ff89c82f-4ac2-4373-9adb-d791613d65d8';
  let page: StaffAccessPage;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StaffAccessPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    page = TestBed.createComponent(StaffAccessPage).componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function signIn() {
    page.credentials.setValue({ email: 'staff@example.test', password: 'synthetic passphrase', code: '012345' });
    const signingIn = page.signIn();
    const request = http.expectOne(login);
    expect(request.request.body.code).toBe('012345');
    request.flush({ accessToken: 'test-token' });
    await Promise.resolve();
    const list = http.expectOne(root + '/grants?page=0');
    expect(list.request.headers.get('Authorization')).toBe('Bearer test-token');
    list.flush([]);
    await signingIn;
  }

  it('validates the authenticator code before sending credentials', async () => {
    page.credentials.setValue({ email: 'staff@example.test', password: 'synthetic passphrase', code: '123' });
    await page.signIn();
    expect(page.error()).toContain('six-digit');
    http.expectNone(login);
  });

  it('signs in with MFA and creates a scoped grant with the bearer token', async () => {
    await signIn();
    expect(page.signedIn()).toBe(true);
    expect(page.credentials.controls.password.value).toBe('');
    page.assignment.patchValue({ subjectId, scope: '/city/pune' });
    const saving = page.grantAccess();
    const request = http.expectOne(root + '/grants');
    expect(request.request.method).toBe('POST');
    expect(request.request.headers.get('Authorization')).toBe('Bearer test-token');
    expect(request.request.body).toMatchObject({ subjectId, scope: '/city/pune', parentGrantId: null });
    request.flush({ ...request.request.body, id: subjectId, revokedAt: null });
    await saving;
    expect(page.notice()).toBe('Permission granted.');
    expect(page.grants()).toHaveLength(1);
  });

  it('reports a denied permission change and clears the session on expiration', async () => {
    await signIn();
    page.assignment.patchValue({ subjectId, scope: '/city/pune' });
    const saving = page.grantAccess();
    http.expectOne(root + '/grants').flush({}, { status: 403, statusText: 'Forbidden' });
    await saving;
    expect(page.error()).toContain('lacks authority');
    expect(page.signedIn()).toBe(true);

    const loading = page.loadPage(0);
    http.expectOne(root + '/grants?page=0').flush({}, { status: 401, statusText: 'Unauthorized' });
    await loading;
    expect(page.signedIn()).toBe(false);
    expect(page.error()).toContain('session expired');
  });
});
