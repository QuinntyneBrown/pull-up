import { expect, Locator, Page } from '@playwright/test';

export class SignUpPagePom {
  readonly fullName: Locator;
  readonly email: Locator;
  readonly password: Locator;
  readonly submit: Locator;
  readonly serverError: Locator;

  constructor(private readonly page: Page) {
    this.fullName = page.getByTestId('signup-fullname');
    this.email = page.getByTestId('signup-email');
    this.password = page.getByTestId('signup-password');
    this.submit = page.getByTestId('signup-submit');
    this.serverError = page.getByTestId('signup-server-error');
  }

  async goto(): Promise<void> {
    await this.page.goto('/sign-up');
    await expect(this.page.getByRole('heading', { name: 'Create your account' })).toBeVisible();
  }

  async fillAndSubmit(fullName: string, email: string, password: string): Promise<void> {
    await this.fullName.fill(fullName);
    await this.email.fill(email);
    await this.password.fill(password);
    await this.submit.click();
  }
}
