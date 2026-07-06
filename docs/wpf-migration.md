# WPF Migration Preparation

The complete WinForms baseline is preserved at:

- Branch: `codex/winforms-backup`
- Tag: `winforms-backup-20260706`

The active migration branch is `codex/wpf-prep`.

## Project Layout

- `DC Font Generator`: existing WinForms application. Keep this buildable while the WPF UI is developed.
- `DC Font Generator.Core`: future home for generation, serialization, texture, project, and glyph workflow services.
- `DC Font Generator.Wpf`: WPF shell that will replace the WinForms presentation layer after the core extraction is complete.

## Migration Boundary

The WPF app should not depend on `MainForm`, WinForms controls, or designer state. Move reusable logic from the WinForms project into `DC Font Generator.Core` in small batches, then bind WPF view models to the Core DTOs and services.

Recommended extraction order:

1. Pure codecs and image buffers: `Bgra32Image`, `TexturePixelCodec`, `FntBinaryCodec`.
2. Font model and metrics: `Fnt_Header`, `Fnt_char`, `FL_FONT`, `GameFontMetricQuantizer`.
3. Rendering and atlas services: `DrawFont`, `FontGenerationServices`, render surface helpers.
4. Project and workflow services.
5. WPF views and view models for Font, Adjust, Advance, INI, and Log tabs.

Keep `.fnt`, `.Tex`, and `.project.xml` formats unchanged during the migration.
