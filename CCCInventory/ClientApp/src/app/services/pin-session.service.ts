import { Injectable } from '@angular/core';

const KEY_ID = 'active_staff_member_id';
const KEY_NAME = 'active_staff_member_name';

@Injectable({ providedIn: 'root' })
export class PinSessionService {
  get activeStaffMemberId(): number | null {
    const val = sessionStorage.getItem(KEY_ID);
    if (!val) return null;
    const parsed = parseInt(val, 10);
    return isNaN(parsed) ? null : parsed;
  }

  get activeStaffMemberName(): string {
    return sessionStorage.getItem(KEY_NAME) ?? '';
  }

  get isActive(): boolean {
    return this.activeStaffMemberId !== null;
  }

  setStaffMember(id: number, name: string): void {
    sessionStorage.setItem(KEY_ID, id.toString());
    sessionStorage.setItem(KEY_NAME, name);
  }

  clear(): void {
    sessionStorage.removeItem(KEY_ID);
    sessionStorage.removeItem(KEY_NAME);
  }
}
