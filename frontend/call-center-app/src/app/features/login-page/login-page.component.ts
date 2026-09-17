import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './login-page.component.html',
  styleUrls: ['./login-page.component.css']
})
export class LoginPageComponent {
  private auth = inject(AuthService);
  private router = inject(Router);

  public usernameOrEmail = signal<string>('');
  public password = signal<string>('');
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

    const u = this.usernameOrEmail().trim();
    const p = this.password();

    if (!u || !p) {
      this.errorMessage.set('Please enter both your username/email and password.');
      return;
    }

    this.isLoading.set(true);
    const result = await this.auth.login(u, p);
    this.isLoading.set(false);

    if (result.success && result.user) {
      this.successMessage.set(`Welcome back, ${result.user.displayName}! Redirecting...`);
      setTimeout(() => {
        const targetRoute = this.auth.getDefaultRouteForRole(result.user?.role);
        this.router.navigate([targetRoute]);
      }, 700);
    } else {
      this.errorMessage.set(result.message || 'Login failed.');
    }
  }

  public fillDemo(username: string, pass: string) {
    this.usernameOrEmail.set(username);
    this.password.set(pass);
    this.onSubmit();
  }
}
