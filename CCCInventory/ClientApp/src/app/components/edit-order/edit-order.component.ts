import { Component, OnInit, OnDestroy, Renderer2, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormGroup, FormBuilder, Validators, AbstractControl, FormArray } from '@angular/forms';
import { RouterModule, ActivatedRoute, NavigationExtras, Router } from '@angular/router';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { Subject } from 'rxjs';
import { debounceTime, filter, takeUntil, switchMap } from 'rxjs/operators';
import { Order } from '../../models/order';
import { OrderAttachment } from '../../models/order-attachment';
import { SignatureCupcake } from '../../models/signature-cupcake';
import { OrderService } from '../../services/order.service';
import { AttachmentService } from '../../services/attachment.service';
import { OptionService } from '../../services/option.service';
import { SignatureCupcakeService } from '../../services/signature-cupcake.service';

@Component({
  selector: 'app-edit-order-component',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterModule, NgbModule],
  templateUrl: './edit-order.component.html'
})
export class EditOrderComponent implements OnInit, OnDestroy {
  attachments: OrderAttachment[] = [];

  // Option lists loaded from API
  cakeTierSizes: string[] = [];
  cakeShapes: string[] = [];
  flavors: string[] = [];
  fillingFlavors: string[] = [];
  icingFlavors: string[] = [];
  cupcakeSizes: string[] = [];
  cookieTypes: string[] = [];
  cookieSizes: string[] = [];
  signatures: SignatureCupcake[] = [];

  // Archive modal state
  showArchiveModal: boolean = false;
  archiveCancellationReason: string = '';

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
  isOtherDetailsHeaderShown: boolean = false;

  private destroy$ = new Subject<void>();

  // main form group
  editOrderFormGroup: FormGroup = this._formBuilder.group({
    orderNumber: ['', Validators.required],
    orderDate: ['', Validators.required],
    orderTime: [''],
    deliveryLocation: [''],
    custName: ['', Validators.required],
    custEmail: [''],
    custPhone: ['', Validators.required],
    details: [''],
    orderType: [''],
    title: [''],
    secondaryName: [''],
    secondaryPhone: [''],
    initialContact: [''],
    contractSent: [''],
    dayOfTextSent: [''],
    confirmationTextSent: [''],
    isReadyForPickup: [false],
    totalCost: [''],
    depositAmount: [''],
    depositPaymentMethod: [''],
    depositDateTime: [''],
    finalPaymentMethod: [''],
    finalPaymentDateTime: [''],
    dateOrderPlaced: ['', Validators.required],
    paidInFull: [''],
    labor: [''],
    flavorUpgrade: [''],
    lookbookPrice: [''],
    cakeTierInfo: this._formBuilder.array([]),
    cupcakeInfo: this._formBuilder.array([]),
    pupcakeInfo: this._formBuilder.array([]),
    cookieInfo: this._formBuilder.array([]),
    otherItemInfo: this._formBuilder.array([])
  });

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

