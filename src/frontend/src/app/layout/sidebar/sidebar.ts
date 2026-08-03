import { Component, model } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { Drawer } from 'primeng/drawer';
import { ChartLine } from '@primeicons/angular/chart-line';
import { Percentage } from '@primeicons/angular/percentage';
import { Clock } from '@primeicons/angular/clock';

@Component({
  selector: 'app-sidebar',
  imports: [NgTemplateOutlet, RouterLink, RouterLinkActive, Drawer, ChartLine, Percentage, Clock],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css',
})
export class Sidebar {
  readonly mobileOpen = model(false);
}
