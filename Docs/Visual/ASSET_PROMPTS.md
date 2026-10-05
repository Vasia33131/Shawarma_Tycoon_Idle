# Промты для генерации ассетов (ChatGPT / GPT-image)

## Как генерировать, чтобы стиль не «поплыл»

1. Заведи **отдельный чат на каждый пакет** (A, B, C…).
2. Первым сообщением приложи `Assets/_Game/Sprites/Shop/starter_pack.png` и `income_x2.png` и напиши:
   > Это референс стиля моей игры. Запомни его. Дальше я буду просить отдельные ассеты — каждый строго в этом стиле, по одному на картинку. Отвечай только картинкой.
3. Каждое следующее сообщение = **СТИЛЬ пакета + строка из таблицы**.
4. Если картинка «выпала» из стиля, не правь её словами — сгенерируй заново в том же чате.
5. Сохрани PNG **с точным именем из таблицы** в указанную папку.

Размеры: квадрат — 1024×1024, широкая — 1536×1024 (в ChatGPT пиши «landscape 3:2»). Unity сам уменьшит.

### Базовый СТИЛЬ (вставлять в начало каждого промта)
```
Casual mobile game asset, cozy cartoon style matching the attached reference exactly:
soft rounded shapes, thick dark-brown outline (#4A2C1D, never black), gentle cel shading
with soft highlights, warm evening light from the upper left, slightly glossy, rich but
not neon colors. Single object, centered, about 8% empty padding on every side, fully
visible, nothing cropped. No text, no letters, no numbers, no logos, no watermark.
Transparent background, PNG.
```

---

## Пакет A — UI-иконки (перед промтом V2)
Папка: `Assets/_Game/Sprites/UI/Icons/` · квадрат 1024×1024.
Добавка к СТИЛЮ: `Game UI icon, must stay readable at 64 px: bold simple silhouette, few details, front 3/4 view.`

| Файл | Промт (после СТИЛЯ + добавки) |
|---|---|
| `icon_coin.png` | A shiny gold coin seen slightly from the side, a simple embossed shawarma roll emblem in the center. |
| `icon_bill.png` | A small stack of three green banknotes with a paper band around them, slightly fanned. |
| `icon_reputation.png` | A lavender-purple star medal with a small cream ribbon, soft glow. |
| `icon_income.png` | A gold coin with a small round green arrow pointing up-right behind it. |
| `tab_stations.png` | A small street grill with a vertical doner meat spit, warm orange glow. |
| `tab_managers.png` | A white chef hat with a small orange name badge pinned on it. |
| `tab_upgrades.png` | A thick rounded green arrow pointing up with two small sparkles. |
| `tab_city.png` | A folded paper map with a red location pin on top. |
| `tab_shop.png` | A kraft-paper shopping bag with rope handles and a small gold coin peeking out. |
| `icon_settings.png` | A rounded wooden gear with a light cream center. |
| `icon_gift.png` | A small gift box with a red ribbon and a bow, lid slightly lifted, soft golden light inside. |
| `icon_calendar.png` | A tear-off desk calendar with rings on top and one star sticker on the page. |
| `icon_quests.png` | A wooden clipboard with a paper list and three green check marks. |
| `icon_ad.png` | A small retro TV with a white play triangle on a soft green screen. |
| `icon_boost.png` | A yellow lightning bolt over a gold coin, sparkles around. |
| `icon_lock.png` | A rounded brass padlock, closed. |
| `icon_offline.png` | A sleeping crescent moon in a nightcap resting on a small pile of gold coins. |
| `icon_check.png` | A big rounded green check mark with a soft highlight. |
| `icon_close.png` | A rounded coral-red circle button with a cream X shape. (X — это форма, не буква) |
| `tutorial_hand.png` | A cartoon hand with a white glove pointing down with the index finger, other fingers curled. |
| `icon_zen.png` | A cozy steaming cup of tea on a small saucer, three soft curls of steam. (кнопка «режим любования», нужна к V7) |

---

