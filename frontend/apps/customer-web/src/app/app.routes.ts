import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'checkout',
    title: 'Checkout | FindYourFood',
    loadComponent: () => import('./orders/checkout-page').then((module) => module.CheckoutPage),
  },
  {
    path: 'orders/:id',
    title: 'Order status | FindYourFood',
    loadComponent: () => import('./orders/order-details').then((module) => module.OrderDetails),
  },
  {
    path: 'orders',
    title: 'Your orders | FindYourFood',
    loadComponent: () => import('./orders/order-history').then((module) => module.OrderHistory),
  },
  {
    path: 'cart',
    title: 'Your cart | FindYourFood',
    loadComponent: () => import('./cart/cart-page').then((module) => module.CartPage),
  },
  {
    path: 'restaurants',
    title: 'Find food | FindYourFood',
    loadComponent: () => import('./discovery/restaurant-search').then((module) => module.RestaurantSearch),
  },
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
