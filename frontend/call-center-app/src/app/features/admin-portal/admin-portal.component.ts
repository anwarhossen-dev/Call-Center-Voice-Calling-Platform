import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TelephonyService, CampaignItem } from '../../core/services/telephony.service';
import { AudioRecordingService } from '../../core/services/audio-recording.service';

@Component({
  selector: 'app-admin-portal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-portal.component.html',
  styleUrl: './admin-portal.component.css'
})
export class AdminPortalComponent implements OnInit {
  telephony = inject(TelephonyService);
  audioRecorder = inject(AudioRecordingService);

  activeSubSection: 'campaigns' | 'agents' | 'queues' | 'recordings' | 'settings' = 'campaigns';

  // Twilio Integration State
  twilioAccountSid = '';
  twilioAuthToken = '';
  twilioFromNumber = '';
  twilioEnabled = false;
  twilioSaveStatus = '';

  // New Campaign Modal
  showNewCampaignModal = false;
  newCampName = '';
  newCampType: 'Inbound' | 'Outbound' = 'Outbound';
  newCampAgents = 12;

  ngOnInit() {
    this.loadTwilioConfig();
  }

  loadTwilioConfig() {
    fetch('http://localhost:5000/api/calls/twilio/config')
      .then(r => r.json())
      .then(data => {
        if (data) {
          this.twilioAccountSid = data.accountSid || '';
          this.twilioFromNumber = data.fromPhoneNumber || '';
          this.twilioEnabled = !!data.enabled;
        }
      })
      .catch(() => {});
  }

  saveTwilioConfig() {
    this.twilioSaveStatus = 'Saving...';
    fetch('http://localhost:5000/api/calls/twilio/config', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        accountSid: this.twilioAccountSid.trim(),
        authToken: this.twilioAuthToken.trim(),
        fromPhoneNumber: this.twilioFromNumber.trim(),
        enabled: this.twilioEnabled
      })
    })
      .then(r => r.json())
      .then(() => {
        this.twilioSaveStatus = '✓ Twilio Settings Saved';
        setTimeout(() => { this.twilioSaveStatus = ''; }, 4000);
      })
      .catch(err => {
        this.twilioSaveStatus = 'Error: ' + err.message;
      });
  }

  formatDuration(seconds: number): string {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  }

  openNewCampaignModal() {
    this.newCampName = '';
    this.newCampType = 'Outbound';
    this.newCampAgents = 10;
    this.showNewCampaignModal = true;
  }

  submitNewCampaign() {
    if (this.newCampName.trim()) {
      const camp: CampaignItem = {
        name: this.newCampName.trim(),
        type: this.newCampType,
        status: 'Active',
        agents: this.newCampAgents || 8
      };
      this.telephony.addCampaign(camp);
      this.showNewCampaignModal = false;
    }
  }

  toggleAudioMock() {
    this.telephony.playDtmf('1');
  }
}
