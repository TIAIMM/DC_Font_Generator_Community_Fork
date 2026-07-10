# Font Vertical Positioning Notes

This document records how Fallout consumes vertical font metrics, and how the
generator should reason about `fBaseLine`, `fTopEdge`, `fHeight`, and the space
glyph. It is based on the current project code, tNVSE field naming, and local
decompilation notes.

## Serialized Fields

The `.fnt` format stores one `FontData` header followed by `FontLetter`
records. The relevant fields are:

- `FontData::fBaseLine`
- `FontLetter::fWidth`
- `FontLetter::fHeight`
- `FontLetter::fLeadingEdge`
- `FontLetter::fSpacing`
- `FontLetter::fTopEdge`

The project code maps these fields in `Fnt_Header` and `Fnt_char`. The old UI
name "Line Height" is misleading for `fBaseLine`; the game uses it as a line
rise/ascent value, not as total line height.

## Game Semantics Confirmed From Decompiled Code

### `fBaseLine` Is Line Rise

The game converts `FontData::fBaseLine` into `CharData::iRise`.

Conceptually:

```text
iRise = int(FontData::fBaseLine)
```

This means `fBaseLine` controls the rise from the line origin, but it does not
alone define where every glyph appears vertically.

### Character Drop Comes From `fHeight - fTopEdge`

For an individual character, the game derives the downwards part of the line box
from the glyph record:

```text
iDrop = int(FontLetter::fHeight - FontLetter::fTopEdge)
```

`TextPage::AddChar` uses the same idea when updating the last font height:

```text
iLastFontHeight = int(fBaseLine) + int(fHeight - fTopEdge)
```

So the effective vertical box is split into:

- rise: `fBaseLine`
- drop: `fHeight - fTopEdge`

Changing only `fBaseLine` cannot reliably fix all vertical placement problems.
If `fTopEdge` or `fHeight` is wrong, the character can still sit too high or too
low inside the line.

### New Lines Use `fBaseLine`

`Font::PrepText` moves to the next line by subtracting `fBaseLine` from the
current vertical position in one wrapping/newline path.

Conceptually:

```text
nextLineZ = currentZ - fBaseLine
```

This confirms that `fBaseLine` affects inter-line stepping. It also means line
spacing changes can affect multi-line layout even when they do not visibly fix
the glyph's position inside a single line.

### Space Glyph Height Is Not Ignored

The decompiled `Font::PrepText` reads character 32 directly:

```text
pFontLetters[32].fHeight
```

One path uses this value together with `fBaseLine` to adjust the initial vertical
position:

```text
z = -(((space.fHeight - fBaseLine) * 2.0) - z)
```

Another path reads both:

```text
space.fWidth
space.fHeight
```

Therefore the space glyph is not just an invisible advance. Its vertical metric
can participate in text placement. The inspected vanilla fonts serialize normal
spaces with zero width and zero vertical metrics, while keeping their horizontal
advance in `fSpacing`.

## Current Generator Implications

The generator computes normal glyph metrics and then applies generated top-edge
calibration to non-empty glyphs. Generated spaces follow the inspected vanilla
font convention:

```text
fWidth = 0
fHeight = 0
fTopEdge = 0
fLeadingEdge = 0
fSpacing = measured space advance
```

This keeps the space invisible while preserving `fWidth + fSpacing`, and avoids
feeding a synthetic baseline-plus-drop height into game paths that read
`pFontLetters[32].fHeight` directly.

The current automatic top-edge calibration is also a generator-side heuristic:

```text
actualCenter = fHeight / 2 - fTopEdge
targetCenter = (fontDescent - fontAscent) / 2
```

The decompiled game code confirms how the fields are consumed, but it does not
prove that this Skia font-design center is the correct visual target for Fallout
UI. Treat it as a calibration heuristic, not as a confirmed game rule.

## Practical Rules For Fixes

Use these rules when changing vertical generation logic:

1. Do not use transparent bitmap padding as the main vertical fix.
   Padding changes atlas space and can hide the real metric problem.

2. Do not assume `fBaseLine` is total line height.
   It is the rise/ascent part of the line.

3. Keep `fHeight - fTopEdge` meaningful.
   This value is the glyph drop used by the game.

4. Treat space character 32 as a real metric participant.
   Match the vanilla serialized convention (`fHeight = 0`, `fTopEdge = 0`) so
   `PrepText` paths do not receive an artificial line-box height.

5. Do not write automatic calibration into `fTopEdgeFixed` or
   `fBaseLineFixed`.
   Fixed fields represent manual/project amendments and should remain separate
   from generated baseline/top-edge calculations.

6. Quantize generated metrics in the same direction as the game consumes them.
   The game casts relevant values to integers, so generated float values should
   be chosen with that integer result in mind.

## Space Glyph Serialization

Generated spaces do not receive a bitmap or vertical line-box metrics. Their
advance is stored entirely in `fSpacing`, matching the inspected vanilla fonts:

```text
space.fWidth = 0
space.fHeight = 0
space.fTopEdge = 0
space.fLeadingEdge = 0
space.fSpacing = targetSpaceAdvance
```

This rule is independent of normal glyph drop calculations and effect bounds.

## Open Verification Points

These points should be verified in game after any vertical fix:

- Single-line English, Chinese, and mixed text.
- Multi-line English, Chinese, and mixed text.
- Pip-Boy list rows, selected row boxes, tooltip text, and description panels.
- UI paths that appear unaffected by manual `Base Line` changes.
- Generated `.fnt` reload in the tool, confirming `fBaseLine`, `fTopEdge`, and
  `fHeight` are serialized and displayed consistently.
