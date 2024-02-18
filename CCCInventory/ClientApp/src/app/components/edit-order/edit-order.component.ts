import { Component, OnInit, EventEmitter, Output } from '@angular/core';
import { FormGroup, FormControl, FormBuilder, Validators, AbstractControl, EmailValidator, FormArray } from '@angular/forms';
import { Order } from '../../models/order';
import { ActivatedRoute } from '@angular/router';
import { OrderService } from '../../services/order.service';
import { switchMap } from 'rxjs/operators';

@Component({
  selector: 'app-edit-order-component',
  templateUrl: './edit-order.component.html'
})

export class EditOrderComponent implements OnInit {
  @Output() orderUpdated = new EventEmitter<Order>();

  onSubmit() {
    console.log(this.editOrderFormGroup.value);
  }

  constructor(private _formBuilder: FormBuilder, private route: ActivatedRoute, private orderService: OrderService) { }

  // used to decide which CRUD buttons to show
  public createOrUpdate: string = "Create";

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
    pickupTime: ['', Validators.required],
    deliveryLocation: [''],
    custName: ['', Validators.required],
    custEmail: ['', Validators.required],
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

  // ng crud buttons
  updateOrder() {
    const { cakeTierInfo, cupcakeInfo, pupcakeInfo, cookieInfo, ...formValue } = this.editOrderFormGroup.value;

    this.orderToEdit = {
      ...formValue,
      cakes: cakeTierInfo,
      cupcakes: cupcakeInfo,
      pupcakes: pupcakeInfo,
      cookies: cookieInfo
    };

    console.log("order to update:");
    console.log(this.orderToEdit);

    this.orderService
      .UpdateOrder(this.orderToEdit)
      .subscribe(result => {
        this.orderUpdated.emit(this.orderToEdit);
        console.log(`Order ${this.orderToEdit.orderNumber} updated.`);
      });
  }

  deleteOrder() {
    this.orderToEdit.deleteFlag = true;
    this.orderService
      .UpdateOrder(this.orderToEdit)
      .subscribe(result => {
        console.log(`Order ${this.orderToEdit.orderNumber} marked as deleted.`);
      });
  }

  createOrder() {
    this.orderService
    .AddOrder(this.orderToEdit)
      .subscribe(result => {
        this.orderUpdated.emit(this.orderToEdit);
        console.log(`Order ${this.orderToEdit.orderNumber} created.`);
      });
  }

  // add/remove/get rows for Cake, Cupcake, Pupcake, and Cookie
  addRow(formGroupName: string, formGroup: FormGroup) {
    const formArray = this.editOrderFormGroup.get(formGroupName) as FormArray;
    formArray.push(this._formBuilder.group(formGroup.controls));

    if (formArray.length >= 1) {
      const propertyName = `is${this.capitalize(formGroupName)}DetailsHeaderShown`;
      (this as any)[propertyName] = true;
    }
  }

  deleteRow(formGroupName: string, index: number) {
    const formArray = this.editOrderFormGroup.get(formGroupName) as FormArray;
    formArray.removeAt(index);

    if (formArray.length === 0) {
      const propertyName = `is${this.capitalize(formGroupName)}DetailsHeaderShown`;
      (this as any)[propertyName] = false;
    }
  }

  getControls(formGroupName: string) {
    return (this.editOrderFormGroup.get(formGroupName) as FormArray).controls;
  }

  capitalize(str: string) {
    return str.charAt(0).toUpperCase() + str.slice(1);
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
