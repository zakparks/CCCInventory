import { Component, OnInit, EventEmitter, Output, Renderer2, ElementRef } from '@angular/core';
import { FormGroup, FormControl, FormBuilder, Validators, AbstractControl, EmailValidator, FormArray } from '@angular/forms';
import { Order } from '../../models/order';
import { ActivatedRoute, NavigationExtras, Router } from '@angular/router';
import { OrderService } from '../../services/order.service';
import { switchMap } from 'rxjs/operators';

@Component({
  selector: 'app-edit-order-component',
  templateUrl: './edit-order.component.html'
})

export class EditOrderComponent implements OnInit {
  @Output() orderUpdated = new EventEmitter<Order>();
  
  constructor(private _formBuilder: FormBuilder, private route: ActivatedRoute, private orderService: OrderService, private router: Router, private renderer: Renderer2, private el: ElementRef) { }

  // used to decide which CRUD buttons to show
  public createOrUpdate: string = "Create";

  // handle the form submit depending on which button was clicked
  public action: string = 'none';

  // main order object
  public orderToEdit: Order = new Order();

  // toggles for showing/hiding details headers
  isCakeDetailsHeaderShown: boolean = false;
  isCupcakeDetailsHeaderShown: boolean = false;
  isPupcakeDetailsHeaderShown: boolean = false;
  isCookieDetailsHeaderShown: boolean = false;

  // main form group
  editOrderFormGroup: FormGroup = this._formBuilder.group({
    orderNumber: ['', Validators.required],
    orderDateTime: ['', Validators.required],
    deliveryLocation: [''],
    custName: ['', Validators.required],
    custEmail: [''],
    custPhone: ['', Validators.required],
    formFiles: [''],
    details: [''],
    pickupOrDelivery: [''],
    secondaryName: [''],
    secondaryPhone: [''],
    initialContact: [''],
    contractSent: [''],
    dayOfTextSent: [''],
    confirmationTextSent: [''],
    cakes: [''],
    cupcakes: [''],
    pupcakes: [''],
    cookies: [''],
    totalCost: [''],
    depositAmount: [''],
    depositPaymentMethod: [''],
    depositDateTime: [''],
    finalPaymentMethod: [''],
    finalPaymentDateTime: [''],
    dateOrderPlaced: ['', Validators.required],
    paidInFull: [''],
    cakeTierInfo: this._formBuilder.array([]),
    cupcakeInfo: this._formBuilder.array([]),
    pupcakeInfo: this._formBuilder.array([]),
    cookieInfo: this._formBuilder.array([])
  });

  // initialize the order form, either based on the order number passed in the query params or a new order
  // use a switchmap to ensure orderToEdit is set before initializing the form
  ngOnInit() {
    this.route.queryParams.pipe(
      switchMap(params => {
        if (params["orderNumber"]) {
          this.createOrUpdate = "Update";
          return this.orderService.GetOrder(params["orderNumber"]);
        } else {
          this.createOrUpdate = "Create";
          return this.orderService.GetNewOrderNumber();
        }
      })
    ).subscribe(result => {
      if (typeof result === 'number') {
        this.orderToEdit = {
          ...this.orderToEdit,
          orderNumber: result,
          dateOrderPlaced: new Date()
        };
      } else {
        this.orderToEdit = result;
      }
      this.initOrderFormGroup();
    });
  }

