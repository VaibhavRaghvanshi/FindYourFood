import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'home',
    title: 'FindYourFood',
    loadComponent: () => import('./home/customer-home').then((module) => module.CustomerHome),
  },
  {
    path: '',
    title: 'Customer account | FindYourFood',
    loadComponent: () => import('./identity/identity-page').then((module) => module.IdentityPage),
  },
  { path: '**', redirectTo: '' },
];
