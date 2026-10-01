import { Component } from '@angular/core';
import { NgbTooltip } from '@ng-bootstrap/ng-bootstrap';

// Small ring icon marking a wedding order; "Wedding Order" on hover.
// Inline SVG rather than an emoji so it renders and prints the same everywhere.
@Component({
  selector: 'app-wedding-icon',
  standalone: true,
  imports: [NgbTooltip],
  template: `<span class="wedding-icon" ngbTooltip="Wedding Order" container="body" role="img" aria-label="Wedding Order">
    <svg viewBox="0 0 16 16" width="1em" height="1em" aria-hidden="true">
      <path d="M8 1.2 L10 3.2 L8 5.6 L6 3.2 Z" fill="#7ec8e3" stroke="#3a7ca5" stroke-width="0.7" stroke-linejoin="round" />
      <circle cx="8" cy="10.3" r="4.4" fill="none" stroke="#c9a227" stroke-width="1.8" />
    </svg>
  </span>`,
  styles: [`.wedding-icon { cursor: default; display: inline-flex; vertical-align: -0.125em; }`]
})
export class WeddingIconComponent { }
