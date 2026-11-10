import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, input, OnInit, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { documentKinds, ProviderApi, ProviderSummary, ProviderView, saveProviderDocument } from '../../../../../shared/provider/provider-api';

@Component({
  selector: 'app-provider-review',
  imports: [FormsModule],
  templateUrl: './provider-review-page.html',
  styleUrl: './provider-review-page.css',
})
export class ProviderReviewPage implements OnInit {
  readonly token = input.required<string>();
  readonly sessionExpired = output<void>();
  private readonly api = inject(ProviderApi);
  readonly queue = signal<ProviderSummary[]>([]);
  readonly queueRestricted = signal(false);
  readonly selected = signal<ProviderView | null>(null);
  readonly busy = signal(false);
  readonly notice = signal('');
  readonly error = signal('');
  readonly documentKinds = documentKinds;
  lookupId = '';
  note = '';

  ngOnInit() { void this.refresh(); }

  refresh() {
    return this.run(async () => {
      try {
        this.queue.set(await firstValueFrom(this.api.pending(this.token())));
        this.queueRestricted.set(false);
      } catch (error) {
        if (!(error instanceof HttpErrorResponse) || error.status !== 403) throw error;
        this.queue.set([]);
        this.queueRestricted.set(true);
      }
    });
  }

  open(id: string) {
    const applicationId = id.trim();
    if (!/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(applicationId)) {
      this.error.set('Enter a valid provider ID.');
      return;
    }
    return this.run(async () => {
      this.selected.set(await firstValueFrom(this.api.application(this.token(), applicationId)));
      this.note = '';
    });
  }

  download(kind: string) {
    const application = this.selected();
    if (!application) return;
    return this.run(async () =>
      saveProviderDocument(await firstValueFrom(this.api.download(this.token(), kind, application.id)), kind));
  }

  decide(approved: boolean) {
    const application = this.selected();
    if (!application) return;
    return this.run(async () => {
      const result = await firstValueFrom(
        this.api.review(this.token(), application.id, application.revision, approved, this.note.trim() || null),
      );
      this.selected.set(result);
      this.queue.update((queue) => queue.filter((item) => item.id !== result.id));
      this.notice.set(approved ? 'Provider approved.' : 'Application returned with feedback.');
      this.note = '';
    });
  }

  private async run(action: () => Promise<void>) {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.notice.set('');
    try {
      await action();
    } catch (error) {
      const status = error instanceof HttpErrorResponse ? error.status : 0;
      if (status === 401) {
        this.sessionExpired.emit();
      } else if (status === 403) {
        this.error.set('Your account lacks review permission for this provider.');
      } else if (status === 409) {
        this.error.set('The application changed or is no longer awaiting review. Reopen it before retrying.');
      } else if (status === 400) {
        this.error.set('Check the review fields. A note is required for rejection.');
      } else if (status === 404) {
        this.error.set('The requested application or document could not be found.');
      } else {
        this.error.set('The provider review service is unavailable. Try again shortly.');
      }
    } finally {
      this.busy.set(false);
    }
  }
}
