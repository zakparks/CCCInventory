import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { fromEvent, merge, Subscription } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { PinSessionService } from './pin-session.service';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class InactivityService {
  private subscription: Subscription | null = null;

  constructor(
    private pinSession: PinSessionService,
    private auth: AuthService,
    private router: Router
  ) { }

  start(): void {
    this.stop(); // clear any existing subscription

    const timeoutMs = this.auth.inactivityTimeoutMinutes * 60 * 1000;

    const activity$ = merge(
      fromEvent(document, 'mousemove'),
      fromEvent(document, 'keydown'),
      fromEvent(document, 'click'),
      fromEvent(document, 'touchstart')
    );

    this.subscription = activity$.pipe(debounceTime(timeoutMs)).subscribe(() => {
      this.stop();
      this.pinSession.clear();
      this.router.navigate(['/pin']);
    });
  }

  stop(): void {
    this.subscription?.unsubscribe();
    this.subscription = null;
  }
}
