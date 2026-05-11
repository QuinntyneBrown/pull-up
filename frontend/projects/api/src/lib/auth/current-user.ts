export interface CurrentUser {
  readonly userId: string;
  readonly email: string;
  readonly fullName: string;
  readonly displayName: string;
  readonly role: string;
  readonly createdAt: string;
}
