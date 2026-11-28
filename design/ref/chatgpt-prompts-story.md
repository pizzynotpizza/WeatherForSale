# ChatGPT image prompts — Story CGI, Mode Select, Level Select, map pictures

**How to use**
- Attach the character sheets when you ask ChatGPT (`design/ref/characters/Sam/Sam_Turnaround.png`,
  `UncleJohn/UncleJohn_Turnaround.png`, `Tempest/Tempest_Turnaround.png`) and the cover
  `design/ref/Title.jpg`, so the faces, clothes and style stay the same in every picture.
- Paste the **style block** first, then the scene.
- Save each picture with the file name given, into `Assets/Art/UI/Story/Cutscenes/`, then rebuild
  (Weather For Sale > Build Front End Scenes). The game picks them up by name; the subtitle is already in
  the game, so **no text in the pictures**.
- 16:9, 1920 × 1080. Leave the bottom fifth of the picture calm (the subtitle box covers it).

## Style block (paste before every CGI scene)

> Cozy fantasy anime-cartoon game CGI illustration, the same style as the attached cover: bright
> saturated colours, soft painterly shading, warm light, clean outlines, expressive faces, a European
> storybook town with blue roofs and towers. Characters exactly as in the attached sheets:
> SAM, a cheerful boy with messy brown hair, aviator goggles on his head, red hooded jacket, navy scarf
> with a gold star, brown leather satchel, cream shorts with red stripes, striped socks, brown boots;
> UNCLE JOHN, a stout kind man with a big moustache, straw hat with a blue band, blue plaid shirt rolled
> to the elbows, brown suspenders with a round weather-dial medallion, brown trousers, a towel on his belt;
> TEMPEST, a huge purple storm-cloud monster with one big glowing yellow eye, red lightning cracks, gold
> star bracers and golden lightning claws. 16:9 wide shot, no text, no letters, no speech bubbles,
> the bottom fifth of the image kept simple.

## Opening (`Opening_1.png` … `Opening_6.png`)

| File | Scene | Subtitle in game |
|---|---|---|
| Opening_1 | Sam's cozy attic bedroom at sunrise, Sam sitting up in bed with a big happy stretch, golden light through a round window, weather jars on a shelf | A sunny morning. Sam wakes up with a big, happy stretch. |
| Opening_2 | Sam in front of a mirror pulling on the red jacket, goggles going on his head, satchel on the bed | Red jacket... lucky goggles... ready! |
| Opening_3 | Sam running down a sunny cobblestone street of the town toward the weather shop with its blue-and-white awning, townsfolk waving | Off to Uncle John's weather shop, just like every day. |
| Opening_4 | Inside the empty shop: Sam at the doorway looking around, puzzled, the counter, shelves of glowing weather bottles, no one there | Uncle John? ...Hello? Nobody's here. |
| Opening_5 | Close-up of a handwritten note pinned on the counter next to the crystal ball (blank lines, no readable words), Sam's hand picking it up | (Uncle John's note) |
| Opening_6 | Sam behind the counter tying on a shop apron, fist up, determined grin, sunlight streaming in | One whole day as shopkeeper. I can do this! |

## Normal Ending (`NormalEnding_1.png` … `NormalEnding_3.png`)

Plays after Uncle John's lines in the shop. Slides 4 and 5 are text cards (no picture).

| File | Scene | Subtitle |
|---|---|---|
| NormalEnding_1 | Sunset inside the shop: Uncle John ruffling Sam's hair at the counter, both smiling, four empty bottle stands | The shop is in good hands. Will you help me again tomorrow? |
| NormalEnding_2 | Sam beaming and nodding, holding the shop sign "Closed" side out (sign blank, no letters), warm orange evening light | Of course! Same time tomorrow! |
| NormalEnding_3 | Wide shot of the town at dusk under a heavy grey sky, a few dark clouds swirling high above the towers, a faint purple glint inside one cloud | But the sky over the town stays grey... as if something is still missing. |

## Secret path — before the boss (`TrueIntro_1.png` … `TrueIntro_6.png`)

Plays after Uncle John's lines in the shop, when all 4 Shards were found.

| File | Scene | Subtitle |
|---|---|---|
| TrueIntro_1 | Outside the shop: the sky over the town turns pitch black in an instant, a huge lightning bolt, Sam and Uncle John at the shop door looking up in shock | CRACK! The sky over the town turns black in an instant. |
| TrueIntro_2 | TEMPEST rising over the town square fountain, enormous, eye glaring, claws spread, wind ripping awnings and flags | WHO has been bottling MY weather?! |
| TrueIntro_3 | Chaos in the streets: rain, snow, a blazing sun beam and a tornado all at once, umbrellas flying, townsfolk running, roof tiles whirling | Rain, snow, heat and wind spin wildly through the streets. |
| TrueIntro_4 | Four glowing purple crystal Shards flying out of Sam's satchel up into the sky and merging in a burst of light into a glowing purple bottle with a cork | The four Shards rise from Sam's bag... and join in the sky into a glowing purple bottle! |
| TrueIntro_5 | The purple bottle floating down into Sam's hands, Uncle John beside him pointing at it urgently, storm behind | Sam! That bottle can seal the storm! |
| TrueIntro_6 | Sam running out of the shop toward Tempest with the purple bottle held high, scarf flying, determined, low heroic angle | Leave it to me. I'll catch this storm! |

