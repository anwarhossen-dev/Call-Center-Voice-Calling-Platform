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
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: LoginPageComponent },
  { path: 'register', component: RegisterPageComponent },
  
  // Agent & Common Workspaces
  { 
    path: 'agent', 
    component: AgentPortalComponent, 
    canActivate: [roleGuard], 
    data: { roles: ['Admin', 'Supervisor', 'Agent'] } 
  },
  { 
    path: 'history', 
    component: HistoryPageComponent, 
    canActivate: [roleGuard], 
    data: { roles: ['Admin', 'Supervisor', 'Agent'] } 
  },

  // Supervisor & QA Management
  { 
    path: 'supervisor', 
    component: SupervisorPortalComponent, 
    canActivate: [roleGuard], 
    data: { roles: ['Admin', 'Supervisor'] } 
  },
  { 
    path: 'recordings', 
    component: RecordingsPageComponent, 
    canActivate: [roleGuard], 
    data: { roles: ['Admin', 'Supervisor'] } 
  },

  // System Administration & Database Management (Strictly Admin)
  { 
    path: 'admin', 
    component: AdminPortalComponent, 
    canActivate: [roleGuard], 
    data: { roles: ['Admin'] } 
  },
  { 
    path: 'tables', 
    component: TablesPortalComponent, 
    canActivate: [roleGuard], 
    data: { roles: ['Admin'] } 
  },

  { path: '**', redirectTo: 'login' }
];
