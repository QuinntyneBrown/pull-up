import { expect, Locator, Page } from '@playwright/test';

export class EventDetailPagePom {
  readonly title: Locator;
  readonly hero: Locator;
  readonly rsvpPicker: Locator;
  readonly goingCount: Locator;
  readonly cancelledBanner: Locator;
  readonly editButton: Locator;
  readonly hostActions: Locator;
  readonly cancelButton: Locator;

  constructor(private readonly page: Page) {
    this.title = page.getByTestId('detail-title');
    this.hero = page.getByTestId('detail-hero');
    this.rsvpPicker = page.getByTestId('detail-rsvp-picker');
    this.goingCount = page.getByTestId('detail-going-count');
    this.cancelledBanner = page.getByTestId('detail-cancelled-banner');
    this.editButton = page.getByTestId('detail-edit-button');
    this.hostActions = page.getByTestId('detail-host-actions');
    this.cancelButton = page.getByTestId('detail-cancel-button');
  }

  async goto(eventId: string): Promise<void> {
    await this.page.goto(`/events/${eventId}`);
    await expect(this.hero).toBeVisible();
  }
}
