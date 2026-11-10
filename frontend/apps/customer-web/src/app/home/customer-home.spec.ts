import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { bundledExperience } from '../../../../../shared/experience/experience-document';
import { CustomerHome } from './customer-home';

describe('Customer homepage configuration', () => {
  const url = '/api/v1/experience/customer-home';
  const storageKey = 'findyourfood.customer-home.v1';
  let http: HttpTestingController;
  let fixture: ComponentFixture<CustomerHome>;

  beforeEach(async () => {
    localStorage.removeItem(storageKey);
    await TestBed.configureTestingModule({
      imports: [CustomerHome],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.removeItem(storageKey);
  });

  function start() {
    fixture = TestBed.createComponent(CustomerHome);
    fixture.detectChanges();
    return http.expectOne(url);
  }

  it('stores and renders a validated publication', async () => {
    const publication = { version: 2, configuration: { ...bundledExperience, palette: 'ocean' } };
    start().flush(publication);
    await Promise.resolve();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.experience').dataset.palette).toBe('ocean');
    expect(JSON.parse(localStorage.getItem(storageKey)!)).toEqual(publication);
  });

  it('uses the last validated publication when the service is unavailable', async () => {
    localStorage.setItem(storageKey, JSON.stringify({
      version: 2, configuration: { ...bundledExperience, palette: 'plum' },
    }));
    start().flush(null, { status: 503, statusText: 'Unavailable' });
    await Promise.resolve();
    expect(fixture.componentInstance.configuration().palette).toBe('plum');
  });

  it('keeps the bundled homepage when a response contains an unknown component', async () => {
    start().flush({
      version: 1,
      configuration: { ...bundledExperience, sections: [{ kind: 'iframe', title: 'Invalid', body: 'Invalid' }] },
    });
    await Promise.resolve();
    expect(fixture.componentInstance.configuration()).toEqual(bundledExperience);
    expect(localStorage.getItem(storageKey)).toBeNull();
  });
});
