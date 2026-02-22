import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { BakeSheetService, BakeSheetResponse } from '../../services/bake-sheet.service';

interface GroupedCount {
  label: string;
  count: number;
}

interface CakeTotalsRow {
  flavor: string;
  size: string;
  shape: string;
  count: number;
}

@Component({
  selector: 'app-bake-sheet',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './bake-sheet.component.html',
  styles: [`
    .bake-section { border: 1px solid #dee2e6; border-radius: 6px; margin-bottom: 1.5rem; }
    .bake-section-header {
      background: #f8f9fa;
      padding: 8px 14px;
      font-weight: 700;
      font-size: .85rem;
      text-transform: uppercase;
      letter-spacing: .05em;
      border-bottom: 1px solid #dee2e6;
      border-radius: 6px 6px 0 0;
      display: flex;
      align-items: center;
      justify-content: space-between;
      cursor: default;
    }
    .bake-section-header.clickable { cursor: pointer; user-select: none; }
    .bake-section-header.clickable:hover { background: #e9ecef; }
    .bake-section-header.collapsed { border-bottom: none; border-radius: 6px; }
    .collapse-indicator { color: #6c757d; font-size: .9rem; }
    .sortable-th { cursor: pointer; user-select: none; white-space: nowrap; }
    .sortable-th:hover { background: #f0f0f0; }
    .sort-indicator { font-size: .7rem; color: #6c757d; margin-left: 2px; }
    .bake-section-body { padding: 10px 14px; }
    .order-row { border-bottom: 1px solid #f0f0f0; padding: 8px 0; }
    .order-row:last-child { border-bottom: none; }
    .item-line { font-size: .88rem; color: #495057; padding-left: 1.25rem; }
    .totals-table { width: 100%; border-collapse: collapse; font-size: .9rem; }
    .totals-table td { padding: 4px 8px; }
    .totals-table tr:not(:last-child) td { border-bottom: 1px solid #f0f0f0; }
    .totals-table .count-col { text-align: right; font-weight: 600; width: 60px; }
    .totals-table tfoot td { border-top: 2px solid #dee2e6 !important; font-weight: 700; }
    .no-orders { color: #6c757d; font-style: italic; padding: 12px 0; }
    @media print {
      .no-print { display: none !important; }
      .bake-section { break-inside: avoid; }
      body { font-size: 11pt; }
    }
  `]
})
export class BakeSheetComponent implements OnInit {
  weekOf = this.isoDate(new Date());
  data: BakeSheetResponse | null = null;
  loading = false;
  ordersExpanded = false;

  cakeSortCol = 'flavor';
  cakeSortDir: 'asc' | 'desc' = 'asc';

  private readonly sizeRank: Record<string, number> = {
    'Micro': 0, '6"': 1, '8"': 2, '10"': 3, '12"': 4, 'Quarter Sheet': 5, 'Half Sheet': 6
  };

  constructor(private service: BakeSheetService, private router: Router) { }

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading = true;
    this.service.getBakeSheet(this.weekOf).subscribe(d => {
      this.data = d;
      this.loading = false;
    });
  }

  prevWeek() {
    const d = new Date(this.weekOf + 'T00:00:00');
    d.setDate(d.getDate() - 7);
    this.weekOf = this.isoDate(d);
    this.load();
  }

  nextWeek() {
    const d = new Date(this.weekOf + 'T00:00:00');
    d.setDate(d.getDate() + 7);
    this.weekOf = this.isoDate(d);
    this.load();
  }

  thisWeek() {
    this.weekOf = this.isoDate(new Date());
    this.load();
  }

  editOrder(orderNumber: number) {
    this.router.navigate(['edit-order'], { queryParams: { orderNumber } });
  }

  sortCake(col: string) {
    if (this.cakeSortCol === col) {
      this.cakeSortDir = this.cakeSortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.cakeSortCol = col;
      this.cakeSortDir = 'asc';
    }
  }

  print() {
    window.print();
  }

  get weekLabel(): string {
    if (!this.data) return '';
    // API returns full ISO strings — parse directly, no appending needed
    const start = new Date(this.data.weekStart);
    const end = new Date(this.data.weekEnd);
    return `${start.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} – ${end.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}`;
  }

  // Cakes: group by flavor + tierSize + cakeShape; count = sum of numTierLayers
  get cakeTotals(): CakeTotalsRow[] {
    if (!this.data) return [];
    const map = new Map<string, { flavor: string; size: string; shape: string; count: number }>();
    for (const order of this.data.orders) {
      for (const c of order.cakes ?? []) {
        const key = `${c.cakeFlavor}|${c.tierSize}|${c.cakeShape}`;
        const existing = map.get(key);
        if (existing) {
          existing.count += (c.numTierLayers ?? 1);
        } else {
          map.set(key, { flavor: c.cakeFlavor!, size: c.tierSize!, shape: c.cakeShape!, count: c.numTierLayers ?? 1 });
        }
      }
    }
    return [...map.values()];
  }

  get sortedCakeTotals(): CakeTotalsRow[] {
    const dir = this.cakeSortDir === 'asc' ? 1 : -1;
    return [...this.cakeTotals].sort((a, b) => {
      switch (this.cakeSortCol) {
        case 'flavor': {
          const fc = a.flavor.localeCompare(b.flavor) * dir;
          return fc !== 0 ? fc : (this.sizeRank[a.size] ?? 99) - (this.sizeRank[b.size] ?? 99);
        }
        case 'size': {
          const sc = ((this.sizeRank[a.size] ?? 99) - (this.sizeRank[b.size] ?? 99)) * dir;
          return sc !== 0 ? sc : a.flavor.localeCompare(b.flavor);
        }
        case 'shape':
          return a.shape.localeCompare(b.shape) * dir;
        case 'count':
          return (a.count - b.count) * dir;
        default:
          return 0;
      }
    });
  }

  // Cupcakes: group by flavor only; count = sum of quantity
  get cupcakeTotals(): GroupedCount[] {
    if (!this.data) return [];
    const map = new Map<string, number>();
    for (const order of this.data.orders) {
      for (const c of order.cupcakes ?? []) {
        map.set(c.cupcakeFlavor!, (map.get(c.cupcakeFlavor!) ?? 0) + (c.cupcakeQuantity ?? 0));
      }
    }
    return [...map.entries()].map(([label, count]) => ({ label, count }));
  }

  get cookieTotals(): GroupedCount[] {
    if (!this.data) return [];
    const map = new Map<string, number>();
    for (const order of this.data.orders) {
      for (const c of order.cookies ?? []) {
        map.set(c.cookieType!, (map.get(c.cookieType!) ?? 0) + (c.cookieQuantity ?? 0));
      }
    }
    return [...map.entries()].map(([label, count]) => ({ label, count }));
  }

  get pupcakeTotals(): GroupedCount[] {
    if (!this.data) return [];
    const map = new Map<string, number>();
    for (const order of this.data.orders) {
      for (const p of order.pupcakes ?? []) {
        map.set(p.pupcakeSize!, (map.get(p.pupcakeSize!) ?? 0) + (p.pupcakeQuantity ?? 0));
      }
    }
    return [...map.entries()].map(([label, count]) => ({ label, count }));
  }


  formatDate(d: string | Date | undefined): string {
    if (!d) return '';
    return new Date(d).toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });
  }

  private isoDate(d: Date): string {
    return d.toISOString().split('T')[0];
  }
}
