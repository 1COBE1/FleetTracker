# FleetTracker UI Design Reference

This document captures the current UI decisions for the .NET MAUI app so new pages and refactors stay visually consistent.

## Design tokens

Tokens live in `MAUI/Resources/Styles/Colors.xaml`.

- **Brand**
  - Primary: `#0B6EF3`
  - Secondary: `#00B894`
- **Neutrals / surfaces**
  - `SurfaceDefault`, `SurfaceSubtle`, `SurfaceElevated`
  - Outlines: `SurfaceOutline`, `SurfaceOutlineStrong`
- **Semantic**
  - Success / Warning / Error / Info + matching `*Background`
- **Spacing**
  - `SpacingXS/S/M/L/XL`
  - `CardPadding`
  - `PageHorizontalPadding`

## Typography roles

Defined in `MAUI/Resources/Styles/Styles.xaml`.

- **Page title**: `PageTitleLabelStyle`
- **Section header**: `SectionHeaderLabelStyle`
- **Card title**: `CardTitleLabelStyle`
- **Meta text**: `MetaLabelStyle`

## Core components / patterns

### Cards

Use:

- `CardStyle` (a `Border` style): elevated surface, rounded corners, subtle shadow.

Applied to:

- Trip list/history items
- Trip details sections (summary/stops/alerts)
- Replay panels (event log + controls)
- Simulator status panel

### Status pill

Use:

- `StatusPillStyle` (`Border`) + `StatusPillLabelStyle` (`Label`)
- Color is driven by `DataTrigger` on status values (e.g. `InProgress`, `Completed`, `Cancelled`)

### Forms

Use:

- `FieldContainerStyle` around `Entry`/`Picker` for consistent padding, outline, and surface.
- Validation is shown via `DisplayAlert("Validation", "...", "OK")` in view-model commands.

## Navigation & page structure

Defined in `MAUI/AppShell.xaml`.

- **Core tabs**: Live Fleet, Trips, History, Start
- **Flyout tools**: Vehicles (Register Vehicle), Simulator
- **Primary actions**: prefer `ToolbarItem` (e.g., Map → Trips) instead of custom in-page headers.

## Map styling & overlays

Maps are Leaflet WebViews in `MAUI/Resources/Raw/`:

- `map.html` (fleet map)
- `trip-map.html` (trip details)
- `replay-map.html` (trip replay)

Conventions:

- Use `--primary` (matches MAUI Primary) for vehicle and route line.
- Stops use semantic colors:
  - Pending: muted
  - Active: warning
  - Completed: success
- A small legend is shown in the bottom-left for quick scanning.

## Page examples

- **Trip lists**
  - `MAUI/Views/TripListPage.xaml`
  - `MAUI/Views/TripHistoryPage.xaml`
- **Details / replay**
  - `MAUI/Views/TripDetailsPage.xaml`
  - `MAUI/Views/TripReplayPage.xaml`

