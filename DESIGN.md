# ScreenTools Visual Design Standards

**Documentation:** [English](DESIGN.md) · [Français](DESIGN.fr.md) · [简体中文](DESIGN.zh-CN.md)

This document defines implementation standards for ScreenTools. The goal is a restrained, high-quality, native-feeling desktop utility—not a dashboard or a collection of unrelated panels.

## 1. Design direction

The guiding idea is **The Digital Curator**: the interface should recede while the user's content and current task remain prominent. Prefer intentional asymmetry, generous whitespace, and clear grouping over dense grids and decorative chrome.

Product qualities:

- **Restrained:** avoid decorative color, effects, and status indicators without a clear purpose.
- **Precise:** alignments, spacing, icon sizes, and text hierarchy must be consistent.
- **Lightweight:** windows should feel like native desktop tools, not heavy application containers.
- **Operational:** frequent actions are prominent; infrequent settings remain secondary.

## 2. Color and surfaces

- Use neutral grays as the foundation. Do not use pure black (`#000000`) or highly saturated decorative colors.
- `Primary` (`#005EB1`) is reserved for focus, selection, and primary actions. Its active variant is `#00529B`.
- Text tokens: `Text-primary` `#2D3435`, `Text-secondary` `#4F5658`, `Icon-secondary` `#5F6B7A`.
- Surface tokens: `Surface-page` `#F9F9F9`, `Surface-card` `#F2F4F4`, `Surface-chip` `#EEF1F2`, `Surface-glass` `#F0FFFFFF`, `Surface-panel-glass` `#F2FFFFFF`.
- Use surface tone changes to establish hierarchy. Avoid unnecessary hard dividers; use spacing and subtle surface contrast instead.
- When a border is required for accessibility, use `outline-variant` at approximately 10–20% opacity.
- Red is reserved for strong state indicators. Do not introduce decorative colors. A screen should generally use no more than one primary accent and one danger color.

## 3. Materials, depth, and shadows

Use a three-layer model:

1. **Host layer:** transparent or nearly transparent; carries no content and must not become a large solid panel.
2. **Functional floating layer:** carries the actual controls and information; use translucent white or light neutral surfaces with soft boundaries.
3. **Emphasis layer:** reserved for the current selection, primary action, active switch, or essential prompt.

Use Mica or a restrained glass treatment only where it supports the native desktop feel. Keep opacity around 80–90% and avoid excessive blur. Floating layers share one ambient shadow style: low-opacity `on-surface`, approximately 20–28 px blur and 4–8 px depth. Avoid hard, dark, or muddy shadows.

## 4. Typography

Use Segoe UI for Latin text and an appropriate system CJK font for Chinese. Maintain a clear hierarchy:

| Role | Size | Usage |
|---|---:|---|
| Display-LG | 3.5 rem | Rare welcome text or key figures |
| Headline-SM | 1.5 rem | Main feature headings |
| Title-MD | 1.125 rem | Group and settings headings |
| Body-MD | 0.875 rem | Default text and input content |
| Label-SM | 0.6875 rem | Supporting notes and metadata |

For the settings page, use approximately 24 px semibold page titles, 16 px medium group titles, 16 px regular setting titles, 15–16 px descriptions, and 12 px monospaced shortcut labels. For floating HUDs, use a 56–58 px semibold timer and compact 10–14 px labels.

Titles should carry more weight than descriptions. Keep body line height comfortable and make timers, durations, and resolutions visually easy to scan.

## 5. Components and interaction

### Buttons

- Primary: `primary` background with `on-primary` text; 4 px corner radius.
- Secondary: `secondary-container` surface, without a decorative border.
- Hover: a subtle background change. Press: a restrained scale response around `0.98`.

### Inputs

Use either a subtle 2 px `outline-variant` underline or a filled `surface-container-high` surface. Focus uses the primary accent and a slight surface lift.

### Lists and cards

Avoid divider-heavy layouts. Separate items with consistent vertical spacing (approximately `spacing.4`, or 0.9 rem). Selected items may use `surface-container-highest` with a 3 px primary indicator on the left.

### Motion

Keep transitions short and purposeful; avoid animations longer than 200 ms. Prefer a crisp ease-out curve such as `cubic-bezier(0, 0, 0, 1)`.

## 6. Icons, shape, and layout

- Use consistent monochrome line icons with a shared visual weight. Secondary gray is the default; primary blue is reserved for active states.
- Corner radii: 4 px for small buttons and chips, 8 px for medium cards, 12–16 px for floating layers and toolbars, and 16 px for large primary cards. Do not exceed 16 px except for switches, badges, or circular buttons.
- Keep more whitespace between groups and tighter spacing within a group.
- Each window gets one visual focal point: the settings content, the recording timer, or the main action in the recording configuration HUD.

## 7. ScreenTools-specific rules

### Settings page

- Aim for a quiet, reliable interface that remains comfortable during long use.
- Do not turn the settings page into a dashboard or marketing page.
- Reserve global shortcuts for screenshot, recording, and replay. Keep low-frequency options—output path, startup behavior, audio preferences, and quality—in settings.

### Recording HUD

- It should feel like a lightweight desktop tool, not a standalone application window.
- Keep the background and information layer visually light.
- Make the current mode unmistakable; secondary tools should recede.
- Do not crowd the toolbar with icons.

### Active-recording overlay

- The timer is the visual focal point.
- Volume or audio indicators provide feedback without competing with the timer.
- Pause and stop must be immediately accessible without making the overlay bulky.
- A secondary or more-actions control may exist, but should remain visually quiet.

## 8. Prohibited patterns and implementation order

Avoid stacked gray panels, multiple competing shadow systems, too many blue surfaces on one screen, meaningless borders, shortcut bindings for every feature, and inconsistent icon styles.

Implement visual changes in this order:

1. Layout and whitespace.
2. Text hierarchy.
3. Color and materials.
4. Icons and shadows.

The interface should support the work and then get out of the way. The best design is one the user barely notices.