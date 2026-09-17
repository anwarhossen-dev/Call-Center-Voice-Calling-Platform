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

  isRefreshing = signal<boolean>(false);

  async refreshRecordings() {
    this.isRefreshing.set(true);
    await this.audioRecorder.loadAllPersistedRecordings();
    setTimeout(() => {
      this.isRefreshing.set(false);
    }, 600);
  }

  formatDuration(seconds: number): string {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  }

  pageSize = signal<number>(8);
  currentPage = signal<number>(1);

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

  totalPages(): number {
    return Math.ceil(this.filteredRecordings().length / this.pageSize()) || 1;
  }

  paginatedRecordings(): RecordedCallItem[] {
    const list = this.filteredRecordings();
    const start = (this.currentPage() - 1) * this.pageSize();
    return list.slice(start, start + this.pageSize());
  }

  nextPage() {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.update(p => p + 1);
    }
  }

  prevPage() {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
    }
  }

  setPage(p: number) {
    if (p >= 1 && p <= this.totalPages()) {
      this.currentPage.set(p);
    }
  }
}
