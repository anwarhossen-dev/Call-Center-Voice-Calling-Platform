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

@Injectable({
  providedIn: 'root'
})
export class AudioRecordingService {
  private mediaRecorder?: MediaRecorder;
  private audioChunks: Blob[] = [];
  private mediaStream?: MediaStream;

  public isRecording = signal<boolean>(false);
  public recordedCalls = signal<RecordedCallItem[]>([
    {
      id: 'rec-01',
      callUuid: 'call-demo-01',
      customerName: 'Rahim Ahmed',
      phoneNumber: '+880 1712 345678',
      durationSeconds: 154,
      audioUrl: '',
      timestamp: 'Today 10:14 AM'
    },
    {
      id: 'rec-02',
      callUuid: 'call-demo-02',
      customerName: 'Karim Ullah',
      phoneNumber: '+880 1819 876543',
      durationSeconds: 165,
      audioUrl: '',
      timestamp: 'Today 09:30 AM'
    }
  ]);

  public currentPlaybackAudio?: HTMLAudioElement;
  public activePlayingId = signal<string | null>(null);
  public latestRecording = signal<RecordedCallItem | null>(null);

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

      this.mediaRecorder.start(250); // Collect every 250ms
      this.isRecording.set(true);
      console.log('Real voice microphone recording started.');
      return true;
    } catch (err) {
      console.warn('Microphone permission denied or not available:', err);
      return false;
    }
  }

  /**
   * Stops microphone recording, produces audio Blob & URL, and adds to recordings list
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
        const audioUrl = URL.createObjectURL(audioBlob);

        const newRec: RecordedCallItem = {
          id: 'rec-' + Date.now(),
          callUuid,
          customerName: customerName || 'Direct Outbound',
          phoneNumber: phoneNumber || 'N/A',
          durationSeconds: Math.max(durationSeconds, 1),
          audioUrl,
          blob: audioBlob,
          timestamp: 'Just now'
        };

        this.latestRecording.set(newRec);
        this.recordedCalls.update((list) => [newRec, ...list]);
        this.isRecording.set(false);
        this.cleanupStream();
        console.log('Voice recording saved successfully:', newRec);
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
      // Fallback synthetic tone for demo items
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
