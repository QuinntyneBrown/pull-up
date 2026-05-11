import { expect, Locator, Page } from '@playwright/test';

export class HomePagePom {
  readonly filterStrip: Locator;
  readonly createFab: Locator;
  readonly loadingSkeleton: Locator;
  readonly emptyState: Locator;
  readonly errorState: Locator;

  constructor(private readonly page: Page) {
    this.filterStrip = page.getByTestId('home-filter-strip');
    this.createFab = page.getByTestId('home-create-fab');
    this.loadingSkeleton = page.getByTestId('home-loading');
    this.emptyState = page.getByTestId('home-empty');
    this.errorState = page.getByTestId('home-error');
  }

  async expectVisible(): Promise<void> {
    await expect(this.page).toHaveURL(/\/home$/);
    await expect(this.filterStrip).toBeVisible();
  }
}
