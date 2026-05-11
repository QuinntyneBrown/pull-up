export interface RefreshAccessTokenResponse {
  readonly accessToken: string;
  readonly accessTokenExpiresAt: string;
  readonly refreshToken: string;
  readonly refreshTokenExpiresAt: string;
}
