import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface StaffMember {
  id: number;
  name: string;
  isActive: boolean;
  pin?: string; // write-only: sent on create/update, never returned by GET
}

@Injectable({ providedIn: 'root' })
export class StaffService {
  constructor(private http: HttpClient) { }

  getAll(): Observable<StaffMember[]> {
    return this.http.get<StaffMember[]>('/api/staffmembers');
  }

  getActive(): Observable<StaffMember[]> {
    return this.getAll().pipe(map(s => s.filter(m => m.isActive)));
  }

  create(member: Omit<StaffMember, 'id'>): Observable<StaffMember> {
    return this.http.post<StaffMember>('/api/staffmembers', member);
  }

  update(id: number, member: StaffMember): Observable<StaffMember> {
    return this.http.put<StaffMember>(`/api/staffmembers/${id}`, member);
  }

  deactivate(id: number): Observable<StaffMember> {
    return this.http.delete<StaffMember>(`/api/staffmembers/${id}`);
  }

  validatePin(pin: string): Observable<{ id: number; name: string }> {
    return this.http.post<{ id: number; name: string }>('/api/staffmembers/validate-pin', { pin });
  }
}
