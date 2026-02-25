import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { BakeSheetService, BakeSheetResponse } from '../../services/bake-sheet.service';

interface BakeRow {
  orderNumber: number;
  sizeOrQty: string;
  flavor: string;
  dayOfWeek: string;
  notes: string;
}

@Component({
  selector: 'app-bake-sheet',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './bake-sheet.component.html',
  styles: [`
    .bake-table {
      width: 100%;
      border-collapse: collapse;
      font-size: .9rem;
      margin-bottom: 0;
    }
    .bake-table th {
      background: #f8f9fa;
      font-size: .78rem;
      text-transform: uppercase;
      letter-spacing: .05em;
      font-weight: 700;
      padding: 6px 8px;
      border: 1px solid #dee2e6;
    }
    .bake-table td {
      padding: 5px 8px;
      border-bottom: 1px solid #efefef;
      border-left: 1px solid #efefef;
      border-right: 1px solid #efefef;
    }
    .bake-table tbody tr:hover td { background-color: rgba(0,0,0,.02); }
    .section-title {
      font-size: .82rem;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: .06em;
      color: #495057;
      margin: 1.5rem 0 .35rem;
      padding-bottom: .25rem;
      border-bottom: 2px solid #dee2e6;
    }
    @media print {
      .bake-table td, .bake-table th {
        border: 1px solid #555 !important;
        /* Force background colors (day highlights, order colors) to print */
        -webkit-print-color-adjust: exact !important;
        print-color-adjust: exact !important;
      }
      .section-title { margin-top: .8rem; }
    }
  `]
})
export class BakeSheetComponent implements OnInit {
  weekOf = this.isoDate(new Date());
  data: BakeSheetResponse | null = null;
  loading = false;

  // Processed row data — recomputed after each load()
  bakeRows: BakeRow[] = [];
  cookieRows: BakeRow[] = [];
  pupcakeRows: BakeRow[] = [];
  otherRows: BakeRow[] = [];
  orderColorMap = new Map<number, string>();

  private readonly ORDER_COLORS = [
    '#fff3cd', '#cff4fc', '#d1e7dd', '#f8d7da',
    '#e2d9f3', '#fde6cc', '#d3e3f5', '#fce4ec'
  ];

  private readonly DAY_COLORS: Record<string, string> = {
    'Monday':    '#ffcccc',
    'Tuesday':   '#ffd9b3',
    'Wednesday': '#ffff99',
    'Thursday':  '#ccffcc',
    'Friday':    '#cce5ff',
    'Saturday':  '#e0ccff',
  };

  // Thu-first day ordering for sorting within same flavor
  private readonly BAKE_DAY_ORDER =
    ['Thursday', 'Friday', 'Saturday', 'Sunday', 'Monday', 'Tuesday', 'Wednesday'];

  constructor(private service: BakeSheetService, private router: Router) { }

  ngOnInit() { this.load(); }