## Secret path — after the boss (`TrueEnding_1.png` … `TrueEnding_5.png`)

Plays after "Boss Sealed". Slide 6 is the "Secret Ending" card.

| File | Scene | Subtitle |
|---|---|---|
| TrueEnding_1 | Sam holding up the bottle as the swirling purple storm spirals into it; the glass turns into a shimmering rainbow bottle (rainbow swirl inside, cork, gold rope and navy star tag) as the cork pops shut, rainbow sparks | Tempest swirls into the bottle, it turns to rainbow, and the cork pops shut. |
| TrueEnding_2 | Close-up of the rainbow bottle: a tiny grumpy Tempest inside among rainbow swirls, arms crossed, pouting, cute | Hmph... Fine. I'll stay in here. For now. |
| TrueEnding_3 | The clouds parting over the town, a huge rainbow, sunlight pouring onto wet rooftops, birds flying | The clouds part, and a rainbow stretches over the whole town. |
| TrueEnding_4 | Uncle John hugging Sam in the town square, townsfolk cheering around them, confetti | Sam... you saved the town! I'm so proud of you. |
| TrueEnding_5 | Sam back at the shop counter, flipping the sign to "Open" (sign blank), the rainbow bottle glowing on the top shelf, sunny | Weather for Sale - open for business! |

## Mode Select (two big cards, one prompt)

Save as `Assets/Art/UI/Story/ModeStory.png` and `ModeStage.png` (cut apart after) — the game writes the
words "Story Mode" / "Stage" under them.

> Two tall game menu cards side by side on a transparent background, same size, rounded corners,
> honey-wood frame with gold corners and small stars, no text. LEFT card: inside the frame, a painted
> scene of Sam standing proudly in front of the weather shop with Uncle John's note in his hand, warm
> morning light. RIGHT card: inside the frame, a painted collage of four places — a rainforest
> waterfall, a windy orange canyon, a golden desert with an arch, a snowy glacier — around a small
> purple question mark cloud in the middle. Cozy fantasy anime-cartoon style, bright colours. Each card
> 3:4, the pair 2048 × 1365.

## Level Select

Background (`Assets/Art/UI/Story/LevelSelectBg.png`):

> A wide painted background for a stage select screen: an old explorer's map spread on a wooden table
> in the weather shop, compass, brass weather tools, a cup of tea and four little weather bottles at the
> edges, soft warm lamp light, blurred and calm in the middle so cards can sit on top. Cozy fantasy
> anime-cartoon style, no text. 16:9, 1920 × 1080.

Secret slot frame (`Assets/Art/UI/Story/SecretSlot.png`):

> A round game medallion frame, dark navy stone with a gold rim, small purple lightning gems around the
> edge, an empty dark centre for a picture, glowing faint purple. Transparent background, square,
> 1024 × 1024, no text.

## The 4 map pictures (new, full scenes — not cut-outs of the 3D models)

Used on the map board, the shop's map panel and Level Select cards. Save as
`Assets/Art/UI/Maps/Map_Rainforest.png`, `Map_Valley.png`, `Map_Desert.png`, `Map_Glacier.png`.
Paste the style block first (without the characters), then:

> **Rainforest** — A lush misty rainforest at the foot of mossy grey cliffs, a tall multi-tier waterfall
> into a turquoise river, giant red-capped mushrooms and floating lily pads as stepping stones, vines
> hanging from huge trees, light rain sparkling in sunbeams. Wide 16:10 landscape painting, no text,
> no people.

> **Valley** — A deep orange sandstone canyon with flat-topped rock pillars and small trees on top,
> white wind streams curling between the pillars, a rope bridge swinging across, a sea of soft clouds
> far below, clear blue sky. Wide 16:10 landscape painting, no text, no people.

> **Desert** — A golden sand-dune desert with a big red-orange rock arch, flowering pink cacti,
> tumbleweeds rolling, heat shimmer, a few floating sandstone slabs over a quicksand pool, bright sun.
> Wide 16:10 landscape painting, no text, no people.

> **Glacier** — Snowy mountains with sharp peaks, a frozen icy slope with slippery blue ice sheets,
> snow-covered pine trees, rolling snowballs, mist in the valley, soft morning sun on fresh snow.
> Wide 16:10 landscape painting, no text, no people.