  ngOnInit() {
    this.optionService.getAll().subscribe(items => {
      this.cakeTierSizes  = items.filter(i => i.category === 'CakeTierSize'  && i.isActive).map(i => i.value);
      this.cakeShapes     = items.filter(i => i.category === 'CakeShape'     && i.isActive).map(i => i.value);
      this.flavors        = items.filter(i => i.category === 'Flavor'        && i.isActive).map(i => i.value);
      this.fillingFlavors = items.filter(i => i.category === 'FillingFlavor' && i.isActive).map(i => i.value);
      this.icingFlavors   = items.filter(i => i.category === 'IcingFlavor'   && i.isActive).map(i => i.value);
      this.cupcakeSizes   = items.filter(i => i.category === 'CupcakeSize'   && i.isActive).map(i => i.value);
      this.cookieTypes    = items.filter(i => i.category === 'CookieType'    && i.isActive).map(i => i.value);
      this.cookieSizes    = items.filter(i => i.category === 'CookieSize'    && i.isActive).map(i => i.value);
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
          this.orderToEdit = new Order();
          return this.orderService.GetNewOrderNumber();
        }
      }),
      takeUntil(this.destroy$)
    ).subscribe(result => {
      if (typeof result === 'number') {
        this.orderToEdit = {
          ...new Order(),
          orderNumber: result,
          dateOrderPlaced: new Date()
        };
      } else {
        this.orderToEdit = result;
        this.loadAttachments(result.orderNumber!);
      }
      this.initOrderFormGroup();
    });

    // Autosave: save after 4 seconds of inactivity when any meaningful field has data
    this.editOrderFormGroup.valueChanges.pipe(
      debounceTime(4000),
      filter(val => {
        const { orderNumber, dateOrderPlaced, ...rest } = val;
        return Object.values(rest).some(v =>
          v !== null && v !== '' && v !== false && v !== undefined &&
          !(Array.isArray(v) && (v as any[]).length === 0)
        );
      }),
      takeUntil(this.destroy$)
    ).subscribe(() => this.autoSave());
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  initOrderFormGroup() {
    // Clear all FormArrays to prevent stale rows when switching between orders
    (this.editOrderFormGroup.get('cakeTierInfo') as FormArray).clear();
    (this.editOrderFormGroup.get('cupcakeInfo') as FormArray).clear();
    (this.editOrderFormGroup.get('pupcakeInfo') as FormArray).clear();
    (this.editOrderFormGroup.get('cookieInfo') as FormArray).clear();
    (this.editOrderFormGroup.get('otherItemInfo') as FormArray).clear();

    this.isCakeDetailsHeaderShown = false;
    this.isCupcakeDetailsHeaderShown = false;
    this.isPupcakeDetailsHeaderShown = false;
    this.isCookieDetailsHeaderShown = false;
    this.isOtherDetailsHeaderShown = false;

    const dt = this.orderToEdit.orderDateTime ? new Date(this.orderToEdit.orderDateTime) : null;
    const orderDate = dt ? this.formatDate(dt) : '';
    const orderTime = dt ? this.formatTime(dt) : '';

    this.editOrderFormGroup.patchValue({
      orderNumber: this.orderToEdit.orderNumber,
      orderDate,
      orderTime,
      deliveryLocation: this.orderToEdit.deliveryLocation,
      custName: this.orderToEdit.custName,
      custEmail: this.orderToEdit.custEmail,
      custPhone: this.orderToEdit.custPhone,
      details: this.orderToEdit.details,
      orderType: this.orderToEdit.orderType,
      title: this.orderToEdit.title,
      secondaryName: this.orderToEdit.secondaryName,
      secondaryPhone: this.orderToEdit.secondaryPhone,
      initialContact: this.orderToEdit.initialContact,
      contractSent: this.orderToEdit.contractSent,
      dayOfTextSent: this.orderToEdit.dayOfTextSent,
      confirmationTextSent: this.orderToEdit.confirmationTextSent,
      isReadyForPickup: this.orderToEdit.isReadyForPickup ?? false,
      totalCost: this.orderToEdit.totalCost,
      depositAmount: this.orderToEdit.depositAmount,
      depositPaymentMethod: this.orderToEdit.depositPaymentMethod,
      depositDateTime: this.orderToEdit.depositDateTime,
      finalPaymentMethod: this.orderToEdit.finalPaymentMethod,
      finalPaymentDateTime: this.orderToEdit.finalPaymentDateTime,
      dateOrderPlaced: this.formatDate(this.orderToEdit.dateOrderPlaced!),
      paidInFull: this.orderToEdit.paidInFull,
      labor: this.orderToEdit.labor,
      flavorUpgrade: this.orderToEdit.flavorUpgrade,
      lookbookPrice: this.orderToEdit.lookbookPrice
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
        splitTier: [cake.splitTier ?? false],
        flavor2: [cake.flavor2 ?? ''],
        useLayerFlavors: [!!cake.layerFlavors],
        layerFlavors: [cake.layerFlavors ?? '']
      }));
    });
    if (cakeTierInfo.length > 0) this.isCakeDetailsHeaderShown = true;

    const cupcakeInfo = this.editOrderFormGroup.get('cupcakeInfo') as FormArray;
    this.orderToEdit.cupcakes?.forEach(cupcake => {
      cupcakeInfo.push(this._formBuilder.group({
        cupcakeSize: [cupcake.cupcakeSize],
        cupcakeQuantity: [cupcake.cupcakeQuantity],
        cupcakeFlavor: [cupcake.cupcakeFlavor],
        fillingFlavor: [cupcake.fillingFlavor],
        icingFlavor: [cupcake.icingFlavor],
        signatureName: [cupcake.signatureName],
        isSignature: [!!cupcake.signatureName]
      }));
    });
    if (cupcakeInfo.length > 0) this.isCupcakeDetailsHeaderShown = true;

    const pupcakeInfo = this.editOrderFormGroup.get('pupcakeInfo') as FormArray;
    this.orderToEdit.pupcakes?.forEach(pupcake => {
      pupcakeInfo.push(this._formBuilder.group({
        pupcakeSize: [pupcake.pupcakeSize],
        pupcakeQuantity: [pupcake.pupcakeQuantity]
      }));
    });
    if (pupcakeInfo.length > 0) this.isPupcakeDetailsHeaderShown = true;

    const cookieInfo = this.editOrderFormGroup.get('cookieInfo') as FormArray;
    this.orderToEdit.cookies?.forEach(cookie => {
      cookieInfo.push(this._formBuilder.group({
        cookieType: [cookie.cookieType],
        cookieQuantity: [cookie.cookieQuantity],
        cookieSize: [cookie.cookieSize ?? '']
      }));
    });
    if (cookieInfo.length > 0) this.isCookieDetailsHeaderShown = true;

    const otherItemInfo = this.editOrderFormGroup.get('otherItemInfo') as FormArray;
    this.orderToEdit.otherItems?.forEach(item => {
      otherItemInfo.push(this._formBuilder.group({
        name: [item.name ?? ''],
        item: [item.item ?? '']
      }));
    });
    if (otherItemInfo.length > 0) this.isOtherDetailsHeaderShown = true;
  }

  // format date to yyyy-mm-dd
  formatDate(date: Date) {
    var d = new Date(date);
    var month = '' + (d.getMonth() + 1);
    var day = '' + d.getDate();
    var year = d.getFullYear();

    if (month.length < 2) month = '0' + month;
    if (day.length < 2) day = '0' + day;

    return [year, month, day].join('-');
  }

  // format time to HH:mm
  formatTime(date: Date): string {
    const d = new Date(date);
    const hours = String(d.getHours()).padStart(2, '0');
    const minutes = String(d.getMinutes()).padStart(2, '0');
    return `${hours}:${minutes}`;
  }

  // Combine separate date and time controls into a Date
  combineDateAndTime(): Date | undefined {
    const dateStr = this.editOrderFormGroup.get('orderDate')?.value;
    const timeStr = this.editOrderFormGroup.get('orderTime')?.value;
    if (!dateStr) return undefined;
    const combined = timeStr ? `${dateStr}T${timeStr}` : `${dateStr}T00:00`;
    return new Date(combined);
  }

  // Build Order from form values (shared by create/update/autoSave)
  buildOrderFromForm(): Order {
    const { cakeTierInfo, cupcakeInfo, pupcakeInfo, cookieInfo, otherItemInfo, orderDate, orderTime, ...formValue } = this.editOrderFormGroup.value;
    return {
      ...formValue,
      orderDateTime: this.combineDateAndTime(),
      cakes: cakeTierInfo,
      cupcakes: cupcakeInfo,
      pupcakes: pupcakeInfo,
      cookies: cookieInfo,
      otherItems: otherItemInfo,
      cancelledFlag: this.orderToEdit.cancelledFlag,
      cancellationReason: this.orderToEdit.cancellationReason,
      cancelledAt: this.orderToEdit.cancelledAt
    };
  }

  // format input as telephone number as it is typed
  onTelInput(event: Event, controlName: string) {
    const input = event.target as HTMLInputElement;
    let val = input.value.replace(/\D/g, '');
    if (val.length < 9 && val !== null) {
      let finalVal = val!.match(/.{1,3}/g)!.join('-');
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
      this.orderToEdit = this.buildOrderFromForm();
      this.orderService
        .UpdateOrder(this.orderToEdit)
        .subscribe({
          next: (result: number) => {
            if (result === this.orderToEdit.orderNumber) {
              this.onSubmitSuccess(`Order ${this.orderToEdit.orderNumber} updated.`);
            }
          },
          error: (err: any) => {
            const errorMessage = Object.values(err.error.errors).join('\n');
            this.toastFailure(errorMessage);
          }
        });
    } else {
      const invalidControlsString = this.getInvalidControlLabels();
      this.toastFailure(`Please check the form for missing information: ${invalidControlsString}`);
    }
  }

  // Archive (soft-cancel) flow
  openArchiveModal() {
    this.archiveCancellationReason = '';
    this.showArchiveModal = true;
  }

  confirmArchive() {
    if (!this.archiveCancellationReason.trim()) {
      this.toastFailure('Cancellation reason is required.');
      return;
    }
    this.orderService.CancelOrder(this.orderToEdit.orderNumber!, this.archiveCancellationReason)
      .subscribe({
        next: () => {
          this.showArchiveModal = false;
          this.onSubmitSuccess(`Order ${this.orderToEdit.orderNumber} archived.`);
        },
        error: (err: any) => {
          const msg = err.error?.message ?? err.message ?? 'Archive failed.';
          this.toastFailure(msg);
        }
      });
  }

  cancelArchiveModal() {
    this.showArchiveModal = false;
    this.archiveCancellationReason = '';
  }

  restoreOrder() {
    // Prepend original cancellation info to the Details field before restoring
    const currentDetails = this.editOrderFormGroup.get('details')?.value ?? '';
    const timestamp = this.orderToEdit.cancelledAt
      ? new Date(this.orderToEdit.cancelledAt).toLocaleString()
      : 'Unknown';
    const reason = this.orderToEdit.cancellationReason ?? '';
    const prefix = `Original Cancellation Reason - ${timestamp} - ${reason}\n\n`;
    this.editOrderFormGroup.get('details')?.setValue(prefix + currentDetails, { emitEvent: false });

    this.orderService.RestoreOrder(this.orderToEdit.orderNumber!).subscribe(() => {
      this.orderToEdit = {
        ...this.orderToEdit,
        cancelledFlag: false,
        cancellationReason: undefined,
        cancelledAt: undefined
      };
      this.toastSuccess(`Order ${this.orderToEdit.orderNumber} restored.`);
    });
  }

  onCreateOrderClicked() {
    this.action = 'create';
  }

  createOrder() {
    if (this.editOrderFormGroup.valid) {
      this.orderToEdit = this.buildOrderFromForm();
      this.orderService
        .AddOrder(this.orderToEdit)
        .subscribe({
          next: (result: number) => {
            this.orderToEdit = { ...this.orderToEdit, orderNumber: result };
            this.onSubmitSuccess(`Order ${result} created.`);
          },
          error: (err: any) => {
            const errorMessage = err.error ? err.error.message : err.message;
            this.toastFailure(errorMessage);
          }
        });
    } else {
      const invalidControlsString = this.getInvalidControlLabels();
      this.toastFailure(`Please check the form for missing information: ${invalidControlsString}`);
    }
  }

  autoSave() {
    const order = this.buildOrderFromForm();
    if (this.createOrUpdate === 'Create') {
      this.orderService.AddOrder(order).subscribe((result: number) => {
        this.createOrUpdate = 'Update';
        this.editOrderFormGroup.get('orderNumber')?.setValue(result, { emitEvent: false });
        this.orderToEdit = { ...order, orderNumber: result };
        this.loadAttachments(result);
        this.toastSuccess(`Order ${result} auto-saved.`);
      });
    } else {
      this.orderService.UpdateOrder(order).subscribe(() => {
        this.toastSuccess(`Order ${order.orderNumber} auto-saved.`);
      });
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
    }
  }

  onSubmitSuccess(message: string) {
    this.toastSuccess(message);
    let navDetails: NavigationExtras = {
      queryParams: { orderNumber: this.orderToEdit.orderNumber }
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
              const gc = group.controls[groupControlName];
              if (gc.invalid) {
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
        input.value = '';
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
      case 'cakeTierInfo':
        this.isCakeDetailsHeaderShown = true;
        // Auto-append a cake note label to the details field
        const letter = 'ABCDEFGHIJ'[formArray.length - 1] ?? formArray.length.toString();
        const currentDetails = this.editOrderFormGroup.get('details')?.value ?? '';
        const newNote = currentDetails ? `${currentDetails}\nCake ${letter} - ` : `Cake ${letter} - `;
        this.editOrderFormGroup.get('details')?.setValue(newNote, { emitEvent: false });
        break;
      case 'cupcakeInfo': this.isCupcakeDetailsHeaderShown = true; break;
      case 'pupcakeInfo': this.isPupcakeDetailsHeaderShown = true; break;
      case 'cookieInfo': this.isCookieDetailsHeaderShown = true; break;
      case 'otherItemInfo': this.isOtherDetailsHeaderShown = true; break;
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
        case 'otherItemInfo': this.isOtherDetailsHeaderShown = false; break;
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
      splitTier: [false],
      flavor2: [''],
      useLayerFlavors: [false],
      layerFlavors: ['']
    });
  }

  getBlankCupcakeFormControls() {
    return this._formBuilder.group({
      cupcakeSize: ['', Validators.required],
      cupcakeQuantity: ['', Validators.required],
      cupcakeFlavor: ['', Validators.required],
      fillingFlavor: ['', Validators.required],
      icingFlavor: ['', Validators.required],
      signatureName: [''],
      isSignature: [false]
    });
  }

  addSignatureCupcakeRow() {
    const group = this._formBuilder.group({
      cupcakeSize: ['Regular', Validators.required],
      cupcakeQuantity: ['', Validators.required],
      cupcakeFlavor: ['', Validators.required],
      fillingFlavor: ['', Validators.required],
      icingFlavor: ['', Validators.required],
      signatureName: [''],
      isSignature: [true]
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
      cookieQuantity: ['', Validators.required],
      cookieSize: ['']
    });
  }

  getBlankOtherItemFormControls() {
    return this._formBuilder.group({
      name: [''],
      item: ['']
    });
  }

  // Returns an array of per-layer flavor strings for a given cake row
  getLayerFlavorsForRow(cakeIndex: number): string[] {
    const arr = this.editOrderFormGroup.get('cakeTierInfo') as FormArray;
    const row = arr.at(cakeIndex) as FormGroup;
    const numLayers = Number(row.get('numTierLayers')?.value) || 1;
    const layerFlavorsStr = row.get('layerFlavors')?.value ?? '';
    if (layerFlavorsStr) {
      try {
        const parsed = JSON.parse(layerFlavorsStr);
        if (Array.isArray(parsed)) {
          // Ensure length matches numLayers
          while (parsed.length < numLayers) parsed.push('');
          return parsed.slice(0, numLayers);
        }
      } catch {}
    }
    return Array(numLayers).fill('');
  }

  onLayerFlavorChange(cakeIndex: number, layerIndex: number, value: string) {
    const arr = this.editOrderFormGroup.get('cakeTierInfo') as FormArray;
    const row = arr.at(cakeIndex) as FormGroup;
    const current = this.getLayerFlavorsForRow(cakeIndex);
    current[layerIndex] = value;
    row.get('layerFlavors')?.setValue(JSON.stringify(current), { emitEvent: false });
  }
}
