import { expect, Locator, Page } from '@playwright/test';

export class RequestPasswordResetPagePom {
  readonly email: Locator;
  readonly submit: Locator;
  readonly success: Locator;
  readonly backLink: Locator;

  constructor(private readonly page: Page) {
    this.email = page.getByTestId('reset-email');
    this.submit = page.getByTestId('reset-submit');
    this.success = page.getByTestId('reset-success');
    this.backLink = page.getByTestId('reset-back');
  }

  async goto(): Promise<void> {
    await this.page.goto('/password-reset');
    await expect(this.page.getByRole('heading', { name: 'Reset your password' })).toBeVisible();
  }

  async fillAndSubmit(email: string): Promise<void> {
    await this.email.fill(email);
    await this.submit.click();
  }
}
