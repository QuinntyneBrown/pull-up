import { expect, Locator, Page } from '@playwright/test';

export class CompletePasswordResetPagePom {
  readonly password: Locator;
  readonly submit: Locator;
  readonly serverError: Locator;

  constructor(private readonly page: Page) {
    this.password = page.getByTestId('confirm-password');
    this.submit = page.getByTestId('confirm-submit');
    this.serverError = page.getByTestId('confirm-server-error');
  }

  async gotoWithToken(token: string): Promise<void> {
    await this.page.goto(`/password-reset/confirm?token=${encodeURIComponent(token)}`);
    await expect(this.page.getByRole('heading', { name: 'Choose a new password' })).toBeVisible();
  }

  async fillAndSubmit(newPassword: string): Promise<void> {
    await this.password.fill(newPassword);
    await this.submit.click();
  }
}
