import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'delivery-area',
    title: 'Delivery coverage | FindYourFood',
    loadComponent: () => import('./location/delivery-check').then((module) => module.DeliveryCheck),
  },
  {
    path: 'account',
    title: 'Account settings | FindYourFood',
    loadComponent: () => import('./account/customer-account').then((module) => module.CustomerAccount),
  },
  {
    path: 'menu/:branch',
    title: 'Menu | FindYourFood',
    loadComponent: () => import('./menu/customer-menu').then((module) => module.CustomerMenu),
  },
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
