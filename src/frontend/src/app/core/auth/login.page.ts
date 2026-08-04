import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Button } from 'primeng/button';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Password } from 'primeng/password';
import { ChartBar } from '@primeicons/angular/chart-bar';
import { ChartLine } from '@primeicons/angular/chart-line';
import { Percentage } from '@primeicons/angular/percentage';
import { Clock } from '@primeicons/angular/clock';
import { Envelope } from '@primeicons/angular/envelope';
import { Lock } from '@primeicons/angular/lock';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-login-page',
  imports: [
    FormsModule,
    Button,
    IconField,
    InputIcon,
    InputText,
    Password,
    Message,
    ChartBar,
    ChartLine,
    Percentage,
    Clock,
    Envelope,
    Lock,
  ],
  templateUrl: './login.page.html',
  styleUrl: './login.page.css',
})
export class LoginPage {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected email = '';
  protected password = '';
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  onSubmit(): void {
    if (!this.email || !this.password || this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    this.authService.login(this.email, this.password).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/pipeline';
        void this.router.navigateByUrl(returnUrl);
      },
      error: () => {
        this.submitting.set(false);
        this.errorMessage.set('Invalid email or password. Please try again.');
      },
    });
  }
}
