# Project Art Style — FindRC77 Find Hidden mini game [v1]

## Style brief
Soft modern anime / cel-shaded illustration matching the main game's level art: clean thin dark-brown line art
(no heavy black outlines), smooth soft gradient shading, warm bright lighting, pastel-but-saturated colors, cute
chibi-proportioned people and animals, cozy and cheerful mood. Mini game scenes use a high 3/4 top-down (slightly
isometric) camera with a dense, edge-to-edge environment so small objects can hide among props: paths, fences,
bushes, flower beds, stalls, benches, lamp posts. Absent: photorealism, pixel art, hard black outlines, dark or
gritty tones, text/letters/logos inside the scene.

## Palette
- Cream path: `#F3E6D8`
- Warm peach / skin: `#F6C9A8`
- Foliage green: `#7CC24A`
- Mint / leaf light: `#B8E07A`
- Sky / water blue: `#7FCBEB`
- Coral pink (awnings, flowers): `#F08A8A`
- Balloon red: `#E8453C`
- Butter yellow (bananas, lights): `#F7D35A`
- Wood brown: `#B07A45`
- Line art dark brown: `#4A3226`
- Winter variant: snow white `#F7F9FC`, icy blue `#BFD9F2`, pine green `#2F6B4A`, cranberry `#C8323A`, warm gold glow `#FFC861`

## References
- docs/target-reskin/style-level1.png, style-level2.png — copies of the main game's level art; anchor line, shading and palette
- docs/ui-ref/hidden-object-layout.jpg — hidden-object reference video frame; LAYOUT / camera / density only, do NOT copy its look
- docs/concept-reskin/minigame-zoo-concept.png — locked direction (Level 1 Zoo Day, option A)
- docs/concept-reskin/minigame-christmas-concept.png — locked direction (Level 2 Christmas Village)
- docs/minigame-levels/LEVELS.md — item lists and counts per level

## Defaults
- pixelsPerUnit: 100
- Level background: 1536x1024 concept; production 3072x2048 if the map pans/zooms
- Hidden item sprite: 256x256 (displayed ~60-90px on the 1536-wide map)
- Tray icon (bottom bar): 256x256
- Square chip / icon: 512x512
- Round icon button: 384x384
- Pill button: 1024x320
- Transparent background: yes (except full-screen level backgrounds)

## Slicing rules (sprite separation)

When slicing UI into individual sprites, **never bake labels or glyphs into the background**:

- **Buttons / chips / capsules / badges / ribbons / tiles that hold text** → generate the background sprite EMPTY (no number, no label, no word). Text is supplied via TMP / UnityUI Text on top.
- **Buttons / chips / tiles that hold an icon or character** → generate the background and the icon as TWO separate transparent PNGs. The icon is overlaid in the prefab.
- **Decorative chrome with no label or glyph** (paint chips, board frame, plain capsules) ships as a single sprite.
- **Hidden items** → each item type is its own transparent sprite, drawn in the same style and lighting as the level so it blends in when placed on the background.

Why: labels need to be localizable, numbers change at runtime, icons need to be theme-tintable, and the same button base should be reusable under different icons. Baked text/icons break all of that.
