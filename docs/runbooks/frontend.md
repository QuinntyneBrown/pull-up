# Frontend runbook — Pull Up Angular

The frontend is an Angular 21 standalone-components workspace under `./frontend/` with three libraries (`api`, `components`, `domain`) plus the `pull-up` application, theming built on **Angular Material (Material 3)**, BEM class names, design tokens, and a Playwright Page-Object-Model acceptance test for the sample flow. This runbook is for developers running it on a workstation.

## Prerequisites (one-time)

1. **Node.js 22** (or any active LTS — repo was scaffolded on 22.21). Confirm with `node --version`.
2. **npm 10** ships with Node 22.
3. **Playwright browsers** (Chromium). Installed by `npx playwright install chromium` during E2E setup.
4. The **backend API** running on `http://localhost:5080` if you want live data (see `./docs/runbooks/backend.md`). The E2E tests mock the backend so they do not need a running API.

## First-time setup

```
cd frontend
npm install
npx playwright install chromium
```

## Run the application

```
cd frontend
npm start            # → ng serve pull-up on http://localhost:4200
```

The dev server hot-reloads on save. Open `http://localhost:4200`; the default route redirects to `/sign-up`. Fill the form, submit; on a successful response the app navigates to `/home` and renders the current user fetched from `GET /api/users/me`.

## Production build

```
cd frontend
npm run build
```

The output goes to `frontend/dist/pull-up`. The build is clean — `ng build pull-up` reports **0 errors, 0 warnings** on the MVP code.

## Workspace layout (libraries → app dependency direction)

```
frontend/projects/
  api/                              # Models + backend-talking services. Depends on nothing.
    src/lib/api-base-url.token.ts   #   API_BASE_URL InjectionToken
    src/lib/auth/
      register-user-request.ts      #   transport DTO
      register-user-response.ts     #   transport DTO
      current-user.ts               #   transport DTO
      auth.service.contract.ts      #   IAuthService interface + AUTH_SERVICE token
      auth.service.ts               #   concrete AuthService (HttpClient implementation)
  components/                       # Reusable presentation components. Depends on nothing.
    src/lib/brand-logo/             #   .ts + .html + .scss triple
  domain/                           # api-consuming components. Depends on api + components.
    src/lib/sign-up-page/           #   uses AuthService via AUTH_SERVICE token
    src/lib/home-page/              #   uses AuthService via AUTH_SERVICE token
  pull-up/                          # Main app. Depends on api + components + domain.
    src/app/app.ts / .html / .scss  #   root <router-outlet />
    src/app/app.config.ts           #   providers: http, animations, router, auth wiring
    src/app/app.routes.ts           #   /sign-up, /home (guarded)
    src/app/auth.guard.ts           #   functional CanActivate
    src/app/auth-jwt.interceptor.ts #   HttpInterceptorFn — attaches Authorization
    src/styles.scss                 #   Material 3 theme + design tokens
```

The TypeScript path aliases in `frontend/tsconfig.json` point at each library's `public-api.ts` so the app consumes libraries directly from source — no `ng build api` step is needed during development.

### Interface-driven service consumption

Every service that other libraries / the app might depend on exposes an interface and an injection token (the pattern from https://github.com/QuinntyneBrown/interface-driven-service-consumption):

- `projects/api/src/lib/auth/auth.service.contract.ts` exports `IAuthService` (interface) and `AUTH_SERVICE` (InjectionToken).
- `projects/api/src/lib/auth/auth.service.ts` exports the concrete `AuthService implements IAuthService`.
- `projects/pull-up/src/app/app.config.ts` wires `{ provide: AUTH_SERVICE, useExisting: AuthService }` so consumers receive the concrete via the token.

Consumers (e.g. `SignUpPageComponent`, `HomePageComponent`) inject `@Inject(AUTH_SERVICE) auth: IAuthService` — they never reference the concrete class. This keeps testability high and decouples domain components from infrastructure.

## Material 3

`projects/pull-up/src/styles.scss` applies the M3 theme:

```scss
@use '@angular/material' as mat;

html {
  @include mat.theme((
    color: (
      primary: mat.$violet-palette,
      tertiary: mat.$rose-palette,
    ),
    typography: Roboto,
    density: 0,
  ));
}
```

Components consume design tokens via the generated `--mat-sys-*` CSS variables (e.g. `var(--mat-sys-primary)`, `var(--mat-sys-on-surface)`, `var(--mat-sys-body-large)`). All BEM class names are local to the component's `.scss` file; no utility classes from Tailwind / Bootstrap are used.

Material Symbols icons + Roboto are loaded from Google Fonts in `index.html`.

## Sample slice (the MVP reference flow)

The slice is the user sign-up → home pattern:

1. `GET /sign-up` lazy-loads `SignUpPageComponent` (from `domain`). The component renders Material 3 form fields (BEM `sign-up-page__field`), validates client-side with reactive forms, and submits to `AuthService.register()`.
2. On 201, the `AuthService` stores the JWT in `localStorage` under `pullup.access-token` and emits the new value on `accessToken$`. The component navigates to `/home`.
3. `/home` is guarded by `authGuard` (`/sign-up` if no token). It lazy-loads `HomePageComponent` which calls `AuthService.loadCurrentUser()`. The `authJwtInterceptor` attaches `Authorization: Bearer <token>` to that request.
4. The response populates the welcome card — "Welcome, {displayName}!" + avatar initial + role.

Sign-out clears the stored token and navigates back to `/sign-up`.

## E2E (Playwright POM)

Tests live in `frontend/e2e/`:

- `playwright.config.ts` — runs against `http://localhost:4204` with two projects: Pixel-7 mobile and 1440-wide desktop. Starts its own `ng serve` on port 4204 so it never collides with another dev server already on 4200.
- `pages/sign-up-page.ts` — POM for the sign-up screen (POM = Page Object Model).
- `pages/home-page.ts` — POM for the authenticated home screen.
- `sign-up-flow.spec.ts` — two tests:
  - happy path: sign up → land on home → welcome card visible,
  - mobile-first: sign-up shell renders correctly at 360 px width with no horizontal overflow.

The backend is mocked via `page.route('**/api/users', ...)` and `page.route('**/api/users/me', ...)` so tests run without the .NET API.

```
cd frontend
npm run e2e
```

Result on the MVP code: **4 passed** (2 projects × 2 tests) in ~12 s.

## Common tasks

- **Add a new component** in a library: keep `.ts`, `.html`, `.scss` separate (one type per file). Add the component to that library's `public-api.ts` so consumers can import via the path alias.
- **Add a new backend-talking service**: place it under `projects/api/src/lib/<feature>/`, pair `<name>.service.ts` with `<name>.service.contract.ts` (interface + injection token), wire `{ provide: <TOKEN>, useExisting: <Class> }` in `app.config.ts`.
- **Add a new authenticated route**: register it in `app.routes.ts` with `canActivate: [authGuard]` and `loadComponent: () => import('domain').then(m => m.<PageComponent>)`.

## Troubleshooting

- *Playwright tests open a different app.* Another Angular dev server may already own port 4200. The Playwright config pins port 4204 with `reuseExistingServer: false`, so this only happens if you change the config; if you do, pick another unused port.
- *`@angular/animations/browser` cannot be resolved.* You forgot `npm install @angular/animations`; this is a separate peer dependency.
- *CORS error against the backend.* `Program.cs` adds `http://localhost:4200` as an allowed origin. If you run the frontend on a different port, update `Cors.AddPolicy` accordingly.
