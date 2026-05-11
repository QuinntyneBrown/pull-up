import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api-base-url.token';
import { ConfirmEmailChangeRequest } from './confirm-email-change-request';
import { DeleteAccountRequest } from './delete-account-request';
import { IProfileService } from './profile.service.contract';
import { NotificationPreferences } from './notification-preferences';
import { RequestEmailChangeRequest } from './request-email-change-request';
import { UpdateNotificationPreferencesRequest } from './update-notification-preferences-request';
import { UpdateProfileRequest } from './update-profile-request';

@Injectable({ providedIn: 'root' })
export class ProfileService implements IProfileService {
  constructor(
    private readonly http: HttpClient,
    @Inject(API_BASE_URL) private readonly baseUrl: string,
  ) {}

  updateProfile(request: UpdateProfileRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/api/users/me/profile`, request);
  }

  requestEmailChange(request: RequestEmailChangeRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/api/users/me/email-change`, request);
  }

  confirmEmailChange(request: ConfirmEmailChangeRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/api/users/me/email-change/confirm`, request);
  }

  deleteAccount(request: DeleteAccountRequest): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/users/me`, { body: request });
  }

  getNotificationPreferences(): Observable<NotificationPreferences> {
    return this.http.get<NotificationPreferences>(`${this.baseUrl}/api/users/me/notification-preferences`);
  }

  updateNotificationPreferences(request: UpdateNotificationPreferencesRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/api/users/me/notification-preferences`, request);
  }
}
