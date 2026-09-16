import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { TelephonyService } from './core/services/telephony.service';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive
  ],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  telephony = inject(TelephonyService);
  auth = inject(AuthService);
  showCapabilitiesModal = signal<boolean>(false);

  onStatusChange(event: Event) {
    const target = event.target as HTMLSelectElement;
    this.telephony.setAgentStatus(target.value as any);
  }

  toggleCapabilities() {
    this.showCapabilitiesModal.update(v => !v);
  }
}
