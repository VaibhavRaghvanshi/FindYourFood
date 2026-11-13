import { CurrencyPipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CartSession } from './cart-session';

@Component({
  imports: [CurrencyPipe, FormsModule, RouterLink],
  templateUrl: './cart-page.html',
  styleUrl: './cart-page.css',
})
export class CartPage implements OnInit {
  readonly cart = inject(CartSession);
  latitude: number | null = null;
  longitude: number | null = null;
  ngOnInit() { void this.cart.load(); }

  locationChanged(key: 'latitude' | 'longitude', value: number | null) {
    this[key] = value;
    this.cart.invalidateQuote();
  }
}
