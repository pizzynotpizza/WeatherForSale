# ChatGPT image prompts — UI v2 (high resolution)

Why: the current UI pieces were cut from one 1254 × 1254 sheet, so each piece is only 50–100 px
(StarBig 90 × 85, RoundWood 100 × 87) and gets stretched 2–3× on screen. That is what looks broken.
Make **one piece per image**, large, on a transparent background. Do not make a sheet.

Paste the **style block** first in every prompt, then the piece.

## Style block (paste before every piece)

> Cozy fantasy weather-shop game UI, polished mobile game style, thick clean dark-brown outline,
> warm honey wood and soft cream parchment, gold trim with small rivets, navy blue and gold accents
> with tiny star motifs, soft top-left lighting, gentle inner highlight, no text, no letters,
> centered, single object, transparent background PNG, 2048 px on the long side, crisp edges.

## 1. Achievement book (replaces BookOpen)

> An open storybook seen straight from the front, lying flat, two blank cream pages with faint
> ruled lines, thick navy-blue leather cover peeking out around the pages, gold corner caps with a
> small star on each, a red ribbon bookmark hanging from the bottom-middle, a small gold moon-and-star
> clasp at the top of the spine. Pages must be empty and flat so text and icons can be placed on them.
> Wide landscape shape, about 2:1.

## 2. Title header frame (replaces RibbonBlue — the "Weather Collected!" / "Choose a map" header)

> A wide horizontal title plaque: a carved honey-wood board with a flat, smooth, empty centre panel
> in deep navy blue (plain, no gradient stripes), gold rope trim around the edge, a small gold star
> medallion on the left end and on the right end, short wooden scroll curls at both ends. The empty
> navy centre panel must be large and flat so white text can sit on it. Very wide, about 5:1.

## 3. Stars (replaces StarBig, StarMid, StarSmall and their empty versions)

Make two images:

> A chunky rounded five-point star, glossy gold with an orange-gold rim, a soft white highlight on the
> top-left point, a slight 3D bevel, thick dark-brown outline. Square image.

> The same chunky rounded five-point star shape but empty: flat dusty-brown inside with a slightly
> darker inner shadow, same thick dark-brown outline, no shine. Square image.

## 4. Buttons (replaces the blue-and-white striped BtnBlueA / BtnBlueB)

> A wide pill-shaped game button made of polished honey wood, a gold rim with two small rivets on each
> end, a soft light band across the top half, flat empty face for a text label. About 3:1.

> The same pill button, but the face is a solid leaf green (for the main action), same gold rim and
> rivets, flat empty face. About 3:1.

## 5. Round key button (replaces RoundWood / KeyCap)

> A round chunky game button seen from the front, warm orange-gold face that is perfectly centred in
> the image, a gold rim with four small rivets, a thin darker lip at the bottom for depth, flat empty
> face for one letter. Square image, the circle centred with even empty space on all sides.

## 6. Panel (replaces PanelLarge)

> A large rectangular game window panel: cream parchment inside with a subtle paper texture, a carved
> honey-wood frame about 6% of the width thick, gold corner caps with a rivet each, flat and empty
> inside. Landscape, about 16:10.

## 7. All icons — one prompt (replaces every Icon*.png, the lock and the trophy)

One image, so keep the grid loose: 5 columns × 4 rows, every icon the same size, big gaps between
them, nothing touching. Each icon then comes out about 300 px, six times the current 45–55 px.

> A game UI icon set for a cozy fantasy weather-shop game, arranged in a clean grid of 5 columns and
> 4 rows with wide even empty space between icons, every icon the same size and centred in its cell,
> nothing overlapping, transparent background. Polished mobile game style: chunky rounded shapes,
> thick clean dark-brown outline, soft top-left light with one small white highlight, warm cream and
> honey-gold main colour with navy blue and gold accents, no text, no letters, no numbers.
> Row 1: padlock (closed), padlock (open), trophy cup, gold star, empty star outline.
> Row 2: purple crystal shard, empty crystal shard outline, red heart, check mark, X close mark.
> Row 3: back arrow pointing left, home house, gear cog, pause (two bars), skip (double arrow right).
> Row 4: music note, speaker with sound waves, speaker muted with a small X, closed book, open book.
> Square image, 2048 × 2048.

After it comes back, send the image as is; I will cut the 20 icons apart and swap them in.

## 8. Controls and the last icons — one prompt (joystick, Jump, menu, stopwatch)

Replaces JoystickBase / JoystickFull, JumpBtn / JumpBtnPressed, IconMenu (top-left button) and the
Time icon on Stage Complete (SunSpinner). 3 columns × 2 rows, loose grid, like the icon sheet.

> A game UI sheet for a cozy fantasy weather-shop game, the same style as a glossy cartoon icon set:
> chunky rounded shapes, thick clean dark-brown outline, soft top-left light with one small white
> highlight, honey-gold and warm orange with navy blue accents. Arranged in a clean grid of 3 columns
> and 2 rows with wide even empty space between items, every item centred in its cell, nothing
> overlapping, transparent background, no text, no letters.
> Row 1: (1) a virtual joystick base seen from the front: a flat round navy-blue disc with a gold rim and
> four small gold arrow notches at top, bottom, left and right, slightly see-through in the middle;
> (2) the joystick knob on its own: a smaller glossy honey-gold round knob with a soft dome highlight;
> (3) a large round jump button: glossy orange face with a gold rim and a bold cream up-arrow in the centre.
> Row 2: (4) the same jump button pressed: face a little darker and flatter, arrow slightly smaller,
> shadow reduced; (5) a menu icon: three thick rounded cream horizontal bars stacked, for placing on
> a round orange button; (6) a stopwatch icon: round gold stopwatch with a navy face, a cream hand
> and a small button on top.
> Square image, 2048 × 2048.

## 9. Shop floor and backdrop (seen behind the counter in the Shop scene)

What is there now: behind the counter the ground is flat plain sand, with a few low-poly trees and
grey cliffs; it reads empty.

Floor texture (seamless, top-down):

> Seamless tileable top-down texture of a cozy village market square: warm sandy cobblestones of mixed
> sizes with soft grass tufts in the gaps and a few fallen leaves, stylized hand-painted cartoon game
> texture, bright and warm, even flat lighting, no shadows from objects, square, 2048 × 2048.

Backdrop (a flat picture placed far behind the trees):

> Wide panoramic background for a cozy fantasy weather shop, seen from inside the shop looking out:
> a sunny village square with small colorful cottages, rolling green hills, a few puffy clouds, distant
> blue mountains, warm late-morning light, stylized hand-painted cartoon game art, soft and slightly
> desaturated toward the horizon so the counter stays in focus. No people, no text. Very wide, 3:1.
