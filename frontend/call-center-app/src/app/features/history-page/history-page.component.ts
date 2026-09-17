import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TelephonyService, CallHistoryItem } from '../../core/services/telephony.service';
import { AuthService } from '../../core/services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-history-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './history-page.component.html',
  styleUrl: './history-page.component.css'
})
export class HistoryPageComponent implements OnInit {
  telephony = inject(TelephonyService);
  auth = inject(AuthService);
  router = inject(Router);

  searchQuery = signal<string>('');
  selectedFilter = signal<'all' | 'completed' | 'cancelled' | 'outbound' | 'inbound'>('all');
  isRefreshing = signal<boolean>(false);

  ngOnInit() {
    this.refreshHistory();
  }

  refreshHistory() {
    this.isRefreshing.set(true);
    this.telephony.loadCallHistory();
    setTimeout(() => this.isRefreshing.set(false), 600);
  }

  callCustomer(phoneNumber: string) {
    this.telephony.originateCall(phoneNumber);
    this.router.navigate(['/agent']);
  }

  formatDuration(seconds: number): string {
    if (!seconds || seconds <= 0) return '00:00';
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  }

  filteredCalls(): CallHistoryItem[] {
    const q = this.searchQuery().toLowerCase().trim();
    const filter = this.selectedFilter();

    return this.telephony.callHistory().filter(call => {
      // 1. Text filter
      const matchText = !q ||
        (call.number && call.number.toLowerCase().includes(q)) ||
        (call.customerName && call.customerName.toLowerCase().includes(q)) ||
        (call.disposition && call.disposition.toLowerCase().includes(q)) ||
        (call.notes && call.notes.toLowerCase().includes(q)) ||
        (call.callUuid && call.callUuid.toLowerCase().includes(q));

      if (!matchText) return false;

      // 2. Tab filter
      if (filter === 'all') return true;
      if (filter === 'completed') {
        return call.status === 'Completed' || (call.disposition && call.disposition !== 'CANCELLED');
      }
      if (filter === 'cancelled') {
        return call.status === 'Abandoned' || call.disposition === 'CANCELLED';
      }
      if (filter === 'outbound') {
        return call.direction === 'Outbound';
      }
      if (filter === 'inbound') {
        return call.direction === 'Inbound';
      }
      return true;
    });
  }

  pageSize = signal<number>(10);
  currentPage = signal<number>(1);

  totalPages(): number {
    return Math.ceil(this.filteredCalls().length / this.pageSize()) || 1;
  }

  paginatedCalls(): CallHistoryItem[] {
    const list = this.filteredCalls();
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

  getCompletedCount(): number {
    return this.telephony.callHistory().filter(c => c.status === 'Completed' || (c.disposition && c.disposition !== 'CANCELLED')).length;
  }

  getCancelledCount(): number {
    return this.telephony.callHistory().filter(c => c.status === 'Abandoned' || c.disposition === 'CANCELLED').length;
  }
}
