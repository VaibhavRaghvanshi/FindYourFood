import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, input, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { CatalogApi, MenuContent, MenuItem, MenuOption, MenuWorkspace } from '../../../../../shared/catalog/catalog-api';

@Component({
  selector: 'app-menu-editor',
  imports: [FormsModule],
  templateUrl: './menu-editor.html',
  styleUrl: './menu-editor.css',
})
export class MenuEditor implements OnInit {
  readonly branch = input.required<string>();
  readonly token = input.required<string>();
  private readonly api = inject(CatalogApi);
  readonly workspace = signal<MenuWorkspace | null>(null);
  readonly menu = signal<MenuContent>({ name: '', items: [] });
  readonly busy = signal(false);
  readonly dirty = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  readonly listFields = ['ingredients', 'tags', 'allergens'] as const;
  readonly optionFields = ['variants', 'addOns'] as const;

  ngOnInit() { void this.load(); }

  load() {
    return this.run(async () => {
      const state = await firstValueFrom(this.api.workspace(this.branch(), this.token()));
      this.workspace.set(state);
      this.menu.set(state.draft ?? { name: '', items: [] });
      this.dirty.set(false);
    });
  }

  name(value: string) { this.change({ ...this.menu(), name: value }); }

  addItem() {
    const item: MenuItem = { id: crypto.randomUUID(), category: '', name: '', description: '', priceMinor: 0,
      available: true, ingredients: [], tags: [], allergens: [], variants: [], addOns: [] };
    this.change({ ...this.menu(), items: [...this.menu().items, item] });
  }

  removeItem(id: string) {
    this.change({ ...this.menu(), items: this.menu().items.filter((item) => item.id !== id) });
  }

  updateItem(id: string, field: keyof MenuItem, value: MenuItem[keyof MenuItem]) {
    this.change({ ...this.menu(), items: this.menu().items.map((item) =>
      item.id === id ? { ...item, [field]: value } : item) });
  }

  setList(item: MenuItem, field: 'ingredients' | 'tags' | 'allergens', value: string) {
    this.updateItem(item.id, field, value.split(',').map((entry) => entry.trim()).filter(Boolean));
  }

  addOption(item: MenuItem, field: 'variants' | 'addOns') {
    this.updateItem(item.id, field, [...item[field], { id: crypto.randomUUID(), name: '', extraMinor: 0 }]);
  }

  removeOption(item: MenuItem, field: 'variants' | 'addOns', id: string) {
    this.updateItem(item.id, field, item[field].filter((option) => option.id !== id));
  }

  option(item: MenuItem, field: 'variants' | 'addOns', id: string, value: Partial<MenuOption>) {
    this.updateItem(item.id, field, item[field].map((option) => option.id === id ? { ...option, ...value } : option));
  }

  minor(value: number | null) { return Math.round((value ?? 0) * 100); }

  save() {
    const state = this.workspace();
    if (!state) return;
    return this.run(async () => {
      this.workspace.set(await firstValueFrom(this.api.save(this.branch(), this.token(), state.revision, this.menu())));
      this.dirty.set(false);
      this.notice.set('Menu draft saved.');
    });
  }

  publish() {
    const state = this.workspace();
    if (!state || this.dirty()) return;
    return this.run(async () => {
      this.workspace.set(await firstValueFrom(this.api.publish(this.branch(), this.token(), state.revision)));
      this.notice.set('Menu published.');
    });
  }

  private change(menu: MenuContent) { this.menu.set(menu); this.dirty.set(true); }

  private async run(action: () => Promise<void>) {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.notice.set('');
    try {
      await action();
    } catch (error) {
      const status = error instanceof HttpErrorResponse ? error.status : 0;
      this.error.set(status === 409 ? 'Reload the saved menu before retrying.' :
        status === 400 ? 'Check names, prices, options and label limits.' :
        status === 401 ? 'Your session expired. Sign in again.' :
        status === 403 ? 'Only the approved branch owner can manage this menu.' :
        'The catalog service is unavailable. Try again shortly.');
    } finally {
      this.busy.set(false);
    }
  }
}
