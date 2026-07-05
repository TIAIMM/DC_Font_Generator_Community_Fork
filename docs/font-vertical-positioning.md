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
can participate in text placement. A generated space glyph with `fHeight = 1`
is suspicious because it does not represent the font's real line box.

## Current Generator Implications

The generator currently computes normal glyph metrics and then applies generated
top-edge calibration to non-empty glyphs. However, the generated space glyph is
special-cased with:

```text
fHeight = 1
fWidth = 1
fTopEdge = 0
```

Since the game can consume `pFontLetters[32].fHeight`, this special case can
cause a global vertical bias in UI paths that use the space glyph as a line-box
reference. This explains why adjusting `Base Line` or normal glyph `Top Edge`
may not visibly fix some text blocks.

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
   Its `fHeight` should match the generated font's line box closely enough for
   `PrepText` paths that use `space.fHeight`.

5. Do not write automatic calibration into `fTopEdgeFixed` or
   `fBaseLineFixed`.
   Fixed fields represent manual/project amendments and should remain separate
   from generated baseline/top-edge calculations.

6. Quantize generated metrics in the same direction as the game consumes them.
   The game casts relevant values to integers, so generated float values should
   be chosen with that integer result in mind.

## Space Glyph Normalization

Generated fonts normalize the space glyph after normal glyph metrics are known.

The current approach is:

1. Generate all glyphs and compute `fBaseLine`.
2. Compute representative line drop from valid non-space glyphs:

   ```text
   lineDrop = max(fHeight - fTopEdge)
   ```

3. Set the space glyph's vertical metrics to represent the same line box:

   ```text
   space.fTopEdge = fBaseLine
   space.fHeight = fBaseLine + lineDrop
   ```

4. Keep space width/spacing logic separate from vertical metrics.

This keeps the space glyph invisible horizontally while giving it vertical data
that matches how the game uses character 32.

## Open Verification Points

These points should be verified in game after any vertical fix:

- Single-line English, Chinese, and mixed text.
- Multi-line English, Chinese, and mixed text.
- Pip-Boy list rows, selected row boxes, tooltip text, and description panels.
- UI paths that appear unaffected by manual `Base Line` changes.
- Generated `.fnt` reload in the tool, confirming `fBaseLine`, `fTopEdge`, and
  `fHeight` are serialized and displayed consistently.
