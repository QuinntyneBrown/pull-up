import { Routes } from '@angular/router';
import { authGuard } from './auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'sign-up' },
  {
    path: 'sign-up',
    loadComponent: () => import('domain').then(m => m.SignUpPageComponent),
  },
  {
    path: 'sign-in',
    loadComponent: () => import('domain').then(m => m.SignInPageComponent),
  },
  {
    path: 'password-reset',
    loadComponent: () => import('domain').then(m => m.RequestPasswordResetPageComponent),
  },
  {
    path: 'password-reset/confirm',
    loadComponent: () => import('domain').then(m => m.CompletePasswordResetPageComponent),
  },
  {
    path: 'home',
    canActivate: [authGuard],
    loadComponent: () => import('domain').then(m => m.HomePageComponent),
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    loadComponent: () => import('domain').then(m => m.ProfilePageComponent),
  },
  { path: '**', redirectTo: 'sign-up' },
];
