import { Component, inject, signal, computed } from '@angular/core';
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

  userInitials = computed(() => {
    const name = this.auth.userDisplayName();
    if (!name || name === 'Guest') return 'SV';
    const parts = name.trim().split(/\s+/);
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.slice(0, 2).toUpperCase();
  });

  onStatusChange(event: Event) {
    const target = event.target as HTMLSelectElement;
    this.telephony.setAgentStatus(target.value as any);
  }

  toggleCapabilities() {
    this.showCapabilitiesModal.update(v => !v);
  }
}