  initOrderFormGroup() {
    this.editOrderFormGroup.patchValue({
      orderNumber: this.orderToEdit.orderNumber,
      orderDateTime: this.orderToEdit.orderDateTime,
      deliveryLocation: this.orderToEdit.deliveryLocation,
      custName: this.orderToEdit.custName,
      custEmail: this.orderToEdit.custEmail,
      custPhone: this.orderToEdit.custPhone,
      formFiles: this.orderToEdit.formFiles,
      details: this.orderToEdit.details,
      pickupOrDelivery: this.orderToEdit.pickupOrDelivery,
      secondaryName: this.orderToEdit.secondaryName,
      secondaryPhone: this.orderToEdit.secondaryPhone,
      initialContact: this.orderToEdit.initialContact,
      contractSent: this.orderToEdit.contractSent,
      dayOfTextSent: this.orderToEdit.dayOfTextSent,
      confirmationTextSent: this.orderToEdit.confirmationTextSent,
      totalCost: this.orderToEdit.totalCost,
      depositAmount: this.orderToEdit.depositAmount,
      depositPaymentMethod: this.orderToEdit.depositPaymentMethod,
      depositDateTime: this.orderToEdit.depositDateTime,
      finalPaymentMethod: this.orderToEdit.finalPaymentMethod,
      finalPaymentDateTime: this.orderToEdit.finalPaymentDateTime,
      dateOrderPlaced: this.formatDate(this.orderToEdit.dateOrderPlaced!),
      paidInFull: this.orderToEdit.paidInFull
    });

    const cakeTierInfo = this.editOrderFormGroup.get('cakeTierInfo') as FormArray;
    this.orderToEdit.cakes?.forEach(cake => {
      cakeTierInfo.push(this._formBuilder.group({
        tierSize: [cake.tierSize],
        numTierLayers: [cake.numTierLayers],
        cakeShape: [cake.cakeShape],
        cakeFlavor: [cake.cakeFlavor],
        fillingFlavor: [cake.fillingFlavor],
        icingFlavor: [cake.icingFlavor],
        splitTier: [cake.splitTier]
      }));
    });

    const cupcakeInfo = this.editOrderFormGroup.get('cupcakeInfo') as FormArray;
    this.orderToEdit.cupcakes?.forEach(cupcake => {
      cupcakeInfo.push(this._formBuilder.group({
        cupcakeSize: [cupcake.cupcakeSize],
        cupcakeQuantity: [cupcake.cupcakeQuantity],
        cupcakeFlavor: [cupcake.cupcakeFlavor],
        fillingFlavor: [cupcake.fillingFlavor],
        icingFlavor: [cupcake.icingFlavor]
      }));
    });

    const pupcakeInfo = this.editOrderFormGroup.get('pupcakeInfo') as FormArray;
    this.orderToEdit.pupcakes?.forEach(pupcake => {
      pupcakeInfo.push(this._formBuilder.group({
        pupcakeSize: [pupcake.pupcakeSize],
        pupcakeQuantity: [pupcake.pupcakeQuantity]
      }));
    });

    const cookieInfo = this.editOrderFormGroup.get('cookieInfo') as FormArray;
    this.orderToEdit.cookies?.forEach(cookie => {
      cookieInfo.push(this._formBuilder.group({
        cookieType: [cookie.cookieType],
        cookieQuantity: [cookie.cookieQuantity]
      }));
    });
  }

  // format date to yyyy-mm-dd
  formatDate(date: Date) {
    var d = new Date(date);
    var month = '' + (d.getMonth() + 1);
    var day = '' + d.getDate();
    var year = d.getFullYear();

    if (month.length < 2)
      month = '0' + month;
    if (day.length < 2)
      day = '0' + day;

    return [year, month, day].join('-');
  }

  // format input as telephone number as it is typed
  onTelInput(event: Event, controlName: string) {
    const input = event.target as HTMLInputElement;
    let val = input.value.replace(/\D/g, ''); // remove all non-numerics 
    if (val.length < 9 && val !== null) {
      let finalVal = val!.match(/.{1,3}/g)!.join('-'); // add dash (-) after every 3rd char.
      this.editOrderFormGroup.controls[controlName].setValue(finalVal);
    }
  }

  // get the numeric value of the input and store it in the hidden input
  onTotalCostInput(event: Event, controlName: string) {
    const input = event.target as HTMLInputElement;
    const strippedValue = input.value.replace(/[^0-9.]/g, '');
    this.editOrderFormGroup.controls[controlName].setValue(strippedValue);
  }

  // update button
  onUpdateOrderClicked() {
    this.action = 'create';
  }

