import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TelephonyService, AgentItem } from '../../core/services/telephony.service';

@Component({
  selector: 'app-supervisor-portal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './supervisor-portal.component.html',
  styleUrl: './supervisor-portal.component.css'
})
export class SupervisorPortalComponent {
  telephony = inject(TelephonyService);
  stats = this.telephony.supervisorDashboard;

  formatDuration(seconds: number): string {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  }

  startCoaching(mode: 'SilentSpy' | 'Whisper' | 'BargeIn', agent: AgentItem) {
    this.telephony.startCoaching(mode, agent.name, agent.extension);
  }
}