## Пакет B — иконки станций (перед промтом V2)
Папка: `Assets/_Game/Sprites/Stations/City0X/`, **имя = имя ассета станции** · квадрат 1024×1024.
Добавка к СТИЛЮ: `Food icon for a street food tycoon game, appetizing, served on or in simple packaging, 3/4 view, readable at 96 px.`

| Файл | Промт |
|---|---|
| **City01/** | |
| `Station_01_ShawarmaClassic.png` | A classic shawarma wrap in lavash, half-wrapped in paper, meat, tomato and cabbage visible. |
| `Station_02_CheeseLavash.png` | A golden toasted lavash roll cut in half with stretchy melted cheese pulling out. |
| `Station_03_Fries.png` | French fries in a red-and-white paper cup, a few fries sticking out. |
| `Station_04_Drinks.png` | A tall paper cup of white ayran with a striped straw next to a small glass bottle of lemonade. |
| `Station_05_ShawarmaXL.png` | A huge overstuffed shawarma wrap, twice the normal size, with a small wooden skewer flag on top. |
| `Station_06_GrillPlate.png` | A plate with grilled meat pieces, rice, grilled tomato and a sprig of greens. |
| **City02/** | |
| `Station_01_KazanShawarma.png` | A shawarma wrap in paper with a small green Tatar ornament printed on the paper. |
| `Station_02_EchpochmakWrap.png` | A golden triangular Tatar pastry echpochmak, one corner open showing meat and potato. |
| `Station_03_ChakChak.png` | A small mound of chak-chak: honey-glazed fried dough pieces, shiny with honey. |
| `Station_04_KatykTea.png` | A glass of black tea in a metal glass holder next to a small bowl of white katyk yogurt. |
| `Station_05_KazanXL.png` | A huge shawarma wrap served in a small black cast-iron kazan pot. |
| `Station_06_PlovPlate.png` | A plate of golden plov with carrots, a whole garlic head on top. |
| **City03/** | |
| `Station_01_BeachShawarma.png` | A shawarma wrap in paper with a tiny paper cocktail umbrella stuck in it. |
| `Station_02_CornCob.png` | A boiled yellow corn cob with a pinch of salt on a small paper napkin. |
| `Station_03_Churchkhela.png` | Two hanging churchkhela sticks, one purple and one amber, on a string. |
| `Station_04_Lemonade.png` | A tall glass jar of homemade lemonade with lemon slices, mint and ice, a striped straw. |
| `Station_05_ResortXL.png` | A huge shawarma wrap wearing tiny sunglasses, a small palm leaf behind it. |
| `Station_06_ShashlikPlate.png` | Two metal skewers of charcoal-grilled shashlik with onion rings on a wooden board. |
| **City04/** | |
| `Station_01_PiterShaverma.png` | A shaverma wrap in paper, a tiny drop of rain on the paper, cool bluish rim light. |
| `Station_02_Pyshki.png` | Three round ring doughnuts heavily dusted with powdered sugar on a paper plate. |
| `Station_03_KoryushkaWrap.png` | A lavash wrap with small crispy fried smelt fish tails sticking out. |
| `Station_04_BreadKvass.png` | A frosty mug of dark kvass with foam next to a slice of rye bread. |
| `Station_05_BridgesXL.png` | A huge shaverma wrap shaped like a raised drawbridge arch. |
| `Station_06_BliniPlate.png` | A stack of thin golden blini with a spoon of red caviar and sour cream on top. |
| **City05/** | |
| `Station_01_CapitalShawarma.png` | A neat shawarma wrap in dark premium paper with a gold sticker (no text on the sticker). |
| `Station_02_Pelmeni.png` | A bowl of pelmeni dumplings with a spoon of sour cream and dill. |
| `Station_03_Sbiten.png` | A steaming clay mug of hot sbiten with a cinnamon stick and a slice of orange. |
| `Station_04_KebabBox.png` | An open takeaway box with kebab meat, fries and sauce. |
| `Station_05_CityXL.png` | A huge shawarma wrap standing upright like a skyscraper, with tiny lit windows drawn on the paper. |
| `Station_06_BusinessLunch.png` | A compartment lunch tray: soup bowl, salad, cutlet with mashed potato, a cup of compote. |

---

## Пакет C — фоны городов (перед промтом V3)
Небо рисует код (градиент), поэтому **все слои прозрачные сверху**.
Папки: `Assets/_Game/Sprites/Scene/City0X/` и `Assets/_Game/Sprites/Scene/Common/` · широкая 1536×1024.

Добавка к СТИЛЮ для слоёв (вместо «Single object, centered…»):
```
Wide side-view background layer for a 2D game, flat orthographic side view, no perspective
ground. Transparent sky (everything above the buildings is fully transparent). Cozy evening:
warm lights in windows, soft purple shadows. Calm, low detail, no people, no cars.
Lower ~15% of the image is a simple flat strip that will be covered by the sidewalk.
```

| Файл | Промт |
|---|---|
| `City01/bg_far.png` | Distant skyline of a cozy Russian hometown: five-story panel apartment blocks, a water tower, TV antenna on roofs, a few birch tree crowns; muted lilac-blue silhouettes with warm yellow lit windows. |
| `City01/bg_near.png` | Near street layer, ONLY at the left and right edges (center 50% empty and transparent): left — a birch tree and a green wooden bench; right — a street lamp with a warm glowing bulb and a small newspaper kiosk. |
| `City02/bg_far.png` | Distant skyline of Kazan: the Kazan Kremlin white walls, the Kul Sharif mosque with turquoise domes and minarets, the leaning Söyembikä tower; lilac silhouettes, warm lit windows. |
| `City02/bg_near.png` | Near street layer, ONLY at the left and right edges (center 50% empty): left — a linden tree and an ornate street lamp; right — a low brick wall with a flower box in Tatar ornament colors. |
| `City03/bg_far.png` | Distant view of Sochi: the sea horizon with the sun half-set into the water, Caucasus mountains on the left, a pier and a small lighthouse on the right; warm orange-pink tones. |
| `City03/bg_near.png` | Near street layer, ONLY at the left and right edges (center 50% empty): left — a tall palm tree; right — a white seaside balustrade and a striped beach umbrella. |
| `City04/bg_far.png` | Distant view of Saint Petersburg: the Neva river, a raised drawbridge, the golden dome of St. Isaac's Cathedral, the Peter and Paul Fortress spire; pale lilac-blue white-nights tones, lit windows. |
| `City04/bg_near.png` | Near street layer, ONLY at the left and right edges (center 50% empty): left — a cast-iron embankment railing and an old lamp post; right — a classic yellow building corner with an arched lit window. |
| `City05/bg_far.png` | Distant night skyline of Moscow: a Stalinist high-rise, Kremlin tower with a red star, modern glass skyscrapers far behind with many lit windows; deep blue and violet tones. |
| `City05/bg_near.png` | Near street layer, ONLY at the left and right edges (center 50% empty): left — a modern street lamp and a neat maple tree; right — a metro entrance sign shape (a plain red letter-free arch) and a flower bed. |
| `Common/cloud_01.png` | (обычный СТИЛЬ, квадрат) A soft fluffy evening cloud, lit pink-orange from below, lavender on top. |
| `Common/cloud_02.png` | (обычный СТИЛЬ, квадрат) A long thin stretched evening cloud, peach and lavender. |
| `Common/cloud_03.png` | (обычный СТИЛЬ, квадрат) A small round puffy cloud, peach-pink. |
| `Common/moon.png` | (обычный СТИЛЬ, квадрат) A soft glowing crescent moon, pale cream-yellow, gentle halo. |

---

## Пакет D — ларёк и украшения (перед промтом V3)
Папка: `Assets/_Game/Sprites/Kiosk/` · квадрат 1024×1024, обычный СТИЛЬ.
**Важно:** части, которые перекрашиваются в цвет города, рисуем **светло-серыми** — код их тонирует.

| Файл | Промт |
|---|---|
| `kiosk_body.png` | Front view of a small street food kiosk booth WITHOUT awning and WITHOUT counter: walls painted in plain very light neutral grey (#E8E8E8), a large rectangular serving window in the upper middle with a dark warm-brown interior, small wooden trim, short legs. Flat front view, symmetric. |
| `kiosk_counter.png` | A wide wooden serving counter plank seen from the front, with a few small details: a napkin holder and a sauce bottle on the right. Flat front view, very wide and short. |
| `kiosk_awning.png` | A scalloped striped market awning seen from the front, stripes alternating white and light grey (#D0D0D0), slight fabric folds. Flat front view, wide and short. |
| `kiosk_sign.png` | A blank rounded wooden signboard with a thick frame, empty center for text. Wide and short. |
| `kiosk_spit.png` | A vertical doner kebab meat spit on a metal rod, golden-brown roasted meat cone, a few juicy highlights. Tall and narrow. |
| `decor_garland.png` | A string of warm glowing round light bulbs hanging in a gentle curve. Wide and short. |
| `decor_menu_board.png` | A small A-frame chalkboard menu stand with simple chalk doodles of a shawarma and a cup (no text). |
| `decor_table_umbrella.png` | A small round cafe table with two stools under a striped patio umbrella, stripes white and light grey. |
| `decor_sign_lights.png` | A rectangular frame made of small glowing marquee light bulbs, empty transparent center. |
| `decor_flowers.png` | Two terracotta pots with blooming geraniums and greenery, side by side. |
| `decor_flags.png` | A string of small triangular bunting flags in white and light grey, hanging in a curve. |

---

## Пакет E — персонажи и кот (перед промтом V4) — ЛИСТАМИ

### Почему листами
Если генерировать каждого персонажа отдельно, нейросеть каждый раз заново «придумывает» стиль: разные пропорции, толщина линий, свет, а кот уходит в реализм. **Внутри одной картинки стиль всегда общий.** Поэтому персонажи рисуются листами по несколько штук, а на отдельные файлы их режет нейросеть-разработчик (кладёшь листы в `Assets/` с любыми именами — она опознает и нарежет).

Всего 6 листов, все **landscape 3:2 (1536×1024)**:

| Лист | Что на нём | Сетка |
|---|---|---|
| 1. `customers_a` | 5 клиентов: студент, бабушка, офисный, курьер, турист | 5 в ряд |
| 2. `customers_b` | 5 клиентов: строитель, девушка в наушниках, рыбак, хипстер, школьник | 5 в ряд |
| 3. `customers_happy_a` / `customers_happy_b` | те же 5 + 5, радуются (тот же порядок!) | 5 в ряд |
| 4. `seller` | продавец: обычный, работает, «класс» | 3 в ряд |
| 5. `cat` | кот: спит, сидит, мурчит, потягивается + коробка-лежанка | 5 в ряд |

### Как генерировать (важно)
1. **Новый чат.** Первым сообщением приложи `Sprites/Shop/starter_pack.png` и 2–3 иконки станций, которые тебе нравятся (например `Sprites/Stations/City01/Station_01_ShawarmaClassic.png`). Напиши: *«Это референс стиля моей игры. Запомни его. Дальше рисуй строго так же».*
2. Сначала **лист 1**. Если он нравится — приложи его же к листу 2 со словами *«same style, same proportions, same line thickness as the attached sheet»*.
3. Для «радостных» листов прикладывай соответствующий обычный лист: *«the same five characters in the same order and same positions, now happy»*.
4. Кота и продавца — тоже с приложенным листом 1: тогда и кот, и продавец будут из того же мира.
5. Если ChatGPT нарисовал «шахматку» вместо прозрачности — не страшно, её уберут при нарезке.

### СТИЛЬ ПЕРСОНАЖЕЙ (вставлять в начало каждого листа)
```
Character sheet for a cozy casual mobile game, matching the attached reference exactly.
Cartoon chibi style: big round head, small body, about 2.5 heads tall, short stubby limbs,
simple dot-like eyes with a small highlight, small rosy cheeks, minimal facial details.
Thick dark-brown outline (#4A2C1D, never black), flat cel shading with only 2-3 tones per
color, soft highlights, warm evening light from the upper left. Clean simple shapes,
no texture, no realistic details, no fur strands, no realistic anatomy, no photorealism,
no painterly rendering. All characters share EXACTLY the same proportions, head size,
eye style and line thickness. Each character stands alone in its own equal-width column,
full body, 3/4 view facing the viewer's LEFT, feet on the same baseline, clear empty
space between characters, nothing overlapping. Plain transparent background, PNG.
No text, no letters, no numbers, no logos, no ground shadow.
```

### Лист 1 — `customers_a` (5 в ряд, слева направо)
```
[СТИЛЬ ПЕРСОНАЖЕЙ]
Five characters in a row, left to right:
1) young student with a big backpack, hoodie and sneakers;
2) kind grandma in a floral headscarf and a cardigan, holding a string shopping bag with a loaf;
3) office worker in a shirt with a loose tie and a lanyard badge, holding a coffee cup;
4) delivery courier with a big plain yellow thermal backpack and a cap, no logos;
5) tourist in a bucket hat and a Hawaiian shirt with a camera on the neck.
```

### Лист 2 — `customers_b`
```
[СТИЛЬ ПЕРСОНАЖЕЙ]
Same style, same proportions, same line thickness as the attached sheet.
Five characters in a row, left to right:
1) construction worker in an orange safety vest and a yellow hard hat, with a mustache;
2) girl with big headphones, an oversized sweater, holding a phone;
3) old fisherman grandpa in a padded jacket and an ushanka hat, holding a fishing rod;
4) bearded hipster in a beanie and round glasses, with a tote bag;
5) schoolkid with a backpack, standing next to a small kick scooter.
```

### Листы 3 — `customers_happy_a`, `customers_happy_b` (приложить лист 1 / лист 2)
```
[СТИЛЬ ПЕРСОНАЖЕЙ]
The same five characters as in the attached sheet, in the same order, same positions,
same size and same 3/4 view, but now happy: eyes closed in happy arcs, big open smile,
arms slightly raised in joy. Do not change their clothes, colors or proportions.
```

### Лист 4 — `seller` (приложить лист 1)
```
[СТИЛЬ ПЕРСОНАЖЕЙ]
Same style as the attached sheet. Three versions of the same shawarma master in a row:
a friendly chubby man with a big mustache, white chef hat and apron, shown from the waist up
(cut at the waist), facing the viewer.
1) standing relaxed, warm smile;
2) wrapping a shawarma with both hands, focused smile;
3) giving a thumbs up and winking.
Identical face, hat, apron and size in all three.
```

### Лист 5 — `cat` (приложить лист 1)
```
[СТИЛЬ ПЕРСОНАЖЕЙ]
Same cartoon style as the attached sheet — a simple chibi CARTOON cat, NOT a realistic cat:
round blob-like body, big round head, tiny triangle ears, dot eyes, no whisker detail,
no fur texture, flat orange color with a few simple darker orange stripe shapes,
white chest and white paws, thick dark-brown outline. Like a cute sticker or emoji.
Five items in a row, left to right, all the same cat at the same size:
1) curled up asleep in a round ball, tail around, eyes closed;
2) sitting upright, looking at the viewer, tail around paws;
3) sitting with eyes closed in happy arcs, small blush, content smile;
4) big stretch, front paws forward, back up, eyes closed, side view facing left;
5) (no cat) an old cardboard box with a soft checkered blanket inside, front view.
```

### Во что режется (делает нейросеть-разработчик)
| Лист | Файлы |
|---|---|
| `customers_a` + `customers_happy_a` | `Characters/Customers/student`, `babushka`, `office`, `courier`, `tourist` (+ `_happy`) |
| `customers_b` + `customers_happy_b` | `builder`, `girl_headphones`, `fisherman`, `hipster`, `schoolkid` (+ `_happy`) |
| `seller` | `Characters/Seller/seller_idle`, `seller_work`, `seller_happy` |
| `cat` | `Characters/Cat/cat_sleep`, `cat_sit`, `cat_purr`, `cat_stretch`, `cat_bed` |

**Если лист «поплыл»** (кто-то выше/ниже, другой стиль) — не правь словами, сгенерируй лист заново. Проверка: прищурься — все персонажи должны выглядеть как из одного мультфильма.

---

## Пакет F — живой мир (перед промтом V5)
Папка: `Assets/_Game/Sprites/Ambient/` · квадрат 1024×1024, обычный СТИЛЬ. Транспорт — **строго сбоку, смотрит влево, без логотипов и номеров**.

| Файл | Промт |
|---|---|
| `car_01.png` | A small boxy retro Soviet-style sedan, pale blue, side view facing left, headlights on. |
| `car_02.png` | A small round city hatchback, cherry red, side view facing left, headlights on. |
| `car_03.png` | A yellow taxi minivan with a blank roof sign, side view facing left. |
| `trolleybus.png` | A cute short city trolleybus with roof poles, cream and blue, warm lit windows, side view facing left. |
| `scooter_courier.png` | A delivery courier on a small electric scooter with a yellow thermal box, side view facing left. |
| `umbrella.png` | An open umbrella seen from the side, held up by an invisible hand (only the handle), canopy plain white so it can be tinted. |
| `bird.png` | A small round sparrow sitting, side view facing left. |

Пар, капли дождя, снежинки, светлячки, лужи и звёзды **не генерировать**: их рисует код.

---

## Пакет I — карта филиалов и мешки монет (перед промтом V5b)
Обычный СТИЛЬ. Сначала приложи референс стиля (как в «Как генерировать»), для карты — ещё `Sprites/Scene/City01/bg_far.png`, чтобы цвета совпали с вечерней сценой.

### Иконки магазина → `Assets/_Game/Sprites/Shop/` · квадрат 1024×1024
Строго как `bills_small/medium/large.png`: **непрозрачная плитка со скруглёнными углами, тёплый оранжевый фон с мягкими лучами, предмет в центре**. Код уже ждёт эти файлы: `Art/Assign Icons` поставит их вместо временной монетки.
Добавка к СТИЛЮ: `Shop item tile icon matching the attached bills icons exactly: rounded-square tile, warm orange background with soft light rays, the item centered and large, readable at 96 px. Not transparent inside the tile.`

| Файл | Промт |
|---|---|
| `coins_small.png` | A small brown leather coin pouch tied with a string, a few gold coins spilling out in front. |
| `coins_medium.png` | A big burlap sack full of gold coins, open at the top, coins piled high and a few rolling out. |
| `coins_large.png` | An open wooden treasure chest with brass corners overflowing with gold coins, soft golden glow from inside. |

### Карта → `Assets/_Game/Sprites/Map/`
| Файл | Размер | Промт |
|---|---|---|
| `map_bg.png` | landscape 3:2 (1536×1024) | (вместо «Single object, centered…») `Top-down cozy illustrated travel map for a casual mobile game, like a hand-painted board game map. Soft green meadows, small rounded hills, patches of tiny round trees, a gentle winding blue river from the top left to the bottom right, a small lake in the upper middle, a strip of sea with a beach at the bottom middle (south), soft snowy forest in the upper right (north). Warm evening light, muted pastel colors, thick soft dark-brown outline on shapes. Five flat EMPTY clearings for cities, evenly spread: left-bottom, left-top, center-bottom near the sea, right-top near the river, right-bottom. NO roads, NO cities, NO buildings, NO text, NO labels, NO compass, NO frame. Not transparent.` |
| `map_city_01.png` | квадрат | A tiny cozy five-story panel apartment block with a water tower and a birch tree, on a small round grassy base, isometric miniature. |
| `map_city_02.png` | квадрат | A tiny Kazan Kremlin miniature: white wall piece and the Kul Sharif mosque with turquoise domes, on a small round grassy base, isometric miniature. |
| `map_city_03.png` | квадрат | A tiny Sochi miniature: a palm tree, a small white lighthouse and a bit of sea, on a small round sandy base, isometric miniature. |
| `map_city_04.png` | квадрат | A tiny Saint Petersburg miniature: a raised drawbridge over a bit of river and a golden spire, on a small round base, isometric miniature. |
| `map_city_05.png` | квадрат | A tiny Moscow miniature: a Kremlin tower with a red star and a Stalinist high-rise, on a small round base, isometric miniature. |
| `map_van.png` | квадрат | A cute little shawarma food truck van, orange with a striped awning folded on the side and a small doner spit sign on the roof (no text), side view facing RIGHT. |
| `map_pin.png` | квадрат | A big rounded map location pin, warm coral red with a cream circle in the middle, soft highlight. |

**Проверка:** миниатюры городов — одного размера и с одинаковым «пятачком» снизу; если одна вышла крупнее или в другом ракурсе — перегенерируй её с приложенной удачной.

---

## Пакет G — звуки (перед промтом V6)
Нейросеть-разработчик звуки не генерирует. Источники с коммерческой лицензией:
- **Pixabay Music / Sound Effects** — коммерческое использование без указания автора.
- **Freesound.org** — только с фильтром «Creative Commons 0».
- **Kenney.nl** — CC0.
- **Suno / Udio** — только на платном тарифе: на бесплатном нет коммерческих прав, а игра монетизируется.

Формат: `.ogg` (или `.wav`), 44.1 кГц; эффекты — моно; петли — бесшовные (проверь стык в Audacity). Источник и лицензию каждого файла запиши в `Assets/_Game/Audio/LICENSES.md`.

| Файл | Что искать | Длина |
|---|---|---|
| `Audio/Music/music_cozy_01.ogg` | Lo-fi / chillhop, тёплое электропиано, 70–85 BPM, без вокала (город 1–2) | 1.5–3 мин, петля |
| `Audio/Music/music_cozy_02.ogg` | Акустическая гитара + мягкие ударные, «летний вечер» (Сочи) | 1.5–3 мин, петля |
| `Audio/Music/music_cozy_03.ogg` | Лёгкий джаз / боса-нова, ночной город (Питер, Москва) | 1.5–3 мин, петля |
| `Audio/Ambient/amb_street.ogg` | Тихая вечерняя улица: далёкие машины, голоса неразборчиво, птицы | 40–90 с, петля |
| `Audio/Ambient/amb_sea.ogg` | Мягкий прибой (Сочи) | 30–60 с, петля |
| `Audio/Ambient/amb_rain.ogg` | Спокойный дождь по навесу | 30–60 с, петля |
| `Audio/Ambient/amb_wind_soft.ogg` | Тихий зимний ветер (для снега) | 30–60 с, петля |
| `Audio/Ambient/amb_sizzle.ogg` | Тихое шипение мяса на гриле | 10–20 с, петля |
| `Audio/Sfx/cat_purr.ogg` | Мурчание кота | 2–4 с |
| `Audio/Sfx/cat_meow.ogg` | Короткое мягкое «мяу» | < 1 с |
| `Audio/Sfx/car_pass.ogg` | Машина проезжает мимо, тихо | 2–3 с |
| `Audio/Sfx/coin_soft.ogg` | Мягкий звон монетки (мягче фишек казино) | < 0.5 с |
| `Audio/Sfx/pop_soft.ogg` | Мягкий «поп» для появления попапа | < 0.3 с |

---

## Пакет H — витрина и загрузка (перед промтом V7, по желанию)
Иконка и обложка в каталоге Яндекс Игр решают, нажмёт ли человек «Играть» вообще. Делай их после V3–V4, когда игра уже выглядит по-новому.
Папка: `Assets/_Game/Sprites/Promo/` (в билд не попадает, кроме `loading_bg.png`).

| Файл | Размер | Промт |
|---|---|---|
| `loading_bg.png` | 1024×1536 (портрет) | Cozy evening street scene: a glowing shawarma kiosk with a striped awning and string lights, a ginger cat sleeping in a box next to it, warm sunset sky, soft and calm. Lower third is a simple blurred street (a loading bar will be placed there). No text. Not transparent. |
| `yandex_icon.png` | 1024×1024 → уменьши до 512×512 | App icon: a happy shawarma master with a big mustache holding a giant shawarma, ginger cat peeking from below, bright warm background with sunset gradient, rounded square composition. No text. Not transparent. |
| `yandex_cover.png` | 1536×1024 → обрежь до 800×470 | Wide cover: a cozy shawarma kiosk at sunset with a happy queue of cartoon customers (student, grandma, courier), cat on a box, string lights, city silhouette behind. Leave the left third calmer for a title overlay. No text. Not transparent. |

Название игры на обложку добавь сам в любом редакторе, шрифтом Rubik Bold: нейросеть пишет кириллицу с ошибками.
