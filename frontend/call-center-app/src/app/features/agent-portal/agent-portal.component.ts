import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TelephonyService, QuickContact, CallHistoryItem } from '../../core/services/telephony.service';
import { AudioRecordingService } from '../../core/services/audio-recording.service';

@Component({
  selector: 'app-agent-portal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './agent-portal.component.html',
  styleUrl: './agent-portal.component.css'
})
export class AgentPortalComponent {
  telephony = inject(TelephonyService);
  audioRecorder = inject(AudioRecordingService);

  dialInput = '';
  selectedDisposition = 'Resolved';
  noteContent = 'Customer query resolved. Fiber connection stable.';
  activeTab: 'info' | 'history' | 'notes' = 'info';
  showInCallKeypad = false;
  lastDtmfKey = '';

  // Transfer Modal State
  showTransferModal = false;
  selectedTransferExt = '1002';
  transferMode: 'Blind' | 'Attended' = 'Attended';

  // New Note
  newNoteText = '';

  dispositionOptions = [
    { val: 'Resolved', label: '✓ Resolved' },
    { val: 'Cancelled', label: '✕ Cancelled by Agent' },
    { val: 'NoAnswer', label: '⌛ No Answer / Busy' },
    { val: 'Callback', label: '📞 Callback Needed' },
    { val: 'Escalated', label: '↗ Escalated Tier 2' },
    { val: 'Interested', label: '★ Sales: Interested' },
    { val: 'NotInterested', label: '✕ Not Interested' }
  ];

  keypadKeys = [
    { digit: '1', sub: '. / ~' },
    { digit: '2', sub: 'ABC' },
    { digit: '3', sub: 'DEF' },
    { digit: '4', sub: 'GHI' },
    { digit: '5', sub: 'JKL' },
    { digit: '6', sub: 'MNO' },
    { digit: '7', sub: 'PQRS' },
    { digit: '8', sub: 'TUV' },
    { digit: '9', sub: 'WXYZ' },
    { digit: '*', sub: 'Tone' },
    { digit: '0', sub: '+' },
    { digit: '#', sub: 'Send' }
  ];

  pressKey(digit: string) {
    this.dialInput += digit;
    this.telephony.playDtmf(digit);
  }

  sendInCallDtmf(digit: string) {
    this.lastDtmfKey = digit;
    this.telephony.playDtmf(digit);
    setTimeout(() => { this.lastDtmfKey = ''; }, 1500);
  }

  onInputChange() {
    if (this.dialInput) {
      const lastChar = this.dialInput.slice(-1);
      if (/[0-9*#+]/.test(lastChar)) {
        this.telephony.playDtmf(lastChar);
      }
    }
  }

  backspace() {
    this.dialInput = this.dialInput.slice(0, -1);
  }

  clearInput() {
    this.dialInput = '';
  }

  dialCurrentNumber() {
    const num = this.dialInput.trim();
    if (num) {
      this.telephony.originateCall(num);
    }
  }

  selectQuickContact(contact: QuickContact) {
    this.dialInput = contact.phone;
    this.telephony.originateCall(contact.phone);
  }

  callbackNumber(number: string) {
    this.dialInput = number;
    this.telephony.originateCall(number);
  }

  openTransferModal() {
    this.selectedTransferExt = '1002';
    this.showTransferModal = true;
  }

  confirmTransfer() {
    if (this.selectedTransferExt.trim()) {
      this.telephony.transferCall(this.selectedTransferExt.trim(), this.transferMode);
      this.showTransferModal = false;
    }
  }

  addSnippet(snippet: string) {
    if (!this.noteContent.includes(snippet)) {
      this.noteContent = this.noteContent ? `${this.noteContent} ${snippet}` : snippet;
    }
  }

  addCustomerNote() {
    if (this.newNoteText.trim()) {
      this.telephony.addCustomerNote(this.newNoteText.trim());
      this.newNoteText = '';
    }
  }

  formatDuration(seconds: number): string {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  }

  saveDisposition() {
    this.telephony.saveDisposition(this.selectedDisposition, this.noteContent);
    this.dialInput = '';
    this.noteContent = 'Customer query resolved. Fiber connection stable.';
  }
}
