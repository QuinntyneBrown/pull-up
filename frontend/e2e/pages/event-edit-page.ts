import { expect, Locator, Page } from '@playwright/test';

export class EventEditPagePom {
  readonly submitButton: Locator;
  readonly titleInput: Locator;
  readonly locationInput: Locator;
  readonly inviteeInput: Locator;
  readonly inviteeAddButton: Locator;
  readonly existingChips: Locator;
  readonly pendingChips: Locator;

  constructor(private readonly page: Page) {
    this.submitButton = page.getByTestId('edit-submit');
    this.titleInput = page.getByTestId('edit-title');
    this.locationInput = page.getByTestId('edit-location');
    this.inviteeInput = page.getByTestId('edit-invitee-input');
    this.inviteeAddButton = page.getByTestId('edit-invitee-add');
    this.existingChips = page.getByTestId('edit-existing-chips');
    this.pendingChips = page.getByTestId('edit-pending-chips');
  }

  async goto(eventId: string): Promise<void> {
    await this.page.goto(`/events/${eventId}/edit`);
    await expect(this.submitButton).toBeVisible();
  }
}
