import { Component, OnInit, OnDestroy, Renderer2, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormGroup, FormBuilder, Validators, AbstractControl, FormArray } from '@angular/forms';
import { RouterModule, ActivatedRoute, NavigationExtras, Router } from '@angular/router';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { Observable, Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, filter, takeUntil, switchMap, tap } from 'rxjs/operators';
import { Order } from '../../models/order';
import { OrderAttachment } from '../../models/order-attachment';
import { SignatureCupcake } from '../../models/signature-cupcake';
import { CustomerSearchResult } from '../../models/customer';
import { OrderService } from '../../services/order.service';
import { AttachmentService } from '../../services/attachment.service';
import { OptionService } from '../../services/option.service';
import { SignatureCupcakeService } from '../../services/signature-cupcake.service';
import { CustomerService } from '../../services/customer.service';
import { GoogleService, GoogleStatus } from '../../services/google.service';
import { WeddingDetails } from '../../models/wedding-details';
import { WeddingIconComponent } from '../shared/wedding-icon/wedding-icon.component';

@Component({
  selector: 'app-edit-order-component',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterModule, NgbModule, WeddingIconComponent],
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

  createdByStaffName: string | null = null;

  // Archive modal state
  showArchiveModal: boolean = false;
  archiveCancellationReason: string = '';

  // Attachment carousel state
  carouselOpen: boolean = false;
  carouselIndex: number = 0;

  get imageAttachments(): OrderAttachment[] {
    return this.attachments.filter(a => a.contentType.startsWith('image/'));
  }

  openCarousel(attachment: OrderAttachment): void {
    const idx = this.imageAttachments.findIndex(a => a.id === attachment.id);
    this.carouselIndex = idx >= 0 ? idx : 0;
    this.carouselOpen = true;
  }

  closeCarousel(): void {
    this.carouselOpen = false;
  }

  carouselPrev(): void {
    const len = this.imageAttachments.length;
    this.carouselIndex = (this.carouselIndex - 1 + len) % len;
  }

  carouselNext(): void {
    this.carouselIndex = (this.carouselIndex + 1) % this.imageAttachments.length;
  }

  // Incomplete-order warning modal state
  showIncompleteWarning: boolean = false;

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
  private _prefillParams: any = {};

  // Wedding contract (Google Docs)
  googleStatus: GoogleStatus | null = null;
  showContractModal = false;
  generatingContract = false;
  contractWarnings: string[] = [];

  readonly boardColors = ['White', 'Gold', 'Silver', 'Black'];
  readonly flowerTypes = [
    { value: 'Live', label: 'Live flowers/greens' },
    { value: 'Fake', label: 'Fake flowers/greens' },
    { value: 'Buttercream', label: 'Buttercream flowers' },
    { value: 'N/A', label: 'N/A' }
  ];
  readonly flowerProviders = [
    { value: 'Florist', label: "Customer's contracted florist" },
    { value: 'Customer', label: 'Customer sourcing their own' },
    { value: 'CCC', label: 'Canonsburg Cake Company (buttercream)' },
    { value: 'N/A', label: 'N/A' }
  ];
  readonly dateHelpText = 'Event Date is the day of the wedding (printed on the contract). ' +
    'Delivery/Pickup Date is when the order leaves the bakery; it drives the bake sheet and order lists. ' +
    'They are usually the same, but a pickup is often the day before the wedding.';

  // Customer autocomplete
  customerSuggestions: CustomerSearchResult[] = [];
  showCustomerDropdown = false;

  // main form group
  editOrderFormGroup: FormGroup = this._formBuilder.group({
    orderNumber: ['', Validators.required],
    orderDate: ['', Validators.required],
    orderTime: [''],
    deliveryLocation: [''],
    custName: ['', Validators.required],
    custEmail: [''],
    custPhone: [''],   // optional when email is provided — handled by soft validation
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
    customerId: [null as number | null],
    isWedding: [false],
    wedding: this._formBuilder.group({
      eventDate: [''],
      receptionLocation: [''],
      ceremonySameLocation: [null as boolean | null],
      ceremonyTime: [''],
      receptionTime: [''],
      partner2Name: [''],
      partner2Phone: [''],
      dayOfContactTitle: [''],
      venueContactName: [''],
      venueContactPhone: [''],
      contractReturnByDate: [''],
      cakeBoardColor: [''],
      cakeTopper: [null as boolean | null],
      hasFlowers: [null as boolean | null],
      flowerType: [''],
      flowersProvidedBy: [''],
      floristName: [''],
      floristPhone: [''],
      floristDeliveryTime: [''],
      deliveryWindowEnd: [''],
      pickupPersonName: [''],
      pickupPersonPhone: [''],
      mainCakeFlavorDescription: [''],
      mainCakeDesignDescription: [''],
      kitchenCakeFlavorDescription: [''],
      cupcakeFlavorDescription: [''],
      cupcakeDesignDescription: [''],
      totalServings: [null as number | null]
    }),
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
    private sigService: SignatureCupcakeService,
    private customerService: CustomerService,
    private googleService: GoogleService
  ) { }

  get isWedding(): boolean {
    return !!this.editOrderFormGroup.get('isWedding')?.value;
  }

  get weddingGroup(): FormGroup {
    return this.editOrderFormGroup.get('wedding') as FormGroup;
  }

  get orderTypeValue(): string {
    return this.editOrderFormGroup.get('orderType')?.value ?? '';
  }

  // Label for the order's own date: in wedding mode it is the delivery or pickup date (not the event date)
  get orderDateLabel(): string {
    if (this.isWedding && this.orderTypeValue === 'Pickup') return 'Pickup Date *';
    return 'Delivery Date *';
  }

  get orderTimeLabel(): string {
    if (!this.isWedding) return 'Delivery Time';
    return this.orderTypeValue === 'Pickup' ? 'Pickup Time' : 'Delivery Window Start';
  }

  // Latest generated contract, kept apart from orderToEdit (which the save paths rebuild from the form)
  contract: { name: string; url: string; generatedAt: string | null } | null = null;

  // Active-only signatures for new orders; all for existing orders
  get signaturesForForm(): SignatureCupcake[] {
    return this.createOrUpdate === 'Update' ? this.signatures : this.signatures.filter(s => s.isActive);
  }

  // Computes the set of incomplete-field keys based on current form values.
  // Keys are used by isFieldIncomplete() to drive red highlighting in the template.
  get incompleteReasons(): Set<string> {
    const v = this.editOrderFormGroup.value;
    const r = new Set<string>();

    if (!v.title?.trim()) r.add('title');
    if (!v.custName?.trim()) r.add('custName');
    if (!v.custPhone?.trim() && !v.custEmail?.trim()) r.add('contact');
    if (!v.orderDate) r.add('orderDate');
    if (!v.orderType) r.add('orderType');

    const hasItems = (v.cakeTierInfo?.length ?? 0) > 0
      || (v.cupcakeInfo?.length ?? 0) > 0
      || (v.pupcakeInfo?.length ?? 0) > 0
      || (v.cookieInfo?.length ?? 0) > 0
      || (v.otherItemInfo?.length ?? 0) > 0;
    if (!hasItems) r.add('noItems');

    (v.cakeTierInfo as any[] ?? []).forEach((cake: any, i: number) => {
      if (!cake.tierSize) r.add(`cake_${i}_tierSize`);
      if (!cake.numTierLayers || +cake.numTierLayers < 1) r.add(`cake_${i}_numTierLayers`);
      if (!cake.cakeShape) r.add(`cake_${i}_cakeShape`);
      if (!cake.cakeFlavor) r.add(`cake_${i}_cakeFlavor`);
      if (!cake.icingFlavor) r.add(`cake_${i}_icingFlavor`);
    });

    (v.cupcakeInfo as any[] ?? []).forEach((cup: any, i: number) => {
      if (!cup.cupcakeSize) r.add(`cupcake_${i}_cupcakeSize`);
      if (!cup.cupcakeQuantity || +cup.cupcakeQuantity < 1) r.add(`cupcake_${i}_cupcakeQuantity`);
      if (!cup.cupcakeFlavor) r.add(`cupcake_${i}_cupcakeFlavor`);
      if (!cup.icingFlavor) r.add(`cupcake_${i}_icingFlavor`);
    });

    (v.pupcakeInfo as any[] ?? []).forEach((pup: any, i: number) => {
      if (!pup.pupcakeQuantity || +pup.pupcakeQuantity < 1) r.add(`pupcake_${i}_pupcakeQuantity`);
    });

    (v.cookieInfo as any[] ?? []).forEach((cookie: any, i: number) => {
      if (!cookie.cookieQuantity || +cookie.cookieQuantity < 1) r.add(`cookie_${i}_cookieQuantity`);
    });

    // Wedding: the staff-filled blanks on the contract (eSignature boxes are left to the client)
    if (v.isWedding) {
      const w = v.wedding ?? {};
      if (!w.eventDate) r.add('w_eventDate');
      if (!w.receptionLocation?.trim()) r.add('w_receptionLocation');
      if (!w.partner2Name?.trim()) r.add('w_partner2Name');
      if (!w.partner2Phone?.trim()) r.add('w_partner2Phone');
      if (!w.contractReturnByDate) r.add('w_contractReturnByDate');
      if (w.totalServings === null || w.totalServings === undefined || w.totalServings === '') r.add('w_totalServings');
      if (!v.custEmail?.trim()) r.add('w_custEmail');
      if (v.orderType === 'Delivery' && !v.deliveryLocation?.trim()) r.add('w_deliveryLocation');
    }

    return r;
  }

  get isOrderIncomplete(): boolean {
    return this.incompleteReasons.size > 0;
  }

  isFieldIncomplete(key: string): boolean {
    return this.incompleteReasons.has(key);
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
    this.googleService.GetStatus().subscribe({
      next: status => this.googleStatus = status,
      error: () => this.googleStatus = null
    });

    // Event Date follows the delivery/pickup date until it is set to something different
    let prevOrderDate = '';
    this.editOrderFormGroup.get('orderDate')!.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(date => {
      const eventDate = this.weddingGroup.get('eventDate')!;
      if (!eventDate.value || eventDate.value === prevOrderDate) eventDate.setValue(date ?? '');
      prevOrderDate = date ?? '';
    });

    // Delivery window end defaults to start + 2 hours (the contract asks for a 2-hour window)
    let prevWindowEnd = '';
    this.editOrderFormGroup.get('orderTime')!.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(time => {
      const end = this.weddingGroup.get('deliveryWindowEnd')!;
      const newEnd = this.addHours(time, 2);
      if (!end.value || end.value === prevWindowEnd) end.setValue(newEnd);
      prevWindowEnd = newEnd;
    });

    // Turning on wedding mode fills the event date from the order date
    this.editOrderFormGroup.get('isWedding')!.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(on => {
      const eventDate = this.weddingGroup.get('eventDate')!;
      if (on && !eventDate.value) eventDate.setValue(this.editOrderFormGroup.get('orderDate')?.value ?? '');
    });

    this.route.queryParams.pipe(
      switchMap(params => {
        this._prefillParams = params;
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
        this.orderService.GetCreatedBy(result.orderNumber!).subscribe(r => {
          this.createdByStaffName = r.staffName;
        });
      }
      this.initOrderFormGroup();
      // Pre-fill contact fields when navigating from Customer detail
      if (this.createOrUpdate === 'Create') {
        const p = this._prefillParams;
        if (p['custName'] || p['customerId']) {
          this.editOrderFormGroup.patchValue({
            custName:   p['custName']   ?? '',
            custEmail:  p['custEmail']  ?? '',
            custPhone:  p['custPhone']  ?? '',
            customerId: p['customerId'] ? Number(p['customerId']) : null
          }, { emitEvent: false });
        }
      }
    });

    // Customer autocomplete on custName field
    this.editOrderFormGroup.get('custName')!.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      filter(val => val && val.length >= 2),
      switchMap(val => this.customerService.Search(val)),
      takeUntil(this.destroy$)
    ).subscribe(results => {
      this.customerSuggestions = results;
      this.showCustomerDropdown = results.length > 0;
    });

    // Autosave: save after 4 seconds of inactivity when any meaningful field has data
    this.editOrderFormGroup.valueChanges.pipe(
      debounceTime(15000),
      filter(val => {
        const { orderNumber, dateOrderPlaced, wedding, isWedding, ...rest } = val;
        const filled = (v: any) =>
          v !== null && v !== '' && v !== false && v !== undefined &&
          !(Array.isArray(v) && (v as any[]).length === 0);
        return Object.values(rest).some(filled) || (isWedding && Object.values(wedding ?? {}).some(filled));
      }),
      takeUntil(this.destroy$)
    ).subscribe(() => this.autoSave());
  }

  selectCustomer(c: CustomerSearchResult) {
    this.editOrderFormGroup.patchValue({
      custName:  `${c.firstName} ${c.lastName}`.trim(),
      custEmail: c.email  ?? '',
      custPhone: c.phone  ?? '',
      customerId: c.customerId ?? null
    }, { emitEvent: false });
    this.showCustomerDropdown = false;
    this.customerSuggestions = [];
  }

  closeCustomerDropdown() {
    // Small delay so a click on a suggestion fires before the dropdown closes
    setTimeout(() => {
      this.showCustomerDropdown = false;
    }, 150);
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
      lookbookPrice: this.orderToEdit.lookbookPrice,
      customerId: this.orderToEdit.customerId ?? null,
      isWedding: this.orderToEdit.isWedding ?? false
    });
    this.patchWeddingForm(this.orderToEdit.weddingDetails);
    const wd = this.orderToEdit.weddingDetails;
    this.contract = wd?.contractDocId
      ? { name: wd.contractDocName ?? 'Wedding Contract', url: wd.contractDocUrl ?? '', generatedAt: wd.contractGeneratedAt ?? null }
      : null;
    this.contractWarnings = [];

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

  addHours(hhmm: string, hours: number): string {
    if (!hhmm) return '';
    const [h, m] = hhmm.split(':').map(Number);
    if (isNaN(h) || isNaN(m)) return '';
    return `${String((h + hours) % 24).padStart(2, '0')}:${String(m).padStart(2, '0')}`;
  }

  patchWeddingForm(w: WeddingDetails | null | undefined) {
    const d = w ?? {};
    this.weddingGroup.reset({
      eventDate: d.eventDate ? this.formatDate(new Date(d.eventDate)) : this.editOrderFormGroup.get('orderDate')?.value ?? '',
      receptionLocation: d.receptionLocation ?? '',
      ceremonySameLocation: d.ceremonySameLocation ?? null,
      ceremonyTime: d.ceremonyTime ?? '',
      receptionTime: d.receptionTime ?? '',
      partner2Name: d.partner2Name ?? '',
      partner2Phone: d.partner2Phone ?? '',
      dayOfContactTitle: d.dayOfContactTitle ?? '',
      venueContactName: d.venueContactName ?? '',
      venueContactPhone: d.venueContactPhone ?? '',
      contractReturnByDate: d.contractReturnByDate ? this.formatDate(new Date(d.contractReturnByDate)) : '',
      cakeBoardColor: d.cakeBoardColor ?? '',
      cakeTopper: d.cakeTopper ?? null,
      hasFlowers: d.hasFlowers ?? null,
      flowerType: d.flowerType ?? '',
      flowersProvidedBy: d.flowersProvidedBy ?? '',
      floristName: d.floristName ?? '',
      floristPhone: d.floristPhone ?? '',
      floristDeliveryTime: d.floristDeliveryTime ?? '',
      deliveryWindowEnd: d.deliveryWindowEnd ?? (this.addHours(this.editOrderFormGroup.get('orderTime')?.value ?? '', 2)),
      pickupPersonName: d.pickupPersonName ?? '',
      pickupPersonPhone: d.pickupPersonPhone ?? '',
      mainCakeFlavorDescription: d.mainCakeFlavorDescription ?? '',
      mainCakeDesignDescription: d.mainCakeDesignDescription ?? '',
      kitchenCakeFlavorDescription: d.kitchenCakeFlavorDescription ?? '',
      cupcakeFlavorDescription: d.cupcakeFlavorDescription ?? '',
      cupcakeDesignDescription: d.cupcakeDesignDescription ?? '',
      totalServings: d.totalServings ?? null
    }, { emitEvent: false });
  }

  // Wedding form → WeddingDetails payload ('' → null). Only sent when wedding mode is on;
  // the server keeps previously saved details when it receives null.
  buildWeddingDetails(): WeddingDetails | null {
    if (!this.isWedding) return null;
    const w = this.weddingGroup.value;
    const out: any = {};
    Object.keys(w).forEach(k => out[k] = w[k] === '' ? null : w[k]);
    out.totalServings = w.totalServings === null || w.totalServings === '' ? null : Number(w.totalServings);
    return out as WeddingDetails;
  }

  // Build Order from form values (shared by create/update/autoSave)
  buildOrderFromForm(): Order {
    const { cakeTierInfo, cupcakeInfo, pupcakeInfo, cookieInfo, otherItemInfo, orderDate, orderTime, wedding, ...formValue } = this.editOrderFormGroup.value;
    return {
      ...formValue,
      weddingDetails: this.buildWeddingDetails(),
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
      let finalVal = val!.match(/.{1,3}/g)?.join('-') ?? '';
      this.editOrderFormGroup.get(controlName)?.setValue(finalVal);
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

  updateOrder(force = false) {
    if (force || this.editOrderFormGroup.valid) {
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

  createOrder(force = false) {
    if (force || this.editOrderFormGroup.valid) {
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
    if (this.action !== 'none' && this.isOrderIncomplete) {
      this.showIncompleteWarning = true;
      return;
    }
    this.executeSave();
  }

  confirmSaveAnyway() {
    this.showIncompleteWarning = false;
    this.executeSave(true);
  }

  dismissIncompleteWarning() {
    this.showIncompleteWarning = false;
  }

  private executeSave(force = false) {
    switch (this.action) {
      case 'create':
        this.createOrder(force);
        break;
      case 'update':
        this.updateOrder(force);
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

    if (this.createOrUpdate === 'Create') {
      // Save the order first so we have an orderNumber, then upload
      const order = this.buildOrderFromForm();
      this.orderService.AddOrder(order).subscribe((result: number) => {
        this.createOrUpdate = 'Update';
        this.editOrderFormGroup.get('orderNumber')?.setValue(result, { emitEvent: false });
        this.orderToEdit = { ...order, orderNumber: result };
        this.toastSuccess(`Order ${result} auto-saved.`);
        this.doUpload(result, files, input);
      });
    } else {
      this.doUpload(this.orderToEdit.orderNumber!, files, input);
    }
  }

  private doUpload(orderNumber: number, files: File[], input: HTMLInputElement) {
    this.attachmentService.UploadFiles(orderNumber, files).subscribe({
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

  // ── Wedding contract ────────────────────────────────────────────────

  onGenerateContractClicked() {
    if (this.contract) {
      this.showContractModal = true;
    } else {
      this.generateContract(null);
    }
  }

  cancelContractModal() {
    this.showContractModal = false;
  }

  generateContract(mode: 'overwrite' | 'revision' | null) {
    this.showContractModal = false;
    this.generatingContract = true;
    this.contractWarnings = [];
    this.saveForContract().pipe(
      switchMap(orderNumber => this.googleService.GenerateContract(orderNumber, mode))
    ).subscribe({
      next: result => {
        this.generatingContract = false;
        this.contract = { name: result.name, url: result.url, generatedAt: result.generatedAt };
        this.contractWarnings = result.warnings ?? [];
        this.toastSuccess(`Contract created: ${result.name}`);
      },
      error: (err: any) => {
        this.generatingContract = false;
        this.toastFailure(err.error?.message ?? err.message ?? 'Contract generation failed.');
      }
    });
  }

  // Saves the form first (creating the order if needed) so the contract matches what's on screen
  private saveForContract(): Observable<number> {
    const order = this.buildOrderFromForm();
    if (this.createOrUpdate === 'Create') {
      return this.orderService.AddOrder(order).pipe(tap(result => {
        this.createOrUpdate = 'Update';
        this.editOrderFormGroup.get('orderNumber')?.setValue(result, { emitEvent: false });
        this.orderToEdit = { ...order, orderNumber: result };
        this.loadAttachments(result);
      }));
    }
    return this.orderService.UpdateOrder(order).pipe(tap(() => this.orderToEdit = order));
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
      cupcakeSize: ['Regular', Validators.required],
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
