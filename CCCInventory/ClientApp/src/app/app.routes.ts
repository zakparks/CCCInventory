import { Routes } from '@angular/router';
import { HomeComponent } from './components/home/home.component';
import { AllOrdersComponent } from './components/all-orders/all-orders.component';
import { EditOrderComponent } from './components/edit-order/edit-order.component';
import { CalendarComponent } from './components/calendar/calendar.component';
import { BakeSheetComponent } from './components/bake-sheet/bake-sheet.component';
import { ManagementComponent } from './components/management/management.component';

export const routes: Routes = [
  { path: '', component: HomeComponent, pathMatch: 'full' },
  { path: 'edit-order', component: EditOrderComponent },
  { path: 'all-orders', component: AllOrdersComponent },
  { path: 'calendar', component: CalendarComponent },
  { path: 'bake-sheet', component: BakeSheetComponent },
  { path: 'management', component: ManagementComponent }
];
