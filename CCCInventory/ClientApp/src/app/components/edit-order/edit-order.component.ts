import { Component, OnInit, Renderer2, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormGroup, FormBuilder, Validators, AbstractControl, FormArray } from '@angular/forms';
import { RouterModule, ActivatedRoute, NavigationExtras, Router } from '@angular/router';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { Order } from '../../models/order';
import { OrderAttachment } from '../../models/order-attachment';
import { SignatureCupcake } from '../../models/signature-cupcake';
import { OrderService } from '../../services/order.service';
import { AttachmentService } from '../../services/attachment.service';
import { OptionService } from '../../services/option.service';
import { SignatureCupcakeService } from '../../services/signature-cupcake.service';
import { switchMap } from 'rxjs/operators';

@Component({
  selector: 'app-edit-order-component',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, NgbModule],
  templateUrl: './edit-order.component.html'
})

export class EditOrderComponent implements OnInit {
  attachments: OrderAttachment[] = [];

  // Option lists loaded from API
  cakeTierSizes: string[] = [];
  cakeShapes: string[] = [];
  flavors: string[] = [];
  fillingFlavors: string[] = [];
  icingFlavors: string[] = [];
  cupcakeSizes: string[] = [];
  cookieTypes: string[] = [];
  signatures: SignatureCupcake[] = [];

  constructor(
    private _formBuilder: FormBuilder,
    private route: ActivatedRoute,
    private orderService: OrderService,
    public attachmentService: AttachmentService,
    private router: Router,
    private renderer: Renderer2,
    private el: ElementRef,
    private optionService: OptionService,
    private sigService: SignatureCupcakeService
  ) { }

  // Active-only signatures for new orders; all for existing orders
  get signaturesForForm(): SignatureCupcake[] {
    return this.createOrUpdate === 'Update' ? this.signatures : this.signatures.filter(s => s.isActive);
  }

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

