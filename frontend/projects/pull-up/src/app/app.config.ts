import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import {
  API_BASE_URL,
  AUTH_SERVICE,
  AuthService,
  EVENTS_SERVICE,
  EventsService,
  PROFILE_SERVICE,
  ProfileService,
} from 'api';
import { authJwtInterceptor } from './auth-jwt.interceptor';
import { errorInterceptor } from './error.interceptor';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([errorInterceptor, authJwtInterceptor])),
    provideAnimations(),
    { provide: API_BASE_URL, useValue: 'http://localhost:5080' },
    { provide: AUTH_SERVICE, useExisting: AuthService },
    { provide: PROFILE_SERVICE, useExisting: ProfileService },
    { provide: EVENTS_SERVICE, useExisting: EventsService },
  ],
};
