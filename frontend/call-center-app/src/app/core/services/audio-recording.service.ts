import { Injectable, signal } from '@angular/core';

export interface RecordedCallItem {
  id: string;
  callUuid: string;
  customerName: string;
  phoneNumber: string;
  durationSeconds: number;
  audioUrl: string;
  blob?: Blob;
  timestamp: string;
  isPlaying?: boolean;
}

const DB_NAME = 'BTCL_VoiceRecordings_DB';
const DB_VERSION = 1;
const STORE_NAME = 'recordings';

@Injectable({
  providedIn: 'root'
})
export class AudioRecordingService {
  private mediaRecorder?: MediaRecorder;
  private audioChunks: Blob[] = [];
  private mediaStream?: MediaStream;

  public isRecording = signal<boolean>(false);
  public recordedCalls = signal<RecordedCallItem[]>([]);

  public currentPlaybackAudio?: HTMLAudioElement;
  public activePlayingId = signal<string | null>(null);
  public latestRecording = signal<RecordedCallItem | null>(null);

  constructor() {
    this.loadAllPersistedRecordings();
  }

  /**
   * Opens or initializes the HTML5 IndexedDB for permanent voice storage
   */
  private openDb(): Promise<IDBDatabase> {
    return new Promise((resolve, reject) => {
      if (typeof indexedDB === 'undefined') {
        return reject('IndexedDB not supported in this environment.');
      }
      const request = indexedDB.open(DB_NAME, DB_VERSION);
      request.onupgradeneeded = () => {
        const db = request.result;
        if (!db.objectStoreNames.contains(STORE_NAME)) {
          db.createObjectStore(STORE_NAME, { keyPath: 'id' });
        }
      };
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(request.error);
    });
  }

  /**
   * Saves a recording item and audio Blob permanently into browser IndexedDB
   */
  private async saveToIndexedDb(item: RecordedCallItem, blob?: Blob): Promise<void> {
    try {
      const db = await this.openDb();
      const tx = db.transaction(STORE_NAME, 'readwrite');
      const store = tx.objectStore(STORE_NAME);
      const dataToSave = {
        id: item.id,
        callUuid: item.callUuid,
        customerName: item.customerName,
        phoneNumber: item.phoneNumber,
        durationSeconds: item.durationSeconds,
        timestamp: item.timestamp,
        audioUrl: item.audioUrl && !item.audioUrl.startsWith('blob:') ? item.audioUrl : '',
        blobData: blob || item.blob
      };
      store.put(dataToSave);
      console.log('✓ Recording permanently saved to IndexedDB:', item.id);
    } catch (err) {
      console.warn('Could not save recording to IndexedDB:', err);
    }
  }

