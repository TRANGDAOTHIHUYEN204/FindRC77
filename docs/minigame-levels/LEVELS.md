# Find Hidden mini game: level design

Style: matches the main game's art (`Assets/GameAssets/Level 1/1_A.png`, `Level 2/2_A.png`). Soft anime / cel-shaded,
thin dark-brown line art, soft gradients, bright pastel-saturated palette. Layout borrowed from the hidden-object
reference video: high 3/4 top-down view, dense scene, objects tucked into the environment.

Canvas: 1536x1024 concept (landscape). Upscale or regenerate at 2x for production if the map is pannable/zoomable.

## Level 1: Zoo Day (`docs/concept-reskin/minigame-zoo-concept.png`, option A, locked; `-alt.png` is the rejected option B)

| Group | Item (FindHiddenData) | Count |
|---|---|---|
| Animals | Monkey | 3 |
| Animals | Penguin | 3 |
| Animals | Parrot | 3 |
| Animals | Giraffe | 1 |
| Animals | Lion | 1 |
| Zoo tools | Feeding bucket | 3 |
| Zoo tools | Binoculars | 2 |
| Toys / food | Red balloon | 4 |
| Toys / food | Teddy bear | 3 |
| Toys / food | Ice cream cone | 4 |
| Toys / food | Banana | 5 |
| | **Total** | **32** |

Areas: monkey island, penguin pool, parrot aviary, giraffe pen, lion cave, snack kiosk, gift shop, café tables.

## Level 2: Christmas Village (`docs/concept-reskin/minigame-christmas-concept.png`)

| Group | Item (FindHiddenData) | Count |
|---|---|---|
| Gifts / sweets | Gift box | 5 |
| Gifts / sweets | Candy cane | 5 |
| Gifts / sweets | Gingerbread man | 4 |
| Decorations | Ornament ball | 4 |
| Decorations | Golden bell | 3 |
| Decorations | Christmas stocking | 3 |
| Decorations | Golden star | 1 |
| Characters / wear | Santa hat | 3 |
| Characters / wear | Snowman | 3 |
| Characters / wear | Reindeer | 2 |
| | **Total** | **33** |

Areas: Christmas tree (center), ice-skating pond, Christmas market stalls, cottages, Santa's sleigh, reindeer pen.

## Notes

- The AI-generated concepts do not match the counts above exactly. Final counts are whatever is actually placed
  as points in each `PointSpawnItem` asset.
- Items are drawn into the concept. The current code spawns item sprites on top of a background, so production needs
  either (a) a clean background plus separate item sprites, or (b) the baked art plus invisible hit areas, with a
  cropped item sprite flying to the tray on collect.
