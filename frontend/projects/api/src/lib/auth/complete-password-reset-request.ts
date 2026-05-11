export interface CompletePasswordResetRequest {
  readonly token: string;
  readonly newPassword: string;
}
