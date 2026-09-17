import { Routes } from '@angular/router';
import { AgentPortalComponent } from './features/agent-portal/agent-portal.component';
import { SupervisorPortalComponent } from './features/supervisor-portal/supervisor-portal.component';
import { RecordingsPageComponent } from './features/recordings-page/recordings-page.component';
import { HistoryPageComponent } from './features/history-page/history-page.component';
import { AdminPortalComponent } from './features/admin-portal/admin-portal.component';
import { TablesPortalComponent } from './features/tables-portal/tables-portal.component';
import { LoginPageComponent } from './features/login-page/login-page.component';
import { RegisterPageComponent } from './features/register-page/register-page.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: LoginPageComponent },
  { path: 'register', component: RegisterPageComponent },
  { path: 'agent', component: AgentPortalComponent, canActivate: [authGuard] },
  { path: 'history', component: HistoryPageComponent, canActivate: [authGuard] },
  { path: 'supervisor', component: SupervisorPortalComponent, canActivate: [authGuard] },
  { path: 'recordings', component: RecordingsPageComponent, canActivate: [authGuard] },
  { path: 'admin', component: AdminPortalComponent, canActivate: [authGuard] },
  { path: 'tables', component: TablesPortalComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: 'login' }
];
