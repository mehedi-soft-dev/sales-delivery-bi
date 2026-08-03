import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { finalize } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class LoadingIndicatorService {
  private readonly pendingRequests = signal(0);
  readonly isLoading = computed(() => this.pendingRequests() > 0);

  requestStarted(): void {
    this.pendingRequests.update((count) => count + 1);
  }

  requestFinished(): void {
    this.pendingRequests.update((count) => Math.max(0, count - 1));
  }
}

export const loadingIndicatorInterceptor: HttpInterceptorFn = (req, next) => {
  const loadingIndicator = inject(LoadingIndicatorService);
  loadingIndicator.requestStarted();
  return next(req).pipe(finalize(() => loadingIndicator.requestFinished()));
};
