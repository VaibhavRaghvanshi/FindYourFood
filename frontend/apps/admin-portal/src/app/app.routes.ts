import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    title: 'Staff access | FindYourFood',
    loadComponent: () => import('./staff-access/staff-access-page').then((module) => module.StaffAccessPage),
  },
  { path: '**', redirectTo: '' },
];
