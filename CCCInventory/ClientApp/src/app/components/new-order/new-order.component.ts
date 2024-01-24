import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl, FormBuilder, Validators, AbstractControl, EmailValidator, FormArray } from '@angular/forms';

@Component({
  selector: 'app-new-order-component',
  templateUrl: './new-order.component.html'
})

export class NewOrderComponent implements OnInit {

  onSubmit() {
    console.log(this.newOrderFormGroup.value);
  }

  constructor(private _formBuilder: FormBuilder) { }

  dateOrderPlaced!: string;

  isCakeDetailsHeaderShown: boolean = false;
  isCupcakeDetailsHeaderShown: boolean = false;
  isPupcakeDetailsHeaderShown: boolean = false;
  isCookieDetailsHeaderShown: boolean = false;

  newOrderFormGroup!: FormGroup;
  cupcakeNewOrderFormGroup!: FormGroup;
  pupcakeNewOrderFormGroup!: FormGroup;
  cookieInfoFormGroup!: FormGroup;

  ngOnInit() {
    this.newOrderFormGroup = this._formBuilder.group({
      cakeTierInfo: this._formBuilder.array([]),
    });

    this.cupcakeNewOrderFormGroup = this._formBuilder.group({
      cupcakeInfo: this._formBuilder.array([]),
    });

    this.pupcakeNewOrderFormGroup = this._formBuilder.group({
      pupcakeInfo: this._formBuilder.array([]),
    });

    this.cookieInfoFormGroup = this._formBuilder.group({
      cookieInfo: this._formBuilder.array([]),
    });

    // set the order date to today
    this.setOrderDate();
  }

  setOrderDate(): void {
    const today: Date = new Date();
    const month: string | number = today.getMonth() + 1; // Months are zero-based
    const day: string | number = today.getDate();
    const year: number = today.getFullYear();

    // Format the date as 'yyyy-MM-dd' (required by the date input)
    this.dateOrderPlaced = `${year}-${month < 10 ? '0' : ''}${month}-${day < 10 ? '0' : ''}${day}`;
  }

  // cake
  addCakeRow() {
    const cakeInfoFormArray = this.newOrderFormGroup.get('cakeTierInfo') as FormArray;
    cakeInfoFormArray.push(
      this._formBuilder.group({
        tierSize: ["", Validators.required],
        numTierLayers: ["", Validators.required],
        cakeShape: ["", Validators.required],
        cakeFlavor: ["", Validators.required],
        fillingFlavor: ["", Validators.required],
        icingFlavor: ["", Validators.required]
      })
    );

    if (cakeInfoFormArray.length >= 1) {
      this.isCakeDetailsHeaderShown = true;
    }
  }

  deleteCakeRow(i: number) {
    const cakeInfoFormArray = this.newOrderFormGroup.get('cakeTierInfo') as FormArray;
    cakeInfoFormArray.removeAt(i);
    if (cakeInfoFormArray.length === 0) {
      this.isCakeDetailsHeaderShown = false;
    }
  }

  getCakeControls() {
    return (this.newOrderFormGroup.get('cakeTierInfo') as FormArray).controls;
  }

  // cupcake
  addCupcakeRow() {
    const cupcakeInfoFormArray = this.cupcakeNewOrderFormGroup.get('cupcakeInfo') as FormArray;
    cupcakeInfoFormArray.push(
      this._formBuilder.group({
        cupcakeSize: ["", Validators.required],
        cupcakeQuantity: ["", Validators.required],
        cupcakeFlavor: ["", Validators.required],
        fillingFlavor: ["", Validators.required],
        icingFlavor: ["", Validators.required]
      })
    );

    if (cupcakeInfoFormArray.length >= 1) {
      this.isCupcakeDetailsHeaderShown = true;
    }
  }

  deleteCupcakeRow(i: number) {
    const cupcakeInfoFormArray = this.cupcakeNewOrderFormGroup.get('cupcakeInfo') as FormArray;
    cupcakeInfoFormArray.removeAt(i);
    if (cupcakeInfoFormArray.length === 0) {
      this.isCupcakeDetailsHeaderShown = false;
    }
  }

  getCupcakeControls() {
    return (this.cupcakeNewOrderFormGroup.get('cupcakeInfo') as FormArray).controls;
  }

  // pupcake
  addPupcakeRow() {
    const pupcakeInfoFormArray = this.pupcakeNewOrderFormGroup.get('pupcakeInfo') as FormArray;
    pupcakeInfoFormArray.push(
      this._formBuilder.group({
        pupcakeSize: ["", Validators.required],
        cookieQuantity: ["", Validators.required],
        pupcakeQuantity: ["", Validators.required]
      })
    );

    if (pupcakeInfoFormArray.length >= 1) {
      this.isPupcakeDetailsHeaderShown = true;
    }
  }

  deletePupcakeRow(i: number) {
    const pupcakeInfoFormArray = this.pupcakeNewOrderFormGroup.get('pupcakeInfo') as FormArray;
    pupcakeInfoFormArray.removeAt(i);
    if (pupcakeInfoFormArray.length === 0) {
      this.isPupcakeDetailsHeaderShown = false;
    }
  }

  getPupcakeControls() {
    return (this.pupcakeNewOrderFormGroup.get('pupcakeInfo') as FormArray).controls;
  }

  // cookie
  addCookieRow() {
    const cookieInfoFormArray = this.cookieInfoFormGroup.get('cookieInfo') as FormArray;
    cookieInfoFormArray.push(
      this._formBuilder.group({
        cookieType: ["", Validators.required],
        cookieQuantity: ["", Validators.required]
      })
    );

    if (cookieInfoFormArray.length >= 1) {
      this.isCookieDetailsHeaderShown = true;
    }
  }

  deleteCookieRow(i: number) {
    const cookieInfoFormArray = this.cookieInfoFormGroup.get('cookieInfo') as FormArray;
    cookieInfoFormArray.removeAt(i);
    if (cookieInfoFormArray.length === 0) {
      this.isCookieDetailsHeaderShown = false;
    }
  }

  getCookieControls() {
    return (this.cookieInfoFormGroup.get('cookieInfo') as FormArray).controls;
  }
}
