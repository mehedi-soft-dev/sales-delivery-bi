import { Component, input } from '@angular/core';
import { Home } from '@primeicons/angular/home';
import { Folder } from '@primeicons/angular/folder';
import { ChartLine } from '@primeicons/angular/chart-line';
import { Percentage } from '@primeicons/angular/percentage';
import { Clock } from '@primeicons/angular/clock';
import { ShoppingCart } from '@primeicons/angular/shopping-cart';
import { Truck } from '@primeicons/angular/truck';
import { Receipt } from '@primeicons/angular/receipt';
import { Reply } from '@primeicons/angular/reply';
import { ChartPie } from '@primeicons/angular/chart-pie';
import { Shield } from '@primeicons/angular/shield';
import { Users } from '@primeicons/angular/users';
import { Sitemap } from '@primeicons/angular/sitemap';
import { Key } from '@primeicons/angular/key';
import type { NavIconName } from '../../../layout/nav-items';

@Component({
  selector: 'app-page-header',
  imports: [
    Home,
    Folder,
    ChartLine,
    Percentage,
    Clock,
    ShoppingCart,
    Truck,
    Receipt,
    Reply,
    ChartPie,
    Shield,
    Users,
    Sitemap,
    Key,
  ],
  templateUrl: './page-header.html',
  styleUrl: './page-header.css',
})
export class PageHeader {
  readonly title = input.required<string>();
  readonly subtitle = input<string | null>(null);
  readonly icon = input<NavIconName | null>(null);
}
