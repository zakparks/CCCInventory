import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { NgbHighlight } from '@ng-bootstrap/ng-bootstrap';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { CustomerService } from '../../services/customer.service';
import { Customer } from '../../models/customer';

@Component({
  selector: 'app-customers',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, NgbHighlight],
  templateUrl: './customers.component.html'
})
export class CustomersComponent implements OnInit {
  allCustomers: Customer[] = [];
  displayCustomers: Customer[] = [];

  sortColumn: string = 'lastName';
  sortDir: 'asc' | 'desc' = 'asc';

  filter = new FormControl('', { nonNullable: true });

  constructor(
    private customerService: CustomerService,
    private router: Router
  ) { }

  ngOnInit() {
    this.loadCustomers();

    this.filter.valueChanges.pipe(
      debounceTime(200),
      distinctUntilChanged()
    ).subscribe(() => this.applyFilter());
  }

  loadCustomers() {
    this.customerService.GetAll().subscribe(customers => {
      this.allCustomers = customers;
      this.applyFilter();
    });
  }

  applyFilter() {
    const term = this.filter.value.toLowerCase().trim();
    if (!term) {
      this.displayCustomers = [...this.allCustomers];
    } else {
      this.displayCustomers = this.allCustomers.filter(c =>
        `${c.firstName} ${c.lastName}`.toLowerCase().includes(term) ||
        (c.email ?? '').toLowerCase().includes(term) ||
        (c.phone ?? '').includes(term)
      );
    }
    this.applySort();
  }

  sort(column: string) {
    if (this.sortColumn === column) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDir = 'asc';
    }
    this.applySort();
  }

  applySort() {
    this.displayCustomers = [...this.displayCustomers].sort((a, b) => {
      let aVal: any, bVal: any;
      switch (this.sortColumn) {
        case 'lastName':  aVal = a.lastName ?? '';    bVal = b.lastName ?? '';    break;
        case 'email':     aVal = a.email ?? '';       bVal = b.email ?? '';       break;
        case 'phone':     aVal = a.phone ?? '';       bVal = b.phone ?? '';       break;
        case 'orderCount': aVal = a.orderCount ?? 0; bVal = b.orderCount ?? 0;   break;
        default:          aVal = a.lastName ?? '';    bVal = b.lastName ?? '';
      }
      const cmp = typeof aVal === 'number'
        ? aVal - bVal
        : aVal.toString().localeCompare(bVal.toString());
      return this.sortDir === 'asc' ? cmp : -cmp;
    });
  }

  openCustomer(id: number | undefined) {
    if (id != null) this.router.navigate(['/customers', id]);
  }

  newCustomer() {
    this.router.navigate(['/customers', 'new']);
  }
}
