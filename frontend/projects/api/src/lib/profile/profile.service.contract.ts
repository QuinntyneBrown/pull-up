import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { ConfirmEmailChangeRequest } from './confirm-email-change-request';
import { DeleteAccountRequest } from './delete-account-request';
import { NotificationPreferences } from './notification-preferences';
import { RequestEmailChangeRequest } from './request-email-change-request';
import { UpdateNotificationPreferencesRequest } from './update-notification-preferences-request';
import { UpdateProfileRequest } from './update-profile-request';

export interface IProfileService {
  updateProfile(request: UpdateProfileRequest): Observable<void>;
  requestEmailChange(request: RequestEmailChangeRequest): Observable<void>;
  confirmEmailChange(request: ConfirmEmailChangeRequest): Observable<void>;
  deleteAccount(request: DeleteAccountRequest): Observable<void>;
  getNotificationPreferences(): Observable<NotificationPreferences>;
  updateNotificationPreferences(request: UpdateNotificationPreferencesRequest): Observable<void>;
}

export const PROFILE_SERVICE = new InjectionToken<IProfileService>('PROFILE_SERVICE');
