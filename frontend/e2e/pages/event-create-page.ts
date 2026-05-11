import { Locator, Page } from '@playwright/test';

export class EventCreatePagePom {
  readonly titleField: Locator;
  readonly locationField: Locator;
  readonly timeField: Locator;
  readonly submitButton: Locator;
  readonly cancelButton: Locator;
  readonly inviteeInput: Locator;
  readonly inviteeAddButton: Locator;
  readonly inviteeChips: Locator;

  constructor(private readonly page: Page) {
    this.titleField = page.getByTestId('create-title');
    this.locationField = page.getByTestId('create-location');
    this.timeField = page.getByTestId('create-time');
    this.submitButton = page.getByTestId('create-submit');
    this.cancelButton = page.getByRole('button', { name: 'Cancel' });
    this.inviteeInput = page.getByTestId('create-invitee-input');
    this.inviteeAddButton = page.getByTestId('create-invitee-add');
    this.inviteeChips = page.getByTestId('create-invitee-chips');
  }

  async goto(): Promise<void> {
    await this.page.goto('/events/new');
    await this.page.waitForSelector('[data-testid="create-title"]');
  }

  async fillBasicFields(title: string, location: string, time: string = '18:00'): Promise<void> {
    await this.titleField.fill(title);
    await this.locationField.fill(location);
    await this.timeField.fill(time);
    await this.page.locator('mat-datepicker-toggle button').click();
    const firstDay = this.page.locator('button.mat-calendar-body-cell:not([aria-disabled="true"])').first();
    await firstDay.click();
  }
}
