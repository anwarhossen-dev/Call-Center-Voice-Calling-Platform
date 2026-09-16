import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AudioRecordingService, RecordedCallItem } from '../../core/services/audio-recording.service';
import { TelephonyService } from '../../core/services/telephony.service';

@Component({
  selector: 'app-recordings-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './recordings-page.component.html',
  styleUrl: './recordings-page.component.css'
})
export class RecordingsPageComponent {
  audioRecorder = inject(AudioRecordingService);
  telephony = inject(TelephonyService);

  searchQuery = signal<string>('');
  selectedFilter = signal<'all' | 'today' | 'outbound' | 'inbound'>('all');

  formatDuration(seconds: number): string {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  }

  filteredRecordings(): RecordedCallItem[] {
    const q = this.searchQuery().toLowerCase().trim();
    return this.audioRecorder.recordedCalls().filter(rec => {
      const matchText = !q ||
        rec.customerName.toLowerCase().includes(q) ||
        rec.phoneNumber.toLowerCase().includes(q) ||
        rec.callUuid.toLowerCase().includes(q);
      return matchText;
    });
  }
}
