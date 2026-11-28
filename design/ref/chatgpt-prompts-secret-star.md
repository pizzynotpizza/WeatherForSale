# ChatGPT prompt: the Secret Star (Tempest's special star)

The secret stage's star: up to 3 are earned by sealing Tempest fast (like the map stars, by time). It must sit next to the
game's normal gold stars (StarBig / StarSmall) and read as rarer at a glance: same chunky
cartoon star shape and outline weight, a different colour.

Save the result as `Assets/Art/UI/Story/StarSecret.png`, then run
**Weather For Sale > Build Front End Scenes** and the boss scene builder. The game picks it up by
file name (the boss Victory panel and the star under the secret slot on Level Select). Until the
file exists, the game shows the gold star tinted violet.

## Prompt

```
A single game UI star icon for a cozy cartoon fantasy game, front view, centred, on a fully
transparent background (PNG with alpha), 1024 x 1024.

Shape: a chunky, slightly puffy five-pointed star with softly rounded tips, thick dark-purple
outline (about 6% of the width), the same style as a glossy mobile-game reward star.

Colour: a deep storm-violet to electric-magenta gradient (top-left light lavender #C9A7FF,
middle violet #8A4DFF, bottom-right deep indigo #3B1C8C), with a glossy white highlight
across the upper-left arm and a soft inner glow.

Detail: a small golden lightning bolt engraved in the centre of the star, and a faint rainbow
shimmer along the outline (a hint of the rainbow bottle that seals the storm). Two or three
tiny sparkles around the star.

Style: polished cel-shaded 2D game asset, clean edges, no text, no background, no drop shadow
outside the star, no frame. Matches bright, warm, storybook UI.
```

## Checks before saving

- Transparent background (no white or checkerboard pixels baked in).
- The star fills about 90% of the canvas; crop extra empty space.
- Readable at 76 px (Level Select) and 150 px (Victory panel): the lightning bolt can be simple.
