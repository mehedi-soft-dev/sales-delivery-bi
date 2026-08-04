import { Component, computed, input } from '@angular/core';
import { Skeleton } from 'primeng/skeleton';

@Component({
  selector: 'app-loading-skeleton',
  imports: [Skeleton],
  templateUrl: './loading-skeleton.html',
  styleUrl: './loading-skeleton.css',
})
export class LoadingSkeleton {
  /** Number of KPI-card-shaped placeholders in the top row. */
  readonly kpiCount = input(4);
  /** One block per grid/chart placeholder below the KPI row, each `Npx` tall. */
  readonly contentBlockHeights = input<number[]>([220]);

  protected readonly kpiPlaceholders = computed(() => Array.from({ length: this.kpiCount() }, (_, i) => i));
}
