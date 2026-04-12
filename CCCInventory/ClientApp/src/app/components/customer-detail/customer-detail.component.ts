import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { CustomerService } from '../../services/customer.service';
import { Customer, CustomerSearchResult } from '../../models/customer';

@Component({
  selector: 'app-customer-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './customer-detail.component.html'
})
export class CustomerDetailComponent implements OnInit {
  customer: Customer | null = null;
  isNew = false;
  saveSuccess = false;
  saveError = '';

  // Merge state
  showMergePanel = false;
  mergeSearch = new FormControl('', { nonNullable: true });
  mergeResults: CustomerSearchResult[] = [];
  mergeTarget: CustomerSearchResult | null = null;

  form: FormGroup;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private customerService: CustomerService,
    private fb: FormBuilder
  ) {
    this.form = this.fb.group({
      firstName: ['', Validators.required],
      lastName:  ['', Validators.required],
      email:     [''],
      phone:     ['']
    });
  }

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id === 'new') {
      this.isNew = true;
      this.customer = new Customer();
    } else {
      this.customerService.GetById(Number(id)).subscribe(c => {
        this.customer = c;
        this.form.patchValue({
          firstName: c.firstName,
          lastName:  c.lastName,
          email:     c.email,
          phone:     c.phone
        });
      });
    }

    this.mergeSearch.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(q => q.length >= 2 ? this.customerService.Search(q) : [[] as CustomerSearchResult[]])
    ).subscribe(results => {
      // Exclude the current customer from results
      this.mergeResults = (results as CustomerSearchResult[]).filter(
        r => r.customerId !== this.customer?.customerId
      );
    });
  }

  save() {
    if (this.form.invalid) return;
    const val = this.form.value;

    if (this.isNew) {
      const newCustomer: Customer = {
        firstName: val.firstName,
        lastName:  val.lastName,
        email:     val.email || undefined,
        phone:     val.phone || undefined
      };
      this.customerService.Create(newCustomer).subscribe({
        next: id => this.router.navigate(['/customers', id]),
        error: () => this.saveError = 'Failed to create customer.'
      });
    } else {
      const updated: Customer = {
        customerId: this.customer!.customerId,
        firstName:  val.firstName,
        lastName:   val.lastName,
        email:      val.email || undefined,
        phone:      val.phone || undefined
      };
      this.customerService.Update(updated).subscribe({
        next: () => {
          this.customer = { ...this.customer, ...updated };
          this.saveSuccess = true;
          setTimeout(() => this.saveSuccess = false, 3000);
        },
        error: () => this.saveError = 'Failed to save changes.'
      });
    }
  }

  openMergePanel() {
    this.showMergePanel = true;
    this.mergeSearch.setValue('');
    this.mergeResults = [];
    this.mergeTarget = null;
  }

  cancelMerge() {
    this.showMergePanel = false;
    this.mergeTarget = null;
  }

  selectMergeTarget(c: CustomerSearchResult) {
    this.mergeTarget = c;
  }

  confirmMerge() {
    if (!this.mergeTarget || !this.customer?.customerId) return;
    // Keep current customer, delete mergeTarget
    this.customerService.Merge(this.customer.customerId, this.mergeTarget.customerId!).subscribe({
      next: () => {
        this.showMergePanel = false;
        // Reload to reflect updated order count
        this.customerService.GetById(this.customer!.customerId!).subscribe(c => {
          this.customer = c;
        });
      },
      error: () => this.saveError = 'Merge failed.'
    });
  }

  newOrder() {
    const val = this.form.value;
    this.router.navigate(['/edit-order'], {
      queryParams: {
        custName:   `${val.firstName} ${val.lastName}`.trim(),
        custEmail:  val.email || '',
        custPhone:  val.phone || '',
        customerId: this.customer?.customerId
      }
    });
  }

  editOrder(orderNumber: number) {
    this.router.navigate(['/edit-order'], { queryParams: { orderNumber } });
  }

  goBack() {
    this.router.navigate(['/customers']);
  }

  getOrderStatus(order: any): string {
    const now = new Date();
    const orderDate = order.orderDateTime ? new Date(order.orderDateTime) : null;
    if (order.cancelledFlag) return 'Cancelled';
    if (!orderDate || orderDate <= now) return 'Archived';
    if (order.isReadyForPickup) return 'Ready for Pickup';
    return 'Active';
  }

  getStatusClass(order: any): string {
    const status = this.getOrderStatus(order);
    switch (status) {
      case 'Cancelled':        return 'badge bg-danger';
      case 'Archived':         return 'badge bg-secondary';
      case 'Ready for Pickup': return 'badge bg-success';
      default:                 return 'badge bg-primary';
    }
  }
}
