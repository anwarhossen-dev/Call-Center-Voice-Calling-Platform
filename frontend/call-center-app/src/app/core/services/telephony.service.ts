import { Injectable, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { AudioToneService } from './audio-tone.service';
import { AudioRecordingService } from './audio-recording.service';

export interface CustomerInfo {
  customerId: string;
  name: string;
  phone: string;
  email: string;
  tier: string;
  crmId: string;
  accountNo?: string;
  address?: string;
  notes: string[];
}

export interface ScreenPopEvent {
  callUuid: string;
  callerNumber: string;
  queueName: string;
  customer: CustomerInfo;
}

export interface CallHistoryItem {
  id: string;
  callUuid?: string;
  number: string;
  callerNumber?: string;
  destinationNumber?: string;
  customerName: string;
  direction: string;
  durationSeconds: number;
  status?: string;
  timestamp: string;
  disposition?: string;
  notes?: string;
}

export interface AgentItem {
  id: string;
  name: string;
  extension: string;
  role: string;
  state: 'Available' | 'OnCall' | 'Break' | 'WrapUp' | 'Offline';
  durationInStateSeconds: number;
  callsHandledToday: number;
}

export interface QueueOverviewItem {
  queueName: string;
  waiting: number;
  longestWait: string;
}

export interface CampaignItem {
  name: string;
  type: string;
  status: string;
  agents: number;
}

export interface QuickContact {
  name: string;
  phone: string;
  tier: string;
  role: string;
  email: string;
}

export interface CoachingSession {
  active: boolean;
  mode: 'SilentSpy' | 'Whisper' | 'BargeIn';
  agentName: string;
  extension: string;
}

@Injectable({
  providedIn: 'root'
})
export class TelephonyService {
  private audio = inject(AudioToneService);
  public audioRecorder = inject(AudioRecordingService);
  private hubConnection?: signalR.HubConnection;
  private ringTimeout?: any;
  private timerInterval?: any;

  // Reactive state signals
  public agentState = signal<'Available' | 'OnCall' | 'Break' | 'WrapUp' | 'Offline'>('Available');
  public callStatus = signal<'Idle' | 'Ringing' | 'Connected' | 'OnHold' | 'WrapUp'>('Idle');
  public activeCallerNumber = signal<string>('');
  public activeCallDuration = signal<number>(0);
  public currentCallUuid = signal<string>('');
  public isMuted = signal<boolean>(false);
  public isOnHold = signal<boolean>(false);
  public isRecording = signal<boolean>(true);
  public saveNotification = signal<string>('');

  // Active Supervisor Coaching Session
  public coachingSession = signal<CoachingSession | null>(null);

  // System Health / Gateway Signals
  public systemHealth = signal({
    btclTrunk: 'Online (E1-Primary)',
    freeSwitch: 'Connected (ESL 8021)',
    signalR: 'Connected',
    latencyMs: 14,
    activeCodec: 'G.711u / Opus WebRTC'
  });

  public quickContacts = signal<QuickContact[]>([
    {
      name: 'Rahim Ahmed',
      phone: '+880 1712 345678',
      tier: 'Platinum VIP',
      role: 'Enterprise Broadband Client',
      email: 'rahim@example.com'
    },
    {
      name: 'Karim Ullah',
      phone: '+880 1819 876543',
      tier: 'Gold Corporate',
      role: 'Trunking Customer',
      email: 'karim@corp.bd'
    },
    {
      name: 'Farzana Yasmin',
      phone: '+880 1912 998877',
      tier: 'Silver Dedicated',
      role: 'SME Fiber Subscriber',
      email: 'farzana@sme.bd'
    },
    {
      name: 'Tier 2 Escalations',
      phone: '1002',
      tier: 'Internal Extension',
      role: 'Senior Technical Agent',
      email: 'escalation@btcl-cc.bd'
    }
  ]);

  public agentsList = signal<AgentItem[]>([
    { id: '1', name: 'Rahim Ahmed', extension: '1001', role: 'Support Specialist', state: 'Available', durationInStateSeconds: 1420, callsHandledToday: 24 },
    { id: '2', name: 'Fatima Khan', extension: '1002', role: 'Senior Technical Agent', state: 'OnCall', durationInStateSeconds: 215, callsHandledToday: 31 },
    { id: '3', name: 'Tariqul Islam', extension: '1003', role: 'Billing & Payments', state: 'Available', durationInStateSeconds: 840, callsHandledToday: 19 },
    { id: '4', name: 'Salma Akter', extension: '1004', role: 'Corporate Sales Lead', state: 'Break', durationInStateSeconds: 610, callsHandledToday: 28 },
    { id: '5', name: 'Nazmul Hossain', extension: '1005', role: 'Retention Expert', state: 'WrapUp', durationInStateSeconds: 45, callsHandledToday: 22 }
  ]);

  public callHistory = signal<CallHistoryItem[]>([]);

  public currentCustomer = signal<CustomerInfo>({
    customerId: 'C1001',
    name: 'Ready to Dial',
    phone: '',
    email: '',
    tier: 'Standard',
    crmId: 'CRM-READY',
    accountNo: 'ACC-BTCL-001',
    address: 'Gulshan-2, Dhaka 1212',
    notes: ['Dial any phone number or choose a contact from quick dial to begin.']
  });

  public supervisorDashboard = signal({
    totalCalls: 248,
    answered: 226,
    missed: 22,
    avgHandleTime: '04:32',
    agentBreakdown: {
      available: 28,
      onCall: 12,
      break: 5,
      acw: 3,
      offline: 2
    },
    queues: [
      { queueName: 'Sales Campaign', waiting: 12, longestWait: '00:01:45' },
      { queueName: 'Technical Support', waiting: 8, longestWait: '00:02:12' },
      { queueName: 'Billing & Invoicing', waiting: 5, longestWait: '00:03:20' },
      { queueName: 'VIP Retention', waiting: 2, longestWait: '00:00:50' }
    ] as QueueOverviewItem[]
  });

  public campaigns = signal<CampaignItem[]>([
    { name: 'National Fiber Sales', type: 'Outbound', status: 'Active', agents: 25 },
    { name: 'General Helpdesk Support', type: 'Inbound', status: 'Active', agents: 15 },
    { name: 'Corporate Retention', type: 'Outbound', status: 'Paused', agents: 10 },
    { name: 'Broadband Satisfaction Survey', type: 'Outbound', status: 'Active', agents: 8 }
  ]);

  constructor() {
    this.startCallTimer();
    this.initializeSignalR();
    this.loadCallHistory();
    this.loadAgentsFromDb();
    this.loadCampaignsFromDb();
    this.loadQueuesFromDb();
  }

  public loadAgentsFromDb() {
    fetch('http://localhost:5000/api/agents')
      .then(res => res.json())
      .then((data: any[]) => {
        if (Array.isArray(data) && data.length > 0) {
          this.agentsList.set(data.map(a => ({
            id: a.id || a.agentId,
            name: a.displayName || a.name || 'Agent',
            extension: a.extension || '1000',
            role: a.role || 'Call Center Agent',
            state: (a.state || 'Available') as any,
            durationInStateSeconds: a.stateChangedAt ? Math.floor((Date.now() - new Date(a.stateChangedAt).getTime()) / 1000) : 120,
            callsHandledToday: 15
          })));
        }
      })
      .catch(err => console.error('Failed to load agents from SQL Server:', err));
  }

  public loadCampaignsFromDb() {
    fetch('http://localhost:5000/api/campaigns')
      .then(res => res.json())
      .then((data: any[]) => {
        if (Array.isArray(data) && data.length > 0) {
          this.campaigns.set(data.map(c => ({
            name: c.name || c.campaignName || 'Campaign',
            type: c.type === 0 ? 'Inbound' : (c.type === 1 ? 'Outbound' : (c.type || 'Outbound')),
            status: c.status === 0 ? 'Draft' : (c.status === 1 ? 'Active' : (c.status === 2 ? 'Paused' : 'Active')),
            agents: c.allocatedAgents || 10
          })));
        }
      })
      .catch(err => console.error('Failed to load campaigns from SQL Server:', err));
  }

  public loadQueuesFromDb() {
    fetch('http://localhost:5000/api/queues')
      .then(res => res.json())
      .then((data: any[]) => {
        if (Array.isArray(data) && data.length > 0) {
          const queues = data.map(q => ({
            queueName: q.queueName,
            waiting: q.waitingCalls || 0,
            longestWait: '00:01:20'
          }));
          this.supervisorDashboard.update(dash => ({
            ...dash,
            queues: queues
          }));
        }
      })
      .catch(err => console.error('Failed to load queues from SQL Server:', err));
  }

  public loadCallHistory() {
    try {
      const cached = localStorage.getItem('btcl_call_history');
      if (cached) {
        const parsed = JSON.parse(cached);
        if (Array.isArray(parsed) && parsed.length > 0) {
          this.callHistory.set(parsed);
        }
      }
    } catch {}

    fetch('http://localhost:5000/api/calls/history?limit=100')
      .then(res => res.json())
      .then((data: any[]) => {
        if (Array.isArray(data) && data.length > 0) {
          const serverHistory = data.map(item => ({
            id: item.id || item.callUuid,
            callUuid: item.callUuid,
            number: item.number || item.destinationNumber || 'Unknown',
            callerNumber: item.callerNumber,
            destinationNumber: item.destinationNumber,
            customerName: item.customerName || item.customerId || 'Customer',
            direction: item.direction || 'Outbound',
            durationSeconds: item.durationSeconds || 0,
            status: item.status || 'Completed',
            timestamp: item.timestamp || 'Recent',
            disposition: item.disposition || item.status,
            notes: item.notes || ''
          }));
          this.callHistory.set(serverHistory);
          this.saveCallHistoryToLocal();
        }
      })
      .catch(err => console.error('Failed to load call history from SQL Server:', err));
  }

  public saveCallHistoryToLocal() {
    try {
      localStorage.setItem('btcl_call_history', JSON.stringify(this.callHistory().slice(0, 100)));
    } catch {}
  }

  private startCallTimer() {
    this.timerInterval = setInterval(() => {
      if (this.callStatus() === 'Connected' || this.callStatus() === 'OnHold') {
        this.activeCallDuration.update(d => d + 1);
      }
    }, 1000);
  }

  private initializeSignalR() {
    try {
      this.hubConnection = new signalR.HubConnectionBuilder()
        .withUrl('http://localhost:5000/hubs/calls', {
          skipNegotiation: false,
          transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
        })
        .withAutomaticReconnect()
        .build();

      this.hubConnection.on('ReceiveScreenPop', (screenPop: any) => {
        this.activeCallerNumber.set(screenPop.callerNumber);
        this.callStatus.set('Ringing');
        this.audio.startRingback();
        this.activeCallDuration.set(0);
        if (screenPop.customer) {
          this.currentCustomer.set({
            customerId: screenPop.customer.customerId,
            name: screenPop.customer.name,
            phone: screenPop.customer.phoneNumber,
            email: screenPop.customer.email,
            tier: screenPop.customer.tier,
            crmId: screenPop.customer.customerId,
            accountNo: 'ACC-' + (screenPop.callerNumber.slice(-4) || '8042'),
            address: 'Dhaka, Bangladesh',
            notes: screenPop.customer.recentNotes || ['Inbound inquiry via BTCL SIP Gateway']
          });
        }
      });

      this.hubConnection.start()
        .then(() => {
          console.log('SignalR Telephony Hub Connected');
          this.systemHealth.update(h => ({ ...h, signalR: 'Connected (Live)' }));
        })
        .catch(err => {
          console.warn('SignalR local fallback:', err);
          this.systemHealth.update(h => ({ ...h, signalR: 'Interactive Standalone' }));
        });
    } catch (e) {
      console.warn('SignalR initialization fallback:', e);
    }
  }

  public originateCall(destinationNumber: string) {
    const cleanNumber = destinationNumber.trim();
    if (!cleanNumber) return;

    this.activeCallerNumber.set(cleanNumber);
    this.callStatus.set('Ringing');
    this.agentState.set('OnCall');
    this.activeCallDuration.set(0);
    this.isMuted.set(false);
    this.isOnHold.set(false);
    this.isRecording.set(true);

    this.audio.startRingback();

    // Check contact match
    const match = this.quickContacts().find(c =>
      c.phone.replace(/[\s+-]/g, '') === cleanNumber.replace(/[\s+-]/g, '') ||
      c.phone.includes(cleanNumber) ||
      cleanNumber.includes(c.phone)
    );

    const crmSuffix = cleanNumber.replace(/[^0-9]/g, '').slice(-4) || '8042';

    if (match) {
      this.currentCustomer.set({
        customerId: 'CRM-' + crmSuffix,
        name: match.name,
        phone: match.phone,
        email: match.email,
        tier: match.tier,
        crmId: 'CRM-' + crmSuffix,
        accountNo: 'ACC-BD-' + crmSuffix,
        address: 'Dhaka, Bangladesh',
        notes: [
          'VIP Contact Record: ' + match.role,
          'Outbound call initiated via BTCL SIP Trunk.'
        ]
      });
    } else {
      this.currentCustomer.set({
        customerId: 'C-' + Math.floor(1000 + Math.random() * 9000),
        name: 'Subscriber (' + cleanNumber + ')',
        phone: cleanNumber,
        email: 'client.' + crmSuffix + '@telecom.bd',
        tier: 'Standard Client',
        crmId: 'CRM-' + crmSuffix,
        accountNo: 'ACC-BD-' + crmSuffix,
        address: 'Bangladesh',
        notes: [
          'Outbound interaction initiated on ' + new Date().toLocaleDateString('en-GB'),
          'Direct dial via FreeSWITCH WebRTC.'
        ]
      });
    }

    const callUuid = 'call-' + Date.now() + '-' + Math.random().toString(36).substring(2, 8);
    this.currentCallUuid.set(callUuid);

    fetch('http://localhost:5000/api/calls/originate', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        callUuid: callUuid,
        agentExtension: '1001',
        destinationNumber: cleanNumber,
        customerId: this.currentCustomer().customerId
      })
    })
      .then(res => res.json())
      .then(data => {
        if (data?.callUuid) this.currentCallUuid.set(data.callUuid);
        if (data?.customer) {
          this.currentCustomer.update(c => ({
            ...c,
            name: data.customer.name || c.name,
            tier: data.customer.tier || c.tier
          }));
        }
      })
      .catch(err => console.error('Call origination error:', err));

    // Automatically answer after 2.6s
    if (this.ringTimeout) clearTimeout(this.ringTimeout);
    this.ringTimeout = setTimeout(() => {
      if (this.callStatus() === 'Ringing') {
        this.answerCall();
      }
    }, 2600);
  }

  public answerCall() {
    if (this.ringTimeout) {
      clearTimeout(this.ringTimeout);
      this.ringTimeout = undefined;
    }
    this.audio.stopRingback();
    this.audio.playConnectedChime();
    this.callStatus.set('Connected');
    this.agentState.set('OnCall');
    this.activeCallDuration.set(0);

    if (this.isRecording()) {
      this.audioRecorder.startMicrophoneRecording().catch(err => {
        console.warn('Microphone recording error:', err);
      });
    }
  }

  public hangupCall() {
    if (this.ringTimeout) {
      clearTimeout(this.ringTimeout);
      this.ringTimeout = undefined;
    }
    this.audio.stopRingback();
    this.audio.playDisconnectTone();

    const uuid = this.currentCallUuid();
    if (uuid) {
      fetch('http://localhost:5000/api/calls/' + uuid + '/terminate', { method: 'POST' })
        .catch(err => console.error('Terminate call fetch error:', err));
    }

    // Stop and save audio recording
    const duration = this.activeCallDuration();
    const customer = this.currentCustomer();
    const phone = this.activeCallerNumber() || customer.phone || 'Direct Outbound';

    if (this.audioRecorder.isRecording()) {
      this.audioRecorder.stopMicrophoneRecording(
        uuid || 'call-' + Date.now(),
        customer.name,
        phone,
        duration
      );
    }

    // Add to Call History
    const wasCancelled = this.callStatus() === 'Ringing';
    const historyEntry: CallHistoryItem = {
      id: 'h-' + Date.now(),
      number: phone,
      customerName: customer.name,
      direction: 'Outbound',
      durationSeconds: duration,
      timestamp: 'Just now',
      disposition: wasCancelled ? 'Cancelled (Pending Wrap-up)' : 'Pending Wrap-up'
    };
    this.callHistory.update(list => [historyEntry, ...list]);
    this.saveCallHistoryToLocal();

    this.callStatus.set('WrapUp');
    this.agentState.set('WrapUp');
  }

  public toggleHold() {
    const nextHold = !this.isOnHold();
    this.isOnHold.set(nextHold);
    this.callStatus.set(nextHold ? 'OnHold' : 'Connected');

    if (nextHold) {
      this.audio.startHoldMusic();
    } else {
      this.audio.stopHoldMusic();
    }

    const uuid = this.currentCallUuid();
    if (uuid) {
      fetch('http://localhost:5000/api/calls/' + uuid + '/hold?hold=' + nextHold, { method: 'POST' })
        .catch(() => {});
    }
  }

  public toggleMute() {
    this.isMuted.update(m => !m);
  }

  public toggleRecording() {
    const next = !this.isRecording();
    this.isRecording.set(next);
    if (this.callStatus() === 'Connected') {
      if (next) {
        this.audioRecorder.startMicrophoneRecording();
      } else {
        const uuid = this.currentCallUuid();
        const customer = this.currentCustomer();
        this.audioRecorder.stopMicrophoneRecording(
          uuid || 'call-' + Date.now(),
          customer.name,
          this.activeCallerNumber() || customer.phone,
          this.activeCallDuration()
        );
      }
    }
  }

  public transferCall(targetExt: string, mode: 'Blind' | 'Attended') {
    const targetAgent = this.agentsList().find(a => a.extension === targetExt);
    const agentName = targetAgent ? targetAgent.name : `Ext ${targetExt}`;
    const uuid = this.currentCallUuid() || ('call-' + Date.now());
    const customer = this.currentCustomer();
    const phone = this.activeCallerNumber() || customer.phone || 'Direct Outbound';
    const duration = this.activeCallDuration();

    // 1. Audio tone & add customer note
    this.audio.playConnectedChime();
    const transferNote = `Call transferred to ${agentName} (${targetExt}) via ${mode} transfer.`;
    this.addCustomerNote(transferNote);

    // 2. Persist transfer to SQL Server backend
    fetch(`http://localhost:5000/api/calls/${uuid}/transfer`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        targetExtension: targetExt,
        transferMode: mode,
        reason: `Transferred to ${agentName} (${targetExt})`,
        customerName: customer.name,
        destinationNumber: phone
      })
    })
      .then(res => res.json())
      .then(data => {
        console.log('✓ Call transfer permanently saved to SQL Server:', data);
        this.saveNotification.set(`✓ Call transferred to ${agentName} (${targetExt}) and saved permanently to SQL Server!`);
        setTimeout(() => this.saveNotification.set(''), 4500);
        this.loadCallHistory();
      })
      .catch(err => {
        console.warn('Call transfer server fallback:', err);
        this.saveNotification.set(`✓ Call transferred to ${agentName} (${targetExt}) locally.`);
        setTimeout(() => this.saveNotification.set(''), 4000);
      });

    // 3. Add to Call History permanently
    const historyEntry: CallHistoryItem = {
      id: 'h-' + Date.now(),
      callUuid: uuid,
      number: phone,
      customerName: customer.name,
      direction: 'Transferred',
      durationSeconds: duration,
      status: 'Transferred',
      timestamp: 'Just now',
      disposition: `TRANSFERRED (${targetExt})`,
      notes: transferNote
    };
    this.callHistory.update(list => [historyEntry, ...list]);
    this.saveCallHistoryToLocal();

    // 4. Save call recording permanently if recording is active
    if (this.audioRecorder.isRecording()) {
      this.audioRecorder.stopMicrophoneRecording(
        uuid,
        customer.name,
        phone,
        duration
      );
    }

    // 5. Reset softphone state
    if (this.ringTimeout) {
      clearTimeout(this.ringTimeout);
      this.ringTimeout = undefined;
    }
    this.audio.stopRingback();
    this.audio.stopHoldMusic();
    this.callStatus.set('Idle');
    this.agentState.set('Available');
    this.activeCallerNumber.set('');
    this.activeCallDuration.set(0);
  }

  public playDtmf(digit: string) {
    this.audio.playDtmf(digit);
  }

  public setAgentStatus(status: 'Available' | 'OnCall' | 'Break' | 'WrapUp' | 'Offline') {
    this.agentState.set(status);
    if (status === 'Available' && this.callStatus() === 'WrapUp') {
      this.callStatus.set('Idle');
    }
  }

  public addCustomerNote(noteText: string) {
    if (!noteText.trim()) return;
    this.currentCustomer.update(c => ({
      ...c,
      notes: [noteText.trim(), ...c.notes]
    }));
  }

  public saveDisposition(disposition: string, notes: string) {
    const uuid = this.currentCallUuid() || ('call-' + Date.now());
    const phone = this.activeCallerNumber() || this.currentCustomer().phone || 'Direct Outbound';
    const customer = this.currentCustomer();

    fetch('http://localhost:5000/api/dispositions/submit', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        callUuid: uuid,
        dispositionName: disposition,
        notes: notes,
        customerId: customer.customerId,
        destinationNumber: phone,
        agentExtension: '1001'
      })
    })
      .then(async res => {
        const data = await res.json();
        console.log('✓ Successfully saved to MS SQL Server (CallCenterDb):', data);
        this.saveNotification.set('✓ Call & disposition saved to MS SQL Server (CallCenterDb)!');
        setTimeout(() => this.saveNotification.set(''), 4500);
        this.loadCallHistory();
      })
      .catch(err => {
        console.error('Failed to submit disposition to backend:', err);
        this.saveNotification.set('⚠ Error saving to SQL Server. Please verify backend is running.');
        setTimeout(() => this.saveNotification.set(''), 5000);
      });

    // Update history record
    this.callHistory.update(list => {
      if (list.length > 0) {
        list[0].disposition = disposition;
      }
      return [...list];
    });

    if (notes.trim()) {
      this.addCustomerNote(`Disposition: ${disposition} — ${notes}`);
    }

    this.callStatus.set('Idle');
    this.agentState.set('Available');
    this.activeCallerNumber.set('');
    this.activeCallDuration.set(0);
  }

  public simulateInboundCall() {
    this.activeCallerNumber.set('+880 1712 345678');
    this.callStatus.set('Ringing');
    this.audio.startRingback();
    this.agentState.set('OnCall');
    this.activeCallDuration.set(0);
    this.currentCustomer.set({
      customerId: 'C12345',
      name: 'Rahim Ahmed',
      phone: '+880 1712 345678',
      email: 'rahim@example.com',
      tier: 'Platinum VIP',
      crmId: 'C12345',
      accountNo: 'ACC-BTCL-9042',
      address: 'Gulshan-2, Dhaka',
      notes: [
        'Billing inquiry regarding previous transaction.',
        'High value customer with 100Mbps dedicated fiber line.'
      ]
    });

    fetch('http://localhost:5000/api/calls/simulate-inbound?callerNumber=+8801712345678&did=09612345678', {
      method: 'POST'
    })
      .then(res => res.json())
      .then(data => {
        if (data?.callUuid) this.currentCallUuid.set(data.callUuid);
      })
      .catch(err => console.error('Simulate inbound error:', err));
  }

  // Supervisor Coaching Methods
  public startCoaching(mode: 'SilentSpy' | 'Whisper' | 'BargeIn', agentName: string, extension: string) {
    this.coachingSession.set({
      active: true,
      mode,
      agentName,
      extension
    });
    this.audio.playConnectedChime();
  }

  public stopCoaching() {
    this.coachingSession.set(null);
    this.audio.playDisconnectTone();
  }

  // Admin Campaign Controls
  public toggleCampaign(campaignName: string) {
    this.campaigns.update(list =>
      list.map(c => {
        if (c.name === campaignName) {
          return { ...c, status: c.status === 'Active' ? 'Paused' : 'Active' };
        }
        return c;
      })
    );
  }

  public deleteCampaign(campaignName: string) {
    this.campaigns.update(list => list.filter(c => c.name !== campaignName));
  }

  public addCampaign(campaign: CampaignItem) {
    this.campaigns.update(list => [campaign, ...list]);
  }
}
