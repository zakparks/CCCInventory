import { Component, ElementRef, Input, ViewChild, forwardRef } from '@angular/core';
import { NgClass } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap';

// Floating-label time field: type anything ("3", "330p", "3:30 pm", "15:30", "noon") or pick
// hour / 15-minute / AM-PM from the clock button. The form value is "HH:mm" (24h) or ''.
@Component({
  selector: 'app-time-input',
  standalone: true,
  imports: [NgClass, NgbDropdownModule],
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => TimeInputComponent), multi: true }],
  template: `
    <div class="input-group has-validation" ngbDropdown #dd="ngbDropdown" placement="bottom-end" autoClose="outside">
      <div class="form-floating">
        <input #box type="text" class="form-control" [id]="inputId" [value]="text" placeholder="h:mm AM"
          autocomplete="off" [disabled]="disabled" [ngClass]="{'is-invalid': invalid || parseError}"
          (input)="text = $any($event.target).value; parseError = false"
          (blur)="commit()" (keydown.enter)="commit(); $event.preventDefault()" />
        <label [for]="inputId">{{ label }}</label>
        @if (parseError) { <div class="invalid-feedback">Try a time like 2:30 PM</div> }
      </div>
      <button type="button" class="btn btn-outline-secondary" ngbDropdownToggle tabindex="-1"
        [disabled]="disabled" aria-label="Pick a time">
        <svg viewBox="0 0 16 16" width="1em" height="1em" aria-hidden="true" fill="currentColor">
          <path d="M8 3.5a.5.5 0 0 0-1 0V9a.5.5 0 0 0 .252.434l3.5 2a.5.5 0 0 0 .496-.868L8 8.71z"/>
          <path d="M8 16A8 8 0 1 0 8 0a8 8 0 0 0 0 16m7-8A7 7 0 1 1 1 8a7 7 0 0 1 14 0"/>
        </svg>
      </button>
      <div ngbDropdownMenu class="p-2 time-picker">
        <div class="small text-muted mb-1">Hour</div>
        <div class="d-grid gap-1 mb-2" style="grid-template-columns: repeat(6, 2.4rem);">
          @for (h of hours; track h) {
            <button type="button" class="btn btn-sm" [class.btn-primary]="h === pickHour"
              [class.btn-outline-secondary]="h !== pickHour" (click)="pick(h, pickMinute, pickPm)">{{ h }}</button>
          }
        </div>
        <div class="small text-muted mb-1">Minute</div>
        <div class="d-flex gap-1 mb-2">
          @for (m of minutes; track m) {
            <button type="button" class="btn btn-sm" style="width: 2.4rem" [class.btn-primary]="m === pickMinute"
              [class.btn-outline-secondary]="m !== pickMinute" (click)="pick(pickHour, m, pickPm)">:{{ m }}</button>
          }
        </div>
        <div class="d-flex gap-1 align-items-center">
          <button type="button" class="btn btn-sm" [class.btn-primary]="!pickPm" [class.btn-outline-secondary]="pickPm"
            (click)="pick(pickHour, pickMinute, false)">AM</button>
          <button type="button" class="btn btn-sm" [class.btn-primary]="pickPm" [class.btn-outline-secondary]="!pickPm"
            (click)="pick(pickHour, pickMinute, true)">PM</button>
          <button type="button" class="btn btn-sm btn-link ms-auto" (click)="clear(); dd.close()">Clear</button>
          <button type="button" class="btn btn-sm btn-outline-primary" (click)="dd.close()">Done</button>
        </div>
      </div>
    </div>`
})
export class TimeInputComponent implements ControlValueAccessor {
  @Input() label = '';
  @Input() inputId = '';
  @Input() invalid = false;
  @ViewChild('box') private box?: ElementRef<HTMLInputElement>;

  readonly hours = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
  readonly minutes = ['00', '15', '30', '45'];

  text = '';
  value = '';            // "HH:mm" or ''
  parseError = false;
  disabled = false;
  pickHour: number | null = null;
  pickMinute = '00';
  pickPm = true;

  private onChange: (v: string) => void = () => { };
  private onTouched: () => void = () => { };

  writeValue(v: string | null): void {
    this.setValue(v ?? '', false);
  }
  registerOnChange(fn: (v: string) => void): void { this.onChange = fn; }
  registerOnTouched(fn: () => void): void { this.onTouched = fn; }
  setDisabledState(d: boolean): void { this.disabled = d; }

  commit() {
    this.onTouched();
    const parsed = TimeInputComponent.parse(this.text);
    if (parsed === undefined) {
      this.parseError = true;     // leave the typed text so it can be fixed; keep the last good value
      return;
    }
    this.setValue(parsed, true);
  }

  pick(hour: number | null, minute: string, pm: boolean) {
    const h12 = hour ?? 12;
    const h24 = (h12 % 12) + (pm ? 12 : 0);
    this.setValue(`${String(h24).padStart(2, '0')}:${minute}`, true);
  }

  clear() {
    this.setValue('', true);
  }

  private setValue(v: string, emit: boolean) {
    this.value = v;
    this.text = TimeInputComponent.display(v);
    // Write the formatted text straight to the field too; the [value] binding alone can skip the update
    // when the typed text and the formatted text change within one change-detection pass.
    if (this.box) this.box.nativeElement.value = this.text;
    this.parseError = false;
    const [h, m] = v ? v.split(':').map(Number) : [NaN, NaN];
    if (!isNaN(h)) {
      this.pickHour = h % 12 === 0 ? 12 : h % 12;
      this.pickMinute = this.minutes.includes(String(m).padStart(2, '0')) ? String(m).padStart(2, '0') : '00';
      this.pickPm = h >= 12;
    } else {
      this.pickHour = null;
    }
    if (emit) this.onChange(v);
  }

  static display(v: string): string {
    if (!v) return '';
    const [h, m] = v.split(':').map(Number);
    if (isNaN(h) || isNaN(m)) return v;
    return `${h % 12 === 0 ? 12 : h % 12}:${String(m).padStart(2, '0')} ${h >= 12 ? 'PM' : 'AM'}`;
  }

  // '' → '' (cleared), unparseable → undefined, otherwise "HH:mm".
  // Without AM/PM, 1–6 is read as PM and 7–11 as AM (bakery hours); 13–23 and 0 are 24-hour times.
  static parse(input: string): string | undefined {
    const s = input.trim().toLowerCase().replace(/\./g, '');
    if (!s) return '';
    if (s === 'noon') return '12:00';
    if (s === 'midnight') return '00:00';
    const m = s.match(/^(\d{1,2})(?::?(\d{2}))?\s*(a|p|am|pm)?$/);
    if (!m) return undefined;
    let h = Number(m[1]);
    const min = m[2] ? Number(m[2]) : 0;
    const ap = m[3]?.[0];
    if (min > 59) return undefined;
    if (ap) {
      if (h < 1 || h > 12) return undefined;
      h = (h % 12) + (ap === 'p' ? 12 : 0);
    } else if (h > 23) {
      return undefined;
    } else if (h >= 1 && h <= 6) {
      h += 12;
    }
    return `${String(h).padStart(2, '0')}:${String(min).padStart(2, '0')}`;
  }
}
