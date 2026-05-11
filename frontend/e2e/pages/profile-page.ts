import { expect, Locator, Page } from '@playwright/test';

export class ProfilePagePom {
  readonly fullName: Locator;
  readonly displayName: Locator;
  readonly email: Locator;
  readonly editButton: Locator;
  readonly editFullName: Locator;
  readonly editDisplayName: Locator;
  readonly editSave: Locator;
  readonly editCancel: Locator;

  constructor(private readonly page: Page) {
    this.fullName = page.getByTestId('profile-fullname');
    this.displayName = page.getByTestId('profile-displayname');
    this.email = page.getByTestId('profile-email');
    this.editButton = page.getByTestId('profile-edit-button');
    this.editFullName = page.getByTestId('edit-fullname');
    this.editDisplayName = page.getByTestId('edit-displayname');
    this.editSave = page.getByTestId('edit-save');
    this.editCancel = page.getByTestId('edit-cancel');
  }

  async goto(): Promise<void> {
    await this.page.goto('/profile');
    await expect(this.page.getByRole('heading', { name: 'Your details' })).toBeVisible();
  }

  async openEdit(): Promise<void> {
    await this.editButton.click();
    await expect(this.page.getByRole('heading', { name: 'Edit profile' })).toBeVisible();
  }

  async editAndSave(fullName: string, displayName: string): Promise<void> {
    await this.editFullName.fill(fullName);
    await this.editDisplayName.fill(displayName);
    await this.editSave.click();
  }
}
