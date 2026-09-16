import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class AudioToneService {
  private audioCtx?: AudioContext;
  private ringInterval?: any;
  private isRingingActive = false;
  private holdInterval?: any;
  private isHoldActive = false;

  private dtmfFreqs: Record<string, [number, number]> = {
    '1': [697, 1209],
    '2': [697, 1336],
    '3': [697, 1477],
    '4': [770, 1209],
    '5': [770, 1336],
    '6': [770, 1477],
    '7': [852, 1209],
    '8': [852, 1336],
    '9': [852, 1477],
    '*': [941, 1209],
    '0': [941, 1336],
    '#': [941, 1477],
    '+': [941, 1336]
  };

  private getAudioContext(): AudioContext | null {
    try {
      if (!this.audioCtx) {
        const AudioContextClass = window.AudioContext || (window as any).webkitAudioContext;
        if (AudioContextClass) {
          this.audioCtx = new AudioContextClass();
        }
      }
      if (this.audioCtx && this.audioCtx.state === 'suspended') {
        this.audioCtx.resume();
      }
      return this.audioCtx || null;
    } catch {
      return null;
    }
  }

  public playDtmf(digit: string, durationMs = 120): void {
    const ctx = this.getAudioContext();
    if (!ctx) return;

    const freqs = this.dtmfFreqs[digit];
    if (!freqs) return;

    try {
      const [f1, f2] = freqs;
      const osc1 = ctx.createOscillator();
      const osc2 = ctx.createOscillator();
      const gainNode = ctx.createGain();

      osc1.type = 'sine';
      osc2.type = 'sine';
      osc1.frequency.setValueAtTime(f1, ctx.currentTime);
      osc2.frequency.setValueAtTime(f2, ctx.currentTime);

      gainNode.gain.setValueAtTime(0.08, ctx.currentTime);
      gainNode.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + durationMs / 1000);

      osc1.connect(gainNode);
      osc2.connect(gainNode);
      gainNode.connect(ctx.destination);

      osc1.start();
      osc2.start();

      osc1.stop(ctx.currentTime + durationMs / 1000);
      osc2.stop(ctx.currentTime + durationMs / 1000);
    } catch (e) {
      console.warn('DTMF audio error:', e);
    }
  }

  public startRingback(): void {
    if (this.isRingingActive) return;
    this.isRingingActive = true;

    const ctx = this.getAudioContext();
    if (!ctx) return;

    const playRingCycle = () => {
      if (!this.isRingingActive || !this.audioCtx) return;
      try {
        const c = this.audioCtx;
        const o1 = c.createOscillator();
        const o2 = c.createOscillator();
        const g = c.createGain();

        o1.type = 'sine';
        o2.type = 'sine';
        o1.frequency.setValueAtTime(440, c.currentTime);
        o2.frequency.setValueAtTime(480, c.currentTime);

        g.gain.setValueAtTime(0.06, c.currentTime);
        g.gain.setValueAtTime(0.06, c.currentTime + 1.6);
        g.gain.exponentialRampToValueAtTime(0.0001, c.currentTime + 1.8);

        o1.connect(g);
        o2.connect(g);
        g.connect(c.destination);

        o1.start(c.currentTime);
        o2.start(c.currentTime);
        o1.stop(c.currentTime + 1.8);
        o2.stop(c.currentTime + 1.8);
      } catch (e) {
        console.warn('Ringback cycle error:', e);
      }
    };

    playRingCycle();
    this.ringInterval = setInterval(() => {
      if (this.isRingingActive) {
        playRingCycle();
      }
    }, 4000);
  }

  public stopRingback(): void {
    this.isRingingActive = false;
    if (this.ringInterval) {
      clearInterval(this.ringInterval);
      this.ringInterval = undefined;
    }
  }

  public startHoldMusic(): void {
    if (this.isHoldActive) return;
    this.isHoldActive = true;
    const ctx = this.getAudioContext();
    if (!ctx) return;

    const notes = [329.63, 392.00, 440.00, 523.25, 440.00, 392.00];
    let noteIdx = 0;

    const playNote = () => {
      if (!this.isHoldActive || !this.audioCtx) return;
      try {
        const c = this.audioCtx;
        const osc = c.createOscillator();
        const gain = c.createGain();
        osc.type = 'triangle';
        osc.frequency.setValueAtTime(notes[noteIdx % notes.length], c.currentTime);
        noteIdx++;
        gain.gain.setValueAtTime(0.03, c.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.0001, c.currentTime + 0.5);
        osc.connect(gain);
        gain.connect(c.destination);
        osc.start();
        osc.stop(c.currentTime + 0.5);
      } catch {}
    };

    playNote();
    this.holdInterval = setInterval(playNote, 600);
  }

  public stopHoldMusic(): void {
    this.isHoldActive = false;
    if (this.holdInterval) {
      clearInterval(this.holdInterval);
      this.holdInterval = undefined;
    }
  }

  public playConnectedChime(): void {
    this.stopRingback();
    this.stopHoldMusic();
    const ctx = this.getAudioContext();
    if (!ctx) return;

    try {
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();

      osc.type = 'sine';
      osc.frequency.setValueAtTime(587.33, ctx.currentTime);
      osc.frequency.setValueAtTime(880, ctx.currentTime + 0.12);

      gain.gain.setValueAtTime(0.08, ctx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + 0.4);

      osc.connect(gain);
      gain.connect(ctx.destination);

      osc.start();
      osc.stop(ctx.currentTime + 0.4);
    } catch (e) {
      console.warn('Connected chime error:', e);
    }
  }

  public playDisconnectTone(): void {
    this.stopRingback();
    this.stopHoldMusic();
    const ctx = this.getAudioContext();
    if (!ctx) return;

    try {
      const o1 = ctx.createOscillator();
      const o2 = ctx.createOscillator();
      const gain = ctx.createGain();

      o1.type = 'sine';
      o2.type = 'sine';
      o1.frequency.setValueAtTime(480, ctx.currentTime);
      o2.frequency.setValueAtTime(620, ctx.currentTime);

      gain.gain.setValueAtTime(0.07, ctx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + 0.35);

      o1.connect(gain);
      o2.connect(gain);
      gain.connect(ctx.destination);

      o1.start();
      o2.start();
      o1.stop(ctx.currentTime + 0.35);
      o2.stop(ctx.currentTime + 0.35);
    } catch (e) {
      console.warn('Disconnect tone error:', e);
    }
  }
}
