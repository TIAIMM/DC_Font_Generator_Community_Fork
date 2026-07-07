# WinUI 3 Migration

The WinUI 3 application is the active UI shell. Historical UI code has been removed from the active project layout after the Core extraction and WinUI migration.

The active migration branch is `winui3_develop`.

## Project Layout

- `DC Font Generator.Core`: shared home for generation, serialization, texture, project, and glyph workflow services.
- `DC Font Generator.WinUI`: WinUI 3 desktop application. It owns XAML views, view models, file pickers, dialogs, `WriteableBitmap` image conversion, dispatcher progress, and request adapters for Core workflows.

## Migration Boundary

The WinUI 3 app must not depend on legacy forms, controls, or designer state. Reusable logic belongs in `DC Font Generator.Core`; WinUI view models bind to Core DTOs and services through WinUI-specific adapters.

Keep `.fnt`, `.Tex`, and `.project.xml` formats unchanged during the migration.