  /**
   * Loads all permanently persisted recordings from IndexedDB and backend API on startup
   */
  public async loadAllPersistedRecordings(): Promise<void> {
    const loadedItems: RecordedCallItem[] = [];

    // 1. Load from IndexedDB
    try {
      const db = await this.openDb();
      const tx = db.transaction(STORE_NAME, 'readonly');
      const store = tx.objectStore(STORE_NAME);
      const request = store.getAll();

      request.onsuccess = () => {
        const records = request.result as any[];
        if (Array.isArray(records) && records.length > 0) {
          for (const r of records) {
            let url = r.audioUrl || '';
            if (r.blobData) {
              try {
                url = URL.createObjectURL(r.blobData);
              } catch {}
            }
            loadedItems.push({
              id: r.id,
              callUuid: r.callUuid,
              customerName: r.customerName,
              phoneNumber: r.phoneNumber,
              durationSeconds: r.durationSeconds,
              audioUrl: url,
              blob: r.blobData,
              timestamp: r.timestamp
            });
          }
        }

        // 2. Fetch server-stored recordings from backend API (Microsoft SQL Server CallRecordings table)
        fetch('http://localhost:5000/api/recordings?limit=100')
          .then((res) => res.json())
          .then((raw: any) => {
            const serverItems: any[] = Array.isArray(raw) ? raw : (raw?.data && Array.isArray(raw.data) ? raw.data : []);
            if (serverItems.length > 0) {
              const existingUuids = new Set(loadedItems.map((i) => i.callUuid));
              for (const s of serverItems) {
                const callUuid = s.callUuid || s.call?.callUuid || (s.storagePath ? s.storagePath.split('/').pop()?.replace('.webm', '') : '') || s.recordingId;
                if (!existingUuids.has(callUuid)) {
                  existingUuids.add(callUuid);
                  const custName = s.customerName || (s.call && s.call.crmContactId ? s.call.crmContactId : 'Direct Caller');
                  const phone = s.phoneNumber || (s.call ? (s.call.destinationNumber || s.call.callerNumber) : 'N/A');
                  const duration = s.durationSeconds || (s.call ? s.call.totalDurationSeconds : 0) || 15;
                  const audioUrl = s.audioUrl || (s.storagePath ? (s.storagePath.startsWith('http') ? s.storagePath : `http://localhost:5000${s.storagePath}`) : '');
                  let time = s.timestamp;
                  if (!time && s.offloadedAt) {
                    try {
                      time = new Date(s.offloadedAt).toLocaleDateString('en-US', { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
                    } catch {
                      time = s.offloadedAt;
                    }
                  }
                  loadedItems.push({
                    id: s.id || s.recordingId || 'srv-' + callUuid,
                    callUuid: callUuid,
                    customerName: custName,
                    phoneNumber: phone,
                    durationSeconds: duration,
                    audioUrl: audioUrl,
                    timestamp: time || 'Archived'
                  });
                }
              }
            }

            this.recordedCalls.set([...loadedItems]);
            console.log(`✓ Synchronized ${loadedItems.length} recordings from SQL Server & Local Storage.`);
          })
          .catch((err) => {
            console.warn('Backend recordings fetch error:', err);
            this.recordedCalls.set([...loadedItems]);
          });
      };
    } catch (e) {
      console.warn('Failed to load recordings from IndexedDB:', e);
    }
  }

  /**
   * Uploads recording file to backend server for permanent disk storage
   */
  public async uploadRecordingToBackend(item: RecordedCallItem, blob: Blob): Promise<void> {
    try {
      const formData = new FormData();
      formData.append('CallUuid', item.callUuid);
      formData.append('CustomerName', item.customerName);
      formData.append('PhoneNumber', item.phoneNumber);
      formData.append('DurationSeconds', item.durationSeconds.toString());
      formData.append('AudioFile', blob, `${item.callUuid}.webm`);

      const res = await fetch('http://localhost:5000/api/recordings/upload', {
        method: 'POST',
        body: formData
      });

      if (res.ok) {
        const data = await res.json();
        if (data?.audioUrl) {
          item.audioUrl = data.audioUrl;
          this.saveToIndexedDb(item, blob);
          this.recordedCalls.update((list) =>
            list.map((r) => (r.id === item.id ? { ...r, audioUrl: data.audioUrl } : r))
          );
          console.log('✓ Recording successfully synced to backend storage:', data.audioUrl);
        }
      }
    } catch (err) {
      console.warn('Backend upload skipped, recording remains permanent in browser IndexedDB:', err);
    }
  }

  /**
   * Starts real microphone voice recording via browser MediaRecorder API
   */
  public async startMicrophoneRecording(): Promise<boolean> {
    try {
      if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        console.warn('Microphone recording not supported on this browser.');
        return false;
      }

      this.mediaStream = await navigator.mediaDevices.getUserMedia({
        audio: {
          echoCancellation: true,
          noiseSuppression: true,
          autoGainControl: true
        }
      });

      this.audioChunks = [];
      const options = { mimeType: 'audio/webm' };

      try {
        this.mediaRecorder = new MediaRecorder(this.mediaStream, options);
      } catch {
        this.mediaRecorder = new MediaRecorder(this.mediaStream);
      }

      this.mediaRecorder.ondataavailable = (event) => {
        if (event.data && event.data.size > 0) {
          this.audioChunks.push(event.data);
        }
      };

      this.mediaRecorder.start(250);
      this.isRecording.set(true);
      console.log('Real voice microphone recording started.');
      return true;
    } catch (err) {
      console.warn('Microphone permission denied or not available:', err);
      return false;
    }
  }

  /**
   * Stops microphone recording, produces audio Blob & URL, and permanently saves
   */
  public stopMicrophoneRecording(
    callUuid: string,
    customerName: string,
    phoneNumber: string,
    durationSeconds: number
  ): Promise<RecordedCallItem | null> {
    return new Promise((resolve) => {
      if (!this.mediaRecorder || this.mediaRecorder.state === 'inactive') {
        this.isRecording.set(false);
        this.cleanupStream();
        resolve(null);
        return;
      }

      this.mediaRecorder.onstop = () => {
        const audioBlob = new Blob(this.audioChunks, { type: 'audio/webm' });
        const localAudioUrl = URL.createObjectURL(audioBlob);
        const nowTime = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) + ', Today';

        const newRec: RecordedCallItem = {
          id: 'rec-' + Date.now(),
          callUuid: callUuid || 'call-' + Date.now(),
          customerName: customerName || 'Direct Outbound',
          phoneNumber: phoneNumber || 'N/A',
          durationSeconds: Math.max(durationSeconds, 1),
          audioUrl: localAudioUrl,
          blob: audioBlob,
          timestamp: nowTime
        };

        this.latestRecording.set(newRec);
        this.recordedCalls.update((list) => [newRec, ...list]);
        this.isRecording.set(false);
        this.cleanupStream();

        // 1. Permanent Storage in IndexedDB
        this.saveToIndexedDb(newRec, audioBlob);

        // 2. Permanent Storage in Backend WebAPI
        this.uploadRecordingToBackend(newRec, audioBlob);

        console.log('✓ Call recording permanently stored:', newRec);
        resolve(newRec);
      };

      try {
        this.mediaRecorder.stop();
      } catch {
        this.isRecording.set(false);
        this.cleanupStream();
        resolve(null);
      }
    });
  }

