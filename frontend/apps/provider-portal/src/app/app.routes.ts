import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    title: 'Provider portal | FindYourFood',
    loadComponent: () => import('./onboarding/provider-page').then((module) => module.ProviderPage),
  },
  { path: '**', redirectTo: '' },
];
