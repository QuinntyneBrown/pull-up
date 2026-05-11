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
    path: 'home',
    canActivate: [authGuard],
    loadComponent: () => import('domain').then(m => m.HomePageComponent),
  },
  { path: '**', redirectTo: 'sign-up' },
];
