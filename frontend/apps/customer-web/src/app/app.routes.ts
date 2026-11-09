import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    title: 'Customer account | FindYourFood',
    loadComponent: () => import('./identity/identity-page').then((module) => module.IdentityPage),
  },
  { path: '**', redirectTo: '' },
];
