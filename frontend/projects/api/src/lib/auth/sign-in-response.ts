export interface SignInResponse {
  readonly userId: string;
  readonly email: string;
  readonly fullName: string;
  readonly displayName: string;
  readonly role: string;
  readonly accessToken: string;
  readonly accessTokenExpiresAt: string;
  readonly refreshToken: string;
  readonly refreshTokenExpiresAt: string;
}
