import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
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
  styleUrls: ['./bake-sheet.component.css']
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

  // Golden-angle hue rotation: guarantees visually distinct colors with no repeats
  private orderColor(index: number): string {
    const hue = Math.round((index * 137.508) % 360);
    return `hsl(${hue}, 55%, 88%)`;
  }

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

  constructor(private service: BakeSheetService) { }

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

    // Assign colors only to orders whose rows span more than one distinct flavor.
    // Same-flavor multi-layer cakes already cluster together when sorted — no color needed.
    const orderFlavors = new Map<number, Set<string>>();
    for (const r of bake) {
      if (!orderFlavors.has(r.orderNumber)) orderFlavors.set(r.orderNumber, new Set());
      orderFlavors.get(r.orderNumber)!.add(r.flavor);
    }
    const colorMap = new Map<number, string>();
    let ci = 0;
    for (const [num, flavors] of orderFlavors) {
      if (flavors.size > 1) colorMap.set(num, this.orderColor(ci++));
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
