import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { PinSessionService } from '../services/pin-session.service';

export const pinGuard: CanActivateFn = () => {
  const pin = inject(PinSessionService);
  const router = inject(Router);

  if (pin.isActive) return true;
  return router.createUrlTree(['/pin']);
};
