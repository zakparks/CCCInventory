import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OptionItem } from '../../models/option-item';
import { SignatureCupcake } from '../../models/signature-cupcake';
import { OptionService } from '../../services/option.service';
import { SignatureCupcakeService } from '../../services/signature-cupcake.service';

interface CategorySection {
  key: string;
  label: string;
  expanded: boolean;
  items: OptionItem[];
  newValue: string;
}

@Component({
  selector: 'app-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './management.component.html',
  styleUrls: ['./management.component.css']
})
export class ManagementComponent implements OnInit {
  // Option categories
  categories: CategorySection[] = [
    { key: 'CakeTierSize',  label: 'Cake Tier Sizes',         expanded: false, items: [], newValue: '' },
    { key: 'CakeShape',     label: 'Cake Shapes',              expanded: false, items: [], newValue: '' },
    { key: 'Flavor',        label: 'Flavors (Cake & Cupcake)', expanded: false, items: [], newValue: '' },
    { key: 'FillingFlavor', label: 'Filling Flavors',          expanded: false, items: [], newValue: '' },
    { key: 'IcingFlavor',   label: 'Icing Flavors',            expanded: false, items: [], newValue: '' },
    { key: 'CupcakeSize',   label: 'Cupcake Sizes',            expanded: false, items: [], newValue: '' },
    { key: 'CookieType',    label: 'Cookie Types',             expanded: false, items: [], newValue: '' },
  ];

  // Option item inline edit
  editingOptionId: number | null = null;
  editingOptionValue: string = '';
  errorMsg: string = '';

  // Signature cupcakes
  sigExpanded = false;
  signatures: SignatureCupcake[] = [];
  editingSig: SignatureCupcake | null = null;
  newSig: Partial<SignatureCupcake> = {};
  addingSig = false;
  sigSortCol: string = 'name';
  sigSortDir: 'asc' | 'desc' = 'asc';

  constructor(
    private optionService: OptionService,
    private sigService: SignatureCupcakeService
  ) { }

  ngOnInit() {
    this.loadOptions();
    this.loadSignatures();
  }

  // ─── Options ─────────────────────────────────────────────────────────────

  get optionFlavors(): string[] {
    return this.categories.find(c => c.key === 'Flavor')?.items.filter(i => i.isActive).map(i => i.value) ?? [];
  }

  get optionFillingFlavors(): string[] {
    return this.categories.find(c => c.key === 'FillingFlavor')?.items.filter(i => i.isActive).map(i => i.value) ?? [];
  }

  get optionIcingFlavors(): string[] {
    return this.categories.find(c => c.key === 'IcingFlavor')?.items.filter(i => i.isActive).map(i => i.value) ?? [];
  }

  loadOptions() {
    this.optionService.getAll().subscribe(items => {
      for (const cat of this.categories) {
        cat.items = items.filter(i => i.category === cat.key);
      }
    });
  }

  addOption(cat: CategorySection) {
    const val = cat.newValue.trim();
    if (!val) return;
    this.optionService.create({ category: cat.key, value: val, isActive: true, sortOrder: cat.items.length })
      .subscribe(created => {
        cat.items.push(created);
        cat.newValue = '';
      });
  }

  toggleActive(item: OptionItem) {
    this.optionService.update(item.id, { ...item, isActive: !item.isActive })
      .subscribe(updated => Object.assign(item, updated));
  }

  startEditOption(item: OptionItem) {
    this.editingOptionId = item.id;
    this.editingOptionValue = item.value;
  }

  saveEditOption(cat: CategorySection, item: OptionItem) {
    const val = this.editingOptionValue.trim();
    if (!val) return;
    if (val === item.value) { this.cancelEditOption(); return; }
    this.optionService.update(item.id, { ...item, value: val })
      .subscribe(updated => {
        Object.assign(item, updated);
        this.editingOptionId = null;
        this.editingOptionValue = '';
      });
  }

  cancelEditOption() {
    this.editingOptionId = null;
    this.editingOptionValue = '';
  }

  deleteOption(cat: CategorySection, item: OptionItem) {
    this.optionService.delete(item.id).subscribe({
      next: () => {
        cat.items = cat.items.filter(i => i.id !== item.id);
      },
      error: err => {
        if (err.status === 409) {
          item.isInUse = true; // sync with backend cache
          this.errorMsg = err.error?.error ?? 'Cannot delete: item is used in one or more orders.';
        } else {
          this.errorMsg = 'An error occurred while deleting.';
        }
        setTimeout(() => this.errorMsg = '', 5000);
      }
    });
  }

  recheckOption(item: OptionItem) {
    this.optionService.recheck(item.id).subscribe(updated => Object.assign(item, updated));
  }

  // ─── Signature Cupcakes ───────────────────────────────────────────────────

  get sortedSignatures(): SignatureCupcake[] {
    return [...this.signatures].sort((a, b) => {
      const aVal = this.getSigSortValue(a);
      const bVal = this.getSigSortValue(b);
      return this.sigSortDir === 'asc' ? aVal.localeCompare(bVal) : bVal.localeCompare(aVal);
    });
  }

  private getSigSortValue(sig: SignatureCupcake): string {
    switch (this.sigSortCol) {
      case 'name':         return (sig.name         ?? '').toLowerCase();
      case 'cupcakeFlavor':return (sig.cupcakeFlavor ?? '').toLowerCase();
      case 'fillingFlavor':return (sig.fillingFlavor ?? '').toLowerCase();
      case 'icingFlavor':  return (sig.icingFlavor   ?? '').toLowerCase();
      case 'isActive':     return sig.isActive ? 'a' : 'b';
      default:             return '';
    }
  }

  sortSig(col: string) {
    if (this.sigSortCol === col) {
      this.sigSortDir = this.sigSortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sigSortCol = col;
      this.sigSortDir = 'asc';
    }
  }

  loadSignatures() {
    this.sigService.getAll().subscribe(s => this.signatures = s);
  }

  startAddSig() {
    this.newSig = { isActive: true, sortOrder: this.signatures.length };
    this.addingSig = true;
    this.editingSig = null;
  }

  cancelAddSig() {
    this.addingSig = false;
    this.newSig = {};
  }

  saveSig() {
    if (!this.newSig.name?.trim()) return;
    this.sigService.create(this.newSig).subscribe(created => {
      this.signatures.push(created);
      this.addingSig = false;
      this.newSig = {};
    });
  }

  startEditSig(sig: SignatureCupcake) {
    this.editingSig = { ...sig };
    this.addingSig = false;
  }

  cancelEditSig() {
    this.editingSig = null;
  }

  saveEditSig() {
    if (!this.editingSig || !this.editingSig.name?.trim()) return;
    this.sigService.update(this.editingSig.id, this.editingSig).subscribe(updated => {
      const idx = this.signatures.findIndex(s => s.id === updated.id);
      if (idx >= 0) this.signatures[idx] = updated;
      this.editingSig = null;
    });
  }

  deleteSig(sig: SignatureCupcake) {
    this.sigService.delete(sig.id).subscribe(() => {
      this.signatures = this.signatures.filter(s => s.id !== sig.id);
    });
  }

  toggleSigActive(sig: SignatureCupcake) {
    this.sigService.update(sig.id, { ...sig, isActive: !sig.isActive })
      .subscribe(updated => Object.assign(sig, updated));
  }

}
