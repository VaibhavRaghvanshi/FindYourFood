import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ExperienceDocument, ExperienceLink } from './experience-document';

@Component({
  selector: 'fyf-experience-view',
  imports: [RouterLink],
  templateUrl: './experience-view.html',
  styleUrl: './experience-view.css',
})
export class ExperienceView {
  readonly configuration = input.required<ExperienceDocument>();
  readonly preview = input(false);

  route(target: ExperienceLink['target']) {
    return target === 'account' ? '/' : '/home';
  }
}
