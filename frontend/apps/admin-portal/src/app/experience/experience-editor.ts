import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, input, OnInit, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import {
  bundledExperience,
  ExperienceDocument,
  ExperienceLink,
  ExperienceSection,
  isExperienceDocument,
  palettes,
  typographyOptions,
} from '../../../../../shared/experience/experience-document';
import { ExperienceView } from '../../../../../shared/experience/experience-view';
import { ExperienceAdminApi, Workspace } from './experience-admin-api';

@Component({
  selector: 'app-experience-editor',
  imports: [DatePipe, FormsModule, ExperienceView],
  templateUrl: './experience-editor.html',
  styleUrl: './experience-editor.css',
})
export class ExperienceEditor implements OnInit {
  readonly token = input.required<string>();
  readonly sessionExpired = output<void>();
  private readonly api = inject(ExperienceAdminApi);
  readonly workspace = signal<Workspace | null>(null);
  readonly configuration = signal<ExperienceDocument>(bundledExperience);
  readonly preview = signal<ExperienceDocument>(bundledExperience);
  readonly busy = signal(false);
  readonly dirty = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  readonly rollbackVersion = signal<number | null>(null);
  readonly valid = computed(() => isExperienceDocument(this.configuration()));
  readonly palettes = palettes;
  readonly typographyOptions = typographyOptions;
  readonly navigationOptions = bundledExperience.navigation;
  readonly optionalSections: ExperienceSection[] = [
    { kind: 'banner', title: 'On the menu', body: 'See what is happening at FindYourFood.' },
    { kind: 'note', title: 'Make yourself at home', body: 'Find your next favorite meal.' },
  ];

  ngOnInit() {
    void this.load();
  }

  async load() {
    await this.run(async () => {
      const workspace = await firstValueFrom(this.api.workspace(this.token()));
      this.accept(workspace);
      this.configuration.set(workspace.draft ?? bundledExperience);
      this.preview.set(this.configuration());
      this.dirty.set(false);
    });
  }

  set<K extends keyof ExperienceDocument>(key: K, value: ExperienceDocument[K]) {
    this.configuration.update((configuration) => ({ ...configuration, [key]: value }));
    this.dirty.set(true);
  }

  link(target: ExperienceLink['target']) {
    return this.configuration().navigation.find((item) => item.target === target);
  }

  toggleLink(item: ExperienceLink, enabled: boolean) {
    const navigation = this.configuration().navigation.filter((link) => link.target !== item.target);
    this.set('navigation', enabled ? [...navigation, item] : navigation);
  }

  setLinkLabel(target: ExperienceLink['target'], label: string) {
    this.set('navigation', this.configuration().navigation.map((link) =>
      link.target === target ? { ...link, label } : link));
  }

  hasSection(kind: ExperienceSection['kind']) {
    return this.configuration().sections.some((section) => section.kind === kind);
  }

  addSection(section: ExperienceSection) {
    if (!this.hasSection(section.kind)) this.set('sections', [...this.configuration().sections, section]);
  }

  removeSection(kind: ExperienceSection['kind']) {
    if (kind !== 'hero') this.set('sections', this.configuration().sections.filter((section) => section.kind !== kind));
  }

  moveSection(index: number, direction: -1 | 1) {
    const sections = [...this.configuration().sections];
    const target = index + direction;
    if (target < 0 || target >= sections.length) return;
    [sections[index], sections[target]] = [sections[target], sections[index]];
    this.set('sections', sections);
  }

  setSection(kind: ExperienceSection['kind'], field: 'title' | 'body', value: string) {
    this.set('sections', this.configuration().sections.map((section) =>
      section.kind === kind ? { ...section, [field]: value } : section));
  }

  async validatePreview() {
    if (!this.valid()) return;
    await this.run(async () => {
      this.preview.set(await firstValueFrom(this.api.preview(this.token(), this.configuration())));
      this.notice.set('Preview validated.');
    });
  }

  async save() {
    const workspace = this.workspace();
    if (!workspace || !this.valid()) return;
    await this.run(async () => {
      const saved = await firstValueFrom(this.api.saveDraft(this.token(), workspace.revision, this.configuration()));
      this.accept(saved);
      this.preview.set(saved.draft ?? bundledExperience);
      this.dirty.set(false);
      this.notice.set('Draft saved.');
    });
  }

  async transition(action: 'approve' | 'publish') {
    const workspace = this.workspace();
    if (!workspace || this.dirty()) return;
    await this.run(async () => {
      this.accept(await firstValueFrom(this.api.transition(this.token(), action, workspace.revision)));
      this.notice.set(action === 'approve' ? 'Draft approved.' : 'Customer homepage published.');
    });
  }

  async rollback(version: number) {
    const workspace = this.workspace();
    if (!workspace || this.dirty()) return;
    await this.run(async () => {
      this.accept(await firstValueFrom(this.api.rollback(this.token(), workspace.revision, version)));
      this.notice.set(`Restored version ${version} as a new publication.`);
    });
  }

  private accept(workspace: Workspace) {
    this.workspace.set(workspace);
    this.rollbackVersion.set(null);
  }

  private async run(operation: () => Promise<void>) {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.notice.set('');
    try {
      await operation();
    } catch (error) {
      const status = error instanceof HttpErrorResponse ? error.status : 0;
      if (status === 401) {
        this.sessionExpired.emit();
      } else if (status === 403) {
        this.error.set('Your account lacks permission for this experience action.');
      } else if (status === 409) {
        this.error.set('The workspace changed or the action is unavailable. Reload the saved workspace.');
      } else if (status === 400) {
        this.error.set('Check the theme, navigation and section values.');
      } else {
        this.error.set('The experience service is unavailable. Try again shortly.');
      }
    } finally {
      this.busy.set(false);
    }
  }
}
