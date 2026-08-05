import { Component, model } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ToggleSwitch } from 'primeng/toggleswitch';

@Component({
  selector: 'app-high-risk-only-toggle',
  imports: [FormsModule, ToggleSwitch],
  templateUrl: './high-risk-only-toggle.html',
  styleUrl: './high-risk-only-toggle.css',
})
export class HighRiskOnlyToggle {
  readonly checked = model(false);
}
