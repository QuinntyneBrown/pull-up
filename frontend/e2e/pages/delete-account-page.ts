import { expect, Locator, Page } from '@playwright/test';

export class DeleteAccountPagePom {
  readonly passwordField: Locator;
  readonly confirmCheckbox: Locator;
  readonly submitButton: Locator;
  readonly serverError: Locator;
  readonly backButton: Locator;

  constructor(private readonly page: Page) {
    this.passwordField = page.getByTestId('delete-password');
    this.confirmCheckbox = page.getByTestId('delete-confirm');
    this.submitButton = page.getByTestId('delete-submit');
    this.serverError = page.getByTestId('delete-server-error');
    this.backButton = page.getByTestId('back-button');
  }

  async goto(): Promise<void> {
    await this.page.goto('/profile/delete');
    await expect(this.page.getByRole('heading', { name: 'Delete account', level: 1 })).toBeVisible();
  }

  async fillAndSubmit(password: string): Promise<void> {
    await this.passwordField.fill(password);
    await this.confirmCheckbox.click();
    await this.submitButton.click();
  }
}