  ngOnInit() {
    this.optionService.getAll().subscribe(items => {
      this.cakeTierSizes = items.filter(i => i.category === 'CakeTierSize' && i.isActive).map(i => i.value);
      this.cakeShapes    = items.filter(i => i.category === 'CakeShape'    && i.isActive).map(i => i.value);
      this.flavors       = items.filter(i => i.category === 'Flavor'       && i.isActive).map(i => i.value);
      this.fillingFlavors= items.filter(i => i.category === 'FillingFlavor'&& i.isActive).map(i => i.value);
      this.icingFlavors  = items.filter(i => i.category === 'IcingFlavor'  && i.isActive).map(i => i.value);
      this.cupcakeSizes  = items.filter(i => i.category === 'CupcakeSize'  && i.isActive).map(i => i.value);
      this.cookieTypes   = items.filter(i => i.category === 'CookieType'   && i.isActive).map(i => i.value);
    });
    this.sigService.getAll().subscribe(sigs => {
      this.signatures = sigs;
    });

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
        this.loadAttachments(result.orderNumber!);
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
        icingFlavor: [cupcake.icingFlavor],
        signatureName: [cupcake.signatureName]
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

  onUpdateOrderClicked() {
    this.action = 'update';
  }

  updateOrder() {
    if (this.editOrderFormGroup.valid) {
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

  onDeleteOrderClicked() {
    this.action = 'delete';
  }

  deleteOrder() {
    this.orderToEdit.deleteFlag = true;
    this.orderService
      .UpdateOrder(this.orderToEdit)
      .subscribe((result: number) => {
        if (result === this.orderToEdit.orderNumber) {
          this.onSubmitSuccess(`Order ${this.orderToEdit.orderNumber} marked as deleted.`);
        }
      },
        error => {
          const errorMessage = error.error ? error.error.message : error.message;
          this.toastFailure(errorMessage);
      }
    );
  }

  restoreOrder() {
    this.orderService.RestoreOrder(this.orderToEdit.orderNumber!).subscribe(() => {
      this.orderToEdit = { ...this.orderToEdit, deleteFlag: false };
      this.toastSuccess(`Order ${this.orderToEdit.orderNumber} restored.`);
    });
  }

  onCreateOrderClicked() {
    this.action = 'create';
  }

  createOrder() {
    if (this.editOrderFormGroup.valid) {
      // Assemble form values the same way updateOrder does
      const { cakeTierInfo, cupcakeInfo, pupcakeInfo, cookieInfo, ...formValue } = this.editOrderFormGroup.value;
      this.orderToEdit = {
        ...formValue,
        cakes: cakeTierInfo,
        cupcakes: cupcakeInfo,
        pupcakes: pupcakeInfo,
        cookies: cookieInfo
      };

      this.orderService
        .AddOrder(this.orderToEdit)
        .subscribe((result: number) => {
          // Use the server-assigned order number, not the pre-filled estimate
          this.orderToEdit = { ...this.orderToEdit, orderNumber: result };
          this.onSubmitSuccess(`Order ${result} created.`);
        },
          error => {
            const errorMessage = error.error ? error.error.message : error.message;
            this.toastFailure(errorMessage);
          }
        );
    } else {
      const invalidControlsString = this.getInvalidControlLabels();
      this.toastFailure(`Please check the form for missing information: ${invalidControlsString}`);
    }
  }

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

  onSubmitSuccess(message: string) {
    this.toastSuccess(message);

    let navDetails: NavigationExtras = {
      queryParams: {
        orderNumber: this.orderToEdit.orderNumber
      }
    };

    this.router.navigate(["edit-order"], navDetails);
  }

  isInvalid(controlName: string): boolean {
    if (this.action !== 'none') {
      const control = this.editOrderFormGroup.controls[controlName];
      return control.invalid;
    }
    return false;
  }

  getInvalidControlLabels(): string {
    const invalidControlLabels: string[] = [];
    Object.keys(this.editOrderFormGroup.controls).forEach(controlName => {
      const control = this.editOrderFormGroup.controls[controlName];
      if (control.invalid) {
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

  toastSuccess(message: string) {
    const toastElement = this.el.nativeElement.querySelector('#toastSuccess');
    if (message) {
      const toastBody = this.el.nativeElement.querySelector('#toastSuccess .toast-body');
      this.renderer.setProperty(toastBody, 'textContent', message);
    }
    this.renderer.addClass(toastElement, 'show');
    setTimeout(() => {
      this.renderer.removeClass(toastElement, 'show');
    }, 3000);
  }

  toastFailure(errorMessage: string) {
    const toastElement = this.el.nativeElement.querySelector('#toastFailure');
    const toastBody = this.el.nativeElement.querySelector('#toastFailure .toast-body');
    this.renderer.setProperty(toastBody, 'textContent', errorMessage);
    this.renderer.addClass(toastElement, 'show');
    setTimeout(() => {
      this.renderer.removeClass(toastElement, 'show');
    }, 15000);
  }

  loadAttachments(orderNumber: number) {
    this.attachmentService.GetAttachments(orderNumber).subscribe(attachments => {
      this.attachments = attachments;
    });
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const files = Array.from(input.files);
    this.attachmentService.UploadFiles(this.orderToEdit.orderNumber!, files).subscribe({
      next: newAttachments => {
        this.attachments = [...this.attachments, ...newAttachments];
        input.value = ''; // reset so same file can be re-uploaded if needed
        this.toastSuccess(`${newAttachments.length} file(s) uploaded.`);
      },
      error: err => {
        const msg = err.error ?? err.message ?? 'Upload failed.';
        this.toastFailure(typeof msg === 'string' ? msg : JSON.stringify(msg));
      }
    });
  }

  deleteAttachment(id: number) {
    this.attachmentService.DeleteAttachment(id).subscribe({
      next: () => {
        this.attachments = this.attachments.filter(a => a.id !== id);
      },
      error: err => {
        const msg = err.error ?? err.message ?? 'Delete failed.';
        this.toastFailure(typeof msg === 'string' ? msg : JSON.stringify(msg));
      }
    });
  }

  addRow(formGroupName: string, formGroup: FormGroup) {
    const formArray = this.editOrderFormGroup.get(formGroupName) as FormArray;
    formArray.push(this._formBuilder.group(formGroup.controls));

    switch (formGroupName) {
      case 'cakeTierInfo': this.isCakeDetailsHeaderShown = true; break;
      case 'cupcakeInfo': this.isCupcakeDetailsHeaderShown = true; break;
      case 'pupcakeInfo': this.isPupcakeDetailsHeaderShown = true; break;
      case 'cookieInfo': this.isCookieDetailsHeaderShown = true; break;
    }
  }

  deleteRow(formGroupName: string, index: number) {
    const formArray = this.editOrderFormGroup.get(formGroupName) as FormArray;
    formArray.removeAt(index);

    if (formArray.length === 0) {
      switch (formGroupName) {
        case 'cakeTierInfo': this.isCakeDetailsHeaderShown = false; break;
        case 'cupcakeInfo': this.isCupcakeDetailsHeaderShown = false; break;
        case 'pupcakeInfo': this.isPupcakeDetailsHeaderShown = false; break;
        case 'cookieInfo': this.isCookieDetailsHeaderShown = false; break;
      }
    }
  }

  getControls(formGroupName: string) {
    return (this.editOrderFormGroup.get(formGroupName) as FormArray).controls;
  }

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
      icingFlavor: ['', Validators.required],
      signatureName: ['']
    });
  }

  addSignatureCupcakeRow() {
    const group = this._formBuilder.group({
      cupcakeSize: ['Regular', Validators.required],
      cupcakeQuantity: ['', Validators.required],
      cupcakeFlavor: ['', Validators.required],
      fillingFlavor: ['', Validators.required],
      icingFlavor: ['', Validators.required],
      signatureName: ['']
    });
    const arr = this.editOrderFormGroup.get('cupcakeInfo') as FormArray;
    arr.push(group);
    this.isCupcakeDetailsHeaderShown = true;
  }

  onSignatureSelected(event: Event, index: number) {
    const name = (event.target as HTMLInputElement).value;
    const sig = this.signatures.find(s => s.name === name);
    if (sig) {
      const arr = this.editOrderFormGroup.get('cupcakeInfo') as FormArray;
      const row = arr.at(index) as FormGroup;
      row.patchValue({
        cupcakeFlavor: sig.cupcakeFlavor,
        fillingFlavor: sig.fillingFlavor,
        icingFlavor: sig.icingFlavor,
        signatureName: sig.name
      });
    }
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
