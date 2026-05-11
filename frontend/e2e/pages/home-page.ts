import { expect, Locator, Page } from '@playwright/test';

export class HomePagePom {
  readonly welcome: Locator;
  readonly avatar: Locator;
  readonly signOut: Locator;

  constructor(private readonly page: Page) {
    this.welcome = page.getByTestId('home-welcome');
    this.avatar = page.getByTestId('home-avatar');
    this.signOut = page.getByTestId('home-sign-out');
  }

  async expectVisible(displayName: string): Promise<void> {
    await expect(this.page).toHaveURL(/\/home$/);
    await expect(this.welcome).toBeVisible();
    await expect(this.welcome).toContainText(`Welcome, ${displayName}!`);
  }
}
