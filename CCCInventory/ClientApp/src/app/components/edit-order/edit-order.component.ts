import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl, FormBuilder, Validators, AbstractControl, EmailValidator, FormArray } from '@angular/forms';
import { Order } from '../../models/order';
import { ActivatedRoute } from '@angular/router';
import { OrderService } from '../../services/order.service';

@Component({
  selector: 'app-edit-order-component',
  templateUrl: './edit-order.component.html'
})

export class EditOrderComponent implements OnInit {

  onSubmit() {
    console.log(this.editOrderFormGroup.value);
  }

  constructor(private _formBuilder: FormBuilder, private route: ActivatedRoute, private orderService: OrderService) {
    this.route.queryParams.subscribe(params => {
      this.editOrder(params["orderNumber"]);
    })
  }

  public orderToEdit: Order = new Order();

  isCakeDetailsHeaderShown: boolean = false;
  isCupcakeDetailsHeaderShown: boolean = false;
  isPupcakeDetailsHeaderShown: boolean = false;
  isCookieDetailsHeaderShown: boolean = false;

  editOrderFormGroup!: FormGroup;

  ngOnInit() {
    // initialize the form with default/blank values
    this.editOrderFormGroup = this._formBuilder.group({
      orderNumber: ['', Validators.required],
      orderDate: ['', Validators.required],
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

    // set the order date to today for new orders
    if (!this.orderToEdit.dateOrderPlaced) {
      this.setOrderDate();
    }
  }

  editOrder(orderNumber: number) {
    if (orderNumber) {
      this.orderService
        .GetOrder(orderNumber)
        .subscribe(result => {
          this.orderToEdit = result;
          console.log("orderToEdit:");
          console.log(this.orderToEdit);

          // intialize the form with the order details
          this.initExistingOrder()
        });
    } else {
      this.initNewOrder();
    }
  }
  
  initNewOrder() {
    this.orderToEdit = new Order();
  }

  initExistingOrder() {
    this.editOrderFormGroup = this._formBuilder.group({
      orderNumber: [this.orderToEdit.orderNumber, Validators.required],
      orderDate: [this.orderToEdit.orderDate, Validators.required],
      pickupTime: [this.orderToEdit.pickupTime, Validators.required],
      deliveryLocation: [this.orderToEdit.deliveryLocation],
      custName: [this.orderToEdit.custName, Validators.required],
      custEmail: [this.orderToEdit.custEmail, Validators.required],
      custPhone: [this.orderToEdit.custPhone, Validators.required],
      formFiles: [this.orderToEdit.formFiles],
      details: [this.orderToEdit.details],
      pickupOrDelivery: [this.orderToEdit.pickupOrDelivery],
      secondaryName: [this.orderToEdit.secondaryName],
      secondaryPhone: [this.orderToEdit.secondaryPhone],
      initialContact: [this.orderToEdit.initialContact],
      contractSent: [this.orderToEdit.contractSent],
      dayOfTextSent: [this.orderToEdit.dayOfTextSent],
      confirmationTextSent: [this.orderToEdit.confirmationTextSent],
      cakes: [this.orderToEdit.cakes],
      cupcakes: [this.orderToEdit.cupcakes],
      pupcakes: [this.orderToEdit.pupcakes],
      cookies: [this.orderToEdit.cookies],
      totalCost: [this.orderToEdit.totalCost],
      depositAmount: [this.orderToEdit.depositAmount],
      depositPaymentMethod: [this.orderToEdit.depositPaymentMethod],
      depositDateTime: [this.orderToEdit.depositDateTime],
      finalPaymentMethod: [this.orderToEdit.finalPaymentMethod],
      finalPaymentDateTime: [this.orderToEdit.finalPaymentDateTime],
      dateOrderPlaced: [this.formatDate(this.orderToEdit.dateOrderPlaced!), Validators.required],
      paidInFull: [this.orderToEdit.paidInFull],
      cakeTierInfo: this._formBuilder.array(this.orderToEdit.cakes?.map(cake => this._formBuilder.group({
        tierSize: [cake.tierSize, Validators.required],
        numTierLayers: [cake.numTierLayers, Validators.required],
        cakeShape: [cake.cakeShape, Validators.required],
        cakeFlavor: [cake.cakeFlavor, Validators.required],
        fillingFlavor: [cake.fillingFlavor, Validators.required],
        icingFlavor: [cake.icingFlavor, Validators.required],
        splitTier: [cake.splitTier || false]
      })) || []),
      cupcakeInfo: this._formBuilder.array(this.orderToEdit.cupcakes?.map(cupcake => this._formBuilder.group({
        cupcakeSize: [cupcake.cupcakeSize, Validators.required],
        cupcakeQuantity: [cupcake.cupcakeQuantity, Validators.required],
        cupcakeFlavor: [cupcake.cupcakeFlavor, Validators.required],
        fillingFlavor: [cupcake.fillingFlavor, Validators.required],
        icingFlavor: [cupcake.icingFlavor, Validators.required]
      })) || []),
      pupcakeInfo: this._formBuilder.array(this.orderToEdit.pupcakes?.map(pupcake => this._formBuilder.group({
        pupcakeSize: [pupcake.pupcakeSize, Validators.required],
        pupcakeQuantity: [pupcake.pupcakeQuantity, Validators.required]
      })) || []),
      cookieInfo: this._formBuilder.array(this.orderToEdit.cookies?.map(cookie => this._formBuilder.group({
        cookieType: [cookie.cookieType, Validators.required],
        cookieQuantity: [cookie.cookieQuantity, Validators.required]
      })) || [])
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
  updateOrder(order: Order) { }

  deleteOrder(order: Order) { }

  createOrder(order: Order) { }

  setOrderDate(): void {
    this.orderToEdit.dateOrderPlaced = new Date();
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
