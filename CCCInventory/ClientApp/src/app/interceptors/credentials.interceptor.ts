import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { PinSessionService } from '../services/pin-session.service';

export const credentialsInterceptor: HttpInterceptorFn = (req, next) => {
  const pinSession = inject(PinSessionService);
  const router = inject(Router);

  const staffId = pinSession.activeStaffMemberId;

  let modified = req.clone({ withCredentials: true });
  if (staffId !== null) {
    modified = modified.clone({
      setHeaders: { 'X-Staff-Member-Id': staffId.toString() }
    });
  }

  return next(modified).pipe(
    catchError((error: HttpErrorResponse) => {
      // Redirect to login on 401, but not for the status/login endpoints themselves
      if (error.status === 401
        && !req.url.includes('/api/auth/status')
        && !req.url.includes('/api/auth/login')
        && !req.url.includes('/api/staffmembers/validate-pin')) {
        router.navigate(['/login']);
      }
      return throwError(() => error);
    })
  );
};
