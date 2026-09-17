import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Role-Based Access Control Guard (RBAC)
 * Verifies if the authenticated user has permission to access the specified route.
 */
export const roleGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn()) {
    return router.createUrlTree(['/login']);
  }

  const allowedRoles = (route.data?.['roles'] as string[]) || [];

  // If no specific roles required, grant access to authenticated user
  if (allowedRoles.length === 0 || auth.canAccess(allowedRoles)) {
    return true;
  }

  // User does not have the required role -> Redirect to their role default dashboard
  const fallbackRoute = auth.getDefaultRouteForRole();
  console.warn(
    `[RBAC Security] Access Denied to route '${state.url}'. Role '${auth.userRole()}' is not authorized. Allowed roles: [${allowedRoles.join(', ')}]. Redirecting to ${fallbackRoute}`
  );
  return router.createUrlTree([fallbackRoute]);
};
