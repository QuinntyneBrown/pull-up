export interface RegisterUserResponse {
  readonly userId: string;
  readonly email: string;
  readonly fullName: string;
  readonly displayName: string;
  readonly accessToken: string;
  readonly accessTokenExpiresAt: string;
}
