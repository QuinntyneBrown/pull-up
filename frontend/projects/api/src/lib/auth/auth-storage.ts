import { Injectable } from '@angular/core';
import { CurrentUser } from './current-user';

const ACCESS_KEY = 'pullup.access-token';
const REFRESH_KEY = 'pullup.refresh-token';
const USER_KEY = 'pullup.current-user';

@Injectable({ providedIn: 'root' })
export class AuthStorage {
  getAccessToken(): string | null {
    return this.read(ACCESS_KEY);
  }

  setAccessToken(token: string | null): void {
    this.write(ACCESS_KEY, token);
  }

  getRefreshToken(): string | null {
    return this.read(REFRESH_KEY);
  }

  setRefreshToken(token: string | null): void {
    this.write(REFRESH_KEY, token);
  }

  getUser(): CurrentUser | null {
    const raw = this.read(USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as CurrentUser;
    } catch {
      return null;
    }
  }

  setUser(user: CurrentUser | null): void {
    this.write(USER_KEY, user === null ? null : JSON.stringify(user));
  }

  clear(): void {
    this.setAccessToken(null);
    this.setRefreshToken(null);
    this.setUser(null);
  }

  private read(key: string): string | null {
    if (typeof localStorage === 'undefined') return null;
    return localStorage.getItem(key);
  }

  private write(key: string, value: string | null): void {
    if (typeof localStorage === 'undefined') return;
    if (value === null) {
      localStorage.removeItem(key);
    } else {
      localStorage.setItem(key, value);
    }
  }
}