  private cleanupStream() {
    if (this.mediaStream) {
      this.mediaStream.getTracks().forEach((track) => track.stop());
      this.mediaStream = undefined;
    }
  }

  /**
   * Play or Pause a recorded call
   */
  public playRecording(item: RecordedCallItem) {
    if (this.currentPlaybackAudio) {
      this.currentPlaybackAudio.pause();
      this.currentPlaybackAudio = undefined;
    }

    if (this.activePlayingId() === item.id) {
      this.activePlayingId.set(null);
      return;
    }

    if (!item.audioUrl) {
      this.playDemoTone();
      return;
    }

    const audio = new Audio(item.audioUrl);
    this.currentPlaybackAudio = audio;
    this.activePlayingId.set(item.id);

    audio.onended = () => {
      this.activePlayingId.set(null);
      this.currentPlaybackAudio = undefined;
    };

    audio.onerror = () => {
      this.activePlayingId.set(null);
      this.currentPlaybackAudio = undefined;
    };

    audio.play().catch(() => {
      this.activePlayingId.set(null);
    });
  }

  private playDemoTone() {
    try {
      const ctx = new (window.AudioContext || (window as any).webkitAudioContext)();
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.frequency.setValueAtTime(440, ctx.currentTime);
      gain.gain.setValueAtTime(0.1, ctx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + 1);
      osc.connect(gain);
      gain.connect(ctx.destination);
      osc.start();
      osc.stop(ctx.currentTime + 1);
    } catch {}
  }
}