  updateOrder() {
    if (this.editOrderFormGroup.valid) {
      // get the formarray values and put them into the order object
      const { cakeTierInfo, cupcakeInfo, pupcakeInfo, cookieInfo, ...formValue } = this.editOrderFormGroup.value;

      this.orderToEdit = {
        ...formValue,
        cakes: cakeTierInfo,
        cupcakes: cupcakeInfo,
        pupcakes: pupcakeInfo,
        cookies: cookieInfo
      };

      this.orderService
        .UpdateOrder(this.orderToEdit)
        .subscribe((result: number) => {
          if (result === this.orderToEdit.orderNumber) {
            this.orderUpdated.emit(this.orderToEdit);
            console.log(`Order ${this.orderToEdit.orderNumber} updated.`);
            this.onSubmitSuccess(`Order ${this.orderToEdit.orderNumber} updated.`);
          }
        },
          error => {
            const errorMessage = Object.values(error.error.errors).join('\n');
            this.toastFailure(errorMessage);
        }
      );
    } else {
      const invalidControlsString = this.getInvalidControlLabels();
      this.toastFailure(`Please check the form for missing information: ${invalidControlsString}`);
    }
  }

  // delete button
  onDeleteOrderClicked() {
    this.action = 'delete';
  }

  deleteOrder() {
    this.orderToEdit.deleteFlag = true;
    this.orderService
      .UpdateOrder(this.orderToEdit)
      .subscribe((result: number) => {
        if (result === this.orderToEdit.orderNumber) {
          console.log(`Order ${this.orderToEdit.orderNumber} marked as deleted.`);
          this.onSubmitSuccess(`Order ${this.orderToEdit.orderNumber} marked as deleted.`);
        }
      },
        error => {
          const errorMessage = error.error ? error.error.message : error.message
          this.toastFailure(errorMessage);
      }
    );
  }

  // create button
  onCreateOrderClicked() {
    this.action = 'create';
  }

  createOrder() {
    if (this.editOrderFormGroup.valid) {
      this.orderToEdit.orderNumber;
      this.orderService
        .AddOrder(this.orderToEdit)
        .subscribe((result: number) => {
          if (result === this.orderToEdit.orderNumber) {
            this.orderUpdated.emit(this.orderToEdit);
            console.log(`Order ${this.orderToEdit.orderNumber} created.`);
            this.onSubmitSuccess(`Order ${this.orderToEdit.orderNumber} created.`);
          }
        },
          error => {
            const errorMessage = error.error ? error.error.message : error.message
            this.toastFailure(errorMessage);
          }
        );
    } else {
      const invalidControlsString = this.getInvalidControlLabels();
      this.toastFailure(`Please check the form for missing information: ${invalidControlsString}`);
    }
  }

  // handle form submission
  onSubmit() {
    switch (this.action) {
      case 'create':
        this.createOrder();
        break;
      case 'update':
        this.updateOrder();
        break;
      case 'delete':
        this.deleteOrder();
        break;
    }
  }

  // finalize form submission
  onSubmitSuccess(message: string) {
    console.log(this.editOrderFormGroup.value);
    this.toastSuccess(message)

    // reload the page in order to reset the form
    let navDetails: NavigationExtras = {
      queryParams: {
        orderNumber: this.orderToEdit.orderNumber
      }
    };

    this.router.navigate(["edit-order"], navDetails)
  }

  // checks control validity
  isInvalid(controlName: string): boolean {
    // action is defaulted to none on page load. If its anything else from a button click, then check for invalidity
    if (this.action !== 'none') {
      const control = this.editOrderFormGroup.controls[controlName];
      return control.invalid;
    }

    return false;
  }

  // get invalid controls
  getInvalidControlLabels(): string {
    const invalidControlLabels: string[] = [];
    Object.keys(this.editOrderFormGroup.controls).forEach(controlName => {
      const control = this.editOrderFormGroup.controls[controlName];
      if (control.invalid) {
        // get the labes of the invalid controls in the form array
        if (control instanceof FormArray) {
          control.controls.forEach((groupControl: AbstractControl, index: number) => {
            const group = groupControl as FormGroup;
            Object.keys(group.controls).forEach(groupControlName => {
              const groupControl = group.controls[groupControlName];
              if (groupControl.invalid) {
                const label = document.querySelector(`label[for="#${groupControlName}${index}"]`);
                if (label) {
                  invalidControlLabels.push(`${label.textContent?.trim() || groupControlName} (row ${index + 1})`);
                } else {
                  invalidControlLabels.push(`${controlName} ${groupControlName} ${index + 1}`);
                }
              }
            });
          });
        // get the labels of other invalid controls
        } else {
          const label = document.querySelector(`label[for="${controlName}"]`);
          if (label) {
            invalidControlLabels.push(label.textContent?.trim() || controlName);
          } else {
            invalidControlLabels.push(controlName);
          }
        }
      }
    });
    return invalidControlLabels.join(', ');
  }

