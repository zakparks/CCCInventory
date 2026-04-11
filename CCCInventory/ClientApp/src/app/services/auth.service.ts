import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';

export interface AuthStatus {
  isAuthenticated: boolean;
  username: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private currentUser$ = new BehaviorSubject<AuthStatus | null>(null);
  inactivityTimeoutMinutes = 5;

  constructor(private http: HttpClient) { }

  // Called once via APP_INITIALIZER before any guard runs.
  // Resolves to the auth status (or null on 401) and caches the result.
  initialize(): Promise<void> {
    const status$ = this.http.get<AuthStatus>('/api/auth/status')
      .pipe(
        tap(status => this.currentUser$.next(status)),
        catchError(() => {
          this.currentUser$.next(null);
          return of(null);
        })
      );

    const config$ = this.http.get<{ inactivityTimeoutMinutes: number }>('/api/auth/config')
      .pipe(
        tap(config => this.inactivityTimeoutMinutes = config.inactivityTimeoutMinutes),
        catchError(() => of(null))
      );

    return Promise.all([status$.toPromise(), config$.toPromise()]).then(() => void 0);
  }

  get isAuthenticated(): boolean {
    return this.currentUser$.value?.isAuthenticated === true;
  }

  get username(): string {
    return this.currentUser$.value?.username ?? '';
  }

  login(username: string, password: string): Observable<void> {
    return this.http.post<{ username: string }>('/api/auth/login', { username, password })
      .pipe(
        tap(res => this.currentUser$.next({ isAuthenticated: true, username: res.username })),
        map(() => void 0)
      );
  }

  logout(): Observable<void> {
    return this.http.post<void>('/api/auth/logout', {})
      .pipe(tap(() => this.currentUser$.next(null)));
  }

  changePassword(currentPassword: string, newPassword: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>('/api/auth/change-password', { currentPassword, newPassword });
  }
}
