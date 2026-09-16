import { Injectable, signal, computed } from '@angular/core';
import { Router } from '@angular/router';

export interface AuthUser {
  userId: string;
  username: string;
  email: string;
  displayName: string;
  role: 'Admin' | 'Supervisor' | 'Agent' | string;
  extension?: string;
  agentId?: string;
  token: string;
  loggedInAt?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly STORAGE_KEY = 'btcl_telephony_auth';

  public currentUser = signal<AuthUser | null>(null);
  public isLoggedIn = computed(() => !!this.currentUser());
  public userRole = computed(() => this.currentUser()?.role || 'Guest');
  public userDisplayName = computed(() => this.currentUser()?.displayName || this.currentUser()?.username || 'Guest');
  public userExtension = computed(() => this.currentUser()?.extension || '');

  constructor(private router: Router) {
    this.restoreSession();
  }

  private restoreSession() {
    try {
      const stored = localStorage.getItem(this.STORAGE_KEY);
      if (stored) {
        const user: AuthUser = JSON.parse(stored);
        this.currentUser.set(user);
      } else {
        this.currentUser.set(null);
      }
    } catch (e) {
      console.error('Failed to parse auth from storage', e);
      this.currentUser.set(null);
    }
  }

  public async login(usernameOrEmail: string, password: string):Promise<{ success: boolean; message?: string; user?: AuthUser }> {
    try {
      const res = await fetch('http://localhost:5000/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ usernameOrEmail, password })
      });

      const data = await res.json();
      if (!res.ok) {
        return { success: false, message: data.message || 'Login failed. Check your credentials.' };
      }

      this.currentUser.set(data);
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(data));
      return { success: true, user: data };
    } catch (err: any) {
      return { success: false, message: 'Could not connect to backend server: ' + (err?.message || err) };
    }
  }

  public async register(payload: {
    username: string;
    email: string;
    password: string;
    displayName: string;
    role: string;
    extension?: string;
  }): Promise<{ success: boolean; message?: string; user?: AuthUser }> {
    try {
      const res = await fetch('http://localhost:5000/api/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });

      const data = await res.json();
      if (!res.ok) {
        return { success: false, message: data.message || 'Registration failed.' };
      }

      this.currentUser.set(data);
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(data));
      return { success: true, user: data };
    } catch (err: any) {
      return { success: false, message: 'Could not connect to backend server: ' + (err?.message || err) };
    }
  }

  public setDemoUser(user: AuthUser) {
    this.currentUser.set(user);
    localStorage.setItem(this.STORAGE_KEY, JSON.stringify(user));
  }

  public logout() {
    this.currentUser.set(null);
    localStorage.removeItem(this.STORAGE_KEY);
    this.router.navigate(['/login']);
  }
}
