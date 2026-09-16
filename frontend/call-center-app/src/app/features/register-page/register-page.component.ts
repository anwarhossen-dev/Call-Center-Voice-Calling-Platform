import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-register-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './register-page.component.html',
  styleUrls: ['./register-page.component.css']
})
export class RegisterPageComponent {
  private auth = inject(AuthService);
  private router = inject(Router);

  public displayName = signal<string>('');
  public username = signal<string>('');
  public email = signal<string>('');
  public password = signal<string>('');
  public confirmPassword = signal<string>('');
  public role = signal<'Agent' | 'Supervisor' | 'Admin'>('Agent');
  public extension = signal<string>('');
  public showPassword = signal<boolean>(false);
  public isLoading = signal<boolean>(false);
  public errorMessage = signal<string>('');
  public successMessage = signal<string>('');

  public toggleShowPassword() {
    this.showPassword.update(v => !v);
  }

  public async onSubmit() {
    this.errorMessage.set('');
    this.successMessage.set('');

    const name = this.displayName().trim();
    const u = this.username().trim();
    const em = this.email().trim();
    const p = this.password();
    const cp = this.confirmPassword();
    const r = this.role();
    const ext = this.extension().trim();

    if (!u || !p) {
      this.errorMessage.set('Username and Password are required.');
      return;
    }

    if (p.length < 6) {
      this.errorMessage.set('Password must be at least 6 characters long.');
      return;
    }

    if (p !== cp) {
      this.errorMessage.set('Passwords do not match. Please re-enter.');
      return;
    }

    this.isLoading.set(true);
    const result = await this.auth.register({
      displayName: name || u,
      username: u,
      email: em,
      password: p,
      role: r,
      extension: ext || undefined
    });
    this.isLoading.set(false);

    if (result.success && result.user) {
      this.successMessage.set(`Registration successful! Welcome, ${result.user.displayName}. Redirecting...`);
      setTimeout(() => {
        if (result.user?.role === 'Admin') {
          this.router.navigate(['/admin']);
        } else if (result.user?.role === 'Supervisor') {
          this.router.navigate(['/supervisor']);
        } else {
          this.router.navigate(['/agent']);
        }
      }, 900);
    } else {
      this.errorMessage.set(result.message || 'Registration failed.');
    }
  }
}
