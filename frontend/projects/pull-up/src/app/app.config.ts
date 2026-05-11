import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { API_BASE_URL, AUTH_SERVICE, AuthService } from 'api';
import { authJwtInterceptor } from './auth-jwt.interceptor';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authJwtInterceptor])),
    provideAnimations(),
    { provide: API_BASE_URL, useValue: 'http://localhost:5080' },
    { provide: AUTH_SERVICE, useExisting: AuthService },
  ],
};
