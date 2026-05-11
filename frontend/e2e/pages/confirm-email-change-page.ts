import { Locator, Page } from '@playwright/test';

export class ConfirmEmailChangePagePom {
  readonly pending: Locator;
  readonly success: Locator;
  readonly invalid: Locator;
  readonly missing: Locator;
  readonly profileLink: Locator;

  constructor(private readonly page: Page) {
    this.pending = page.getByTestId('confirm-email-pending');
    this.success = page.getByTestId('confirm-email-success');
    this.invalid = page.getByTestId('confirm-email-invalid');
    this.missing = page.getByTestId('confirm-email-missing');
    this.profileLink = page.getByTestId('confirm-email-profile-link');
  }

  async gotoWithToken(token: string): Promise<void> {
    await this.page.goto(`/email-change/confirm?token=${encodeURIComponent(token)}`);
  }

  async gotoWithoutToken(): Promise<void> {
    await this.page.goto('/email-change/confirm');
  }
}