  load() {
    this.loading = true;
    this.service.getBakeSheet(this.weekOf).subscribe(d => {
      this.data = d;
      this.processData();
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

  print() { window.print(); }

  get weekLabel(): string {
    if (!this.data) return '';
    const start = new Date(this.data.weekStart);
    const end = new Date(this.data.weekEnd);
    const fmt = (d: Date, year = false) =>
      d.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric', ...(year ? { year: 'numeric' } : {}) });
    return `${fmt(start)} – ${fmt(end, true)}`;
  }

  dayColor(day: string): string {
    return this.DAY_COLORS[day] ?? '';
  }

  // ── Row generation ──────────────────────────────────────────────────────

  private processData() {
    if (!this.data) {
      this.bakeRows = this.cookieRows = this.pupcakeRows = this.otherRows = [];
      this.orderColorMap = new Map();
      return;
    }

    const bake: BakeRow[] = [];
    const cookies: BakeRow[] = [];
    const pupcakes: BakeRow[] = [];
    const other: BakeRow[] = [];

    for (const order of this.data.orders) {
      const day = this.toDayName(order.orderDateTime);

      // Cakes — one row per layer
      for (const cake of order.cakes ?? []) {
        const displaySize = this.cakeDisplaySize(cake);
        const notes = this.cakeNotes(cake);
        for (const flavor of this.expandCakeLayers(cake)) {
          bake.push({ orderNumber: order.orderNumber!, sizeOrQty: displaySize, flavor, dayOfWeek: day, notes });
        }
      }

      // Cupcakes — one row per cupcake line
      for (const cup of order.cupcakes ?? []) {
        bake.push({
          orderNumber: order.orderNumber!,
          sizeOrQty: String(cup.cupcakeQuantity ?? ''),
          flavor: cup.cupcakeFlavor ?? '',
          dayOfWeek: day,
          notes: cup.cupcakeSize ?? ''
        });
      }

      // Cookies
      for (const c of order.cookies ?? []) {
        cookies.push({
          orderNumber: order.orderNumber!,
          sizeOrQty: String(c.cookieQuantity ?? ''),
          flavor: c.cookieType ?? '',
          dayOfWeek: day,
          notes: c.cookieSize ?? ''
        });
      }

      // Pupcakes
      for (const p of order.pupcakes ?? []) {
        pupcakes.push({
          orderNumber: order.orderNumber!,
          sizeOrQty: String(p.pupcakeQuantity ?? ''),
          flavor: p.pupcakeSize ?? '',
          dayOfWeek: day,
          notes: ''
        });
      }

      // Other items
      for (const item of order.otherItems ?? []) {
        other.push({
          orderNumber: order.orderNumber!,
          sizeOrQty: item.name ?? '',
          flavor: item.item ?? '',
          dayOfWeek: day,
          notes: ''
        });
      }
    }

    // Sort cakes+cupcakes by flavor, then by bake-week day position
    bake.sort((a, b) => {
      const fc = a.flavor.localeCompare(b.flavor);
      if (fc !== 0) return fc;
      return this.BAKE_DAY_ORDER.indexOf(a.dayOfWeek) - this.BAKE_DAY_ORDER.indexOf(b.dayOfWeek);
    });

    // Assign pastel colors to order numbers that produce multiple bake rows
    const rowCounts = new Map<number, number>();
    for (const r of bake) rowCounts.set(r.orderNumber, (rowCounts.get(r.orderNumber) ?? 0) + 1);
    const colorMap = new Map<number, string>();
    let ci = 0;
    for (const [num, count] of rowCounts) {
      if (count > 1) colorMap.set(num, this.ORDER_COLORS[ci++ % this.ORDER_COLORS.length]);
    }

    this.bakeRows = bake;
    this.cookieRows = cookies;
    this.pupcakeRows = pupcakes;
    this.otherRows = other;
    this.orderColorMap = colorMap;
  }

  // Expand a cake into a flavor string per layer.
  // Respects layerFlavors JSON, then splitTier, then uniform cakeFlavor.
  private expandCakeLayers(cake: any): string[] {
    const n = Math.max(1, Number(cake.numTierLayers ?? 1));

    if (cake.layerFlavors) {
      try {
        const parsed: string[] = JSON.parse(cake.layerFlavors);
        if (Array.isArray(parsed)) {
          while (parsed.length < n) parsed.push(cake.cakeFlavor ?? '');
          return parsed.slice(0, n).map((f: string) => f || cake.cakeFlavor || '');
        }
      } catch { /* fall through */ }
    }

    const flavor = (cake.splitTier && cake.flavor2)
      ? `${cake.cakeFlavor} / ${cake.flavor2}`
      : (cake.cakeFlavor ?? '');
    return Array(n).fill(flavor);
  }

  // Apply special-case size display rules (7.4)
  private cakeDisplaySize(cake: any): string {
    if (cake.tierSize === 'Micro') return '6"';
    if (cake.tierSize === 'Quarter Sheet') return 'Half Sheet';
    return cake.tierSize ?? '';
  }

  // Pre-populate Notes for special display cases (7.4)
  private cakeNotes(cake: any): string {
    if (cake.tierSize === 'Micro') return 'Micro';
    if (cake.tierSize === 'Quarter Sheet') return cake.splitTier ? 'Cut twice' : 'Cut';
    return '';
  }

  private toDayName(date: string | Date | undefined): string {
    if (!date) return '';
    return new Date(date as any).toLocaleDateString('en-US', { weekday: 'long' });
  }

  private isoDate(d: Date): string {
    return d.toISOString().split('T')[0];
  }
}
