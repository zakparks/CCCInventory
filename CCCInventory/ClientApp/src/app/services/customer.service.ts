import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Customer, CustomerSearchResult } from '../models/customer';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class CustomerService {
  private readonly baseUrl = `${environment.apiUrl}/Customer`;

  constructor(private http: HttpClient) { }

  public GetAll(): Observable<Customer[]> {
    return this.http.get<Customer[]>(this.baseUrl);
  }

  public GetById(id: number): Observable<Customer> {
    return this.http.get<Customer>(`${this.baseUrl}/${id}`);
  }

  public Search(q: string): Observable<CustomerSearchResult[]> {
    return this.http.get<CustomerSearchResult[]>(`${this.baseUrl}/search?q=${encodeURIComponent(q)}`);
  }

  public Create(customer: Customer): Observable<number> {
    return this.http.post<number>(this.baseUrl, customer);
  }

  public Update(customer: Customer): Observable<number> {
    return this.http.put<number>(this.baseUrl, customer);
  }

  public Delete(id: number): Observable<number> {
    return this.http.delete<number>(`${this.baseUrl}/${id}`);
  }

  public Merge(keepId: number, mergeId: number): Observable<number> {
    return this.http.post<number>(`${this.baseUrl}/merge`, { keepId, mergeId });
  }
}
