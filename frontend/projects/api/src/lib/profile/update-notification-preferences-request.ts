export interface UpdateNotificationPreferencesRequest {
  readonly newInvitations: boolean;
  readonly eventReminders: boolean;
  readonly rsvpChanges: boolean;
}
