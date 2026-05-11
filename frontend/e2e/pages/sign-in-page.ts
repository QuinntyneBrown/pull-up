import { expect, Locator, Page } from '@playwright/test';

export class SignInPagePom {
  readonly email: Locator;
  readonly password: Locator;
  readonly submit: Locator;
  readonly serverError: Locator;
  readonly forgotLink: Locator;

  constructor(private readonly page: Page) {
    this.email = page.getByTestId('signin-email');
    this.password = page.getByTestId('signin-password');
    this.submit = page.getByTestId('signin-submit');
    this.serverError = page.getByTestId('signin-server-error');
    this.forgotLink = page.getByTestId('signin-forgot');
  }

  async goto(): Promise<void> {
    await this.page.goto('/sign-in');
    await expect(this.page.getByRole('heading', { name: 'Sign in' })).toBeVisible();
  }

  async fillAndSubmit(email: string, password: string): Promise<void> {
    await this.email.fill(email);
    await this.password.fill(password);
    await this.submit.click();
  }
}
