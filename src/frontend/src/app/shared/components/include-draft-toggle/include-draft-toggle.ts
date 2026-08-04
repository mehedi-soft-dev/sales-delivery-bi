import { Component, model } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ToggleSwitch } from 'primeng/toggleswitch';

@Component({
  selector: 'app-include-draft-toggle',
  imports: [FormsModule, ToggleSwitch],
  templateUrl: './include-draft-toggle.html',
  styleUrl: './include-draft-toggle.css',
})
export class IncludeDraftToggle {
  readonly checked = model(false);
}