  // toast of success :D
  toastSuccess(message: string) {
    const toastElement = this.el.nativeElement.querySelector('#toastSuccess');
    if (message) {
      const toastBody = this.el.nativeElement.querySelector('#toastSuccess .toast-body');
      this.renderer.setProperty(toastBody, 'textContent', message);
    }
    this.renderer.addClass(toastElement, 'show');
    setTimeout(() => {
      this.renderer.removeClass(toastElement, 'show');
    }, 3000); // Hide the toast after 3 seconds
  }

  // toast of failure D:
  toastFailure(errorMessage: string) {
    const toastElement = this.el.nativeElement.querySelector('#toastFailure');
    const toastBody = this.el.nativeElement.querySelector('#toastFailure .toast-body');
    this.renderer.setProperty(toastBody, 'textContent', errorMessage);
    this.renderer.addClass(toastElement, 'show');
    setTimeout(() => {
      this.renderer.removeClass(toastElement, 'show');
    }, 15000); // Hide the toast after 15 seconds
  }

  // add/delete/get rows for Cake, Cupcake, Pupcake, and Cookie sections
  addRow(formGroupName: string, formGroup: FormGroup) {
    const formArray = this.editOrderFormGroup.get(formGroupName) as FormArray;
    formArray.push(this._formBuilder.group(formGroup.controls));

    // Update the corresponding header shown variable
    switch (formGroupName) {
      case 'cakeTierInfo':
        this.isCakeDetailsHeaderShown = true;
        break;
      case 'cupcakeInfo':
        this.isCupcakeDetailsHeaderShown = true;
        break;
      case 'pupcakeInfo':
        this.isPupcakeDetailsHeaderShown = true;
        break;
      case 'cookieInfo':
        this.isCookieDetailsHeaderShown = true;
        break;
    }
  }

  deleteRow(formGroupName: string, index: number) {
    const formArray = this.editOrderFormGroup.get(formGroupName) as FormArray;
    formArray.removeAt(index);

    // Update the corresponding header shown variable
    if (formArray.length === 0) {
      switch (formGroupName) {
        case 'cakeTierInfo':
          this.isCakeDetailsHeaderShown = false;
          break;
        case 'cupcakeInfo':
          this.isCupcakeDetailsHeaderShown = false;
          break;
        case 'pupcakeInfo':
          this.isPupcakeDetailsHeaderShown = false;
          break;
        case 'cookieInfo':
          this.isCookieDetailsHeaderShown = false;
          break;
      }
    }
  }

  // get form controls from a form group
  getControls(formGroupName: string) {
    return (this.editOrderFormGroup.get(formGroupName) as FormArray).controls;
  }

  // get blank form controls when adding a new row
  getBlankCakeFormControls() {
    return this._formBuilder.group({
      tierSize: ['', Validators.required],
      numTierLayers: ['', Validators.required],
      cakeShape: ['', Validators.required],
      cakeFlavor: ['', Validators.required],
      fillingFlavor: ['', Validators.required],
      icingFlavor: ['', Validators.required],
      splitTier: [false]
    });
  }

  getBlankCupcakeFormControls() {
    return this._formBuilder.group({
      cupcakeSize: ['', Validators.required],
      cupcakeQuantity: ['', Validators.required],
      cupcakeFlavor: ['', Validators.required],
      fillingFlavor: ['', Validators.required],
      icingFlavor: ['', Validators.required]
    });
  }

  getBlankPupcakeFormControls() {
    return this._formBuilder.group({
      pupcakeSize: ['', Validators.required],
      pupcakeQuantity: ['', Validators.required]
    });
  }

  getBlankCookieFormControls() {
    return this._formBuilder.group({
      cookieType: ['', Validators.required],
      cookieQuantity: ['', Validators.required]
    });
  }
}
