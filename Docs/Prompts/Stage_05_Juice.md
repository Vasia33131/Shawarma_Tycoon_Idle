# Этап 05 — Клиенты и «сочность»

## Цель
Экран «живой»:
- нарисованный ларёк с продавцом и вертелом;
- очередь клиентов, которые подходят к окну, говорят реплику в облачке, платят и уходят;
- монеты вылетают от станций в HUD, над полосой всплывает «+X»;
- кнопки нажимаются с отскоком, доступные покупки «дышат»;
- звучат звуки;
- шрифт Rubik.

Экономику ничего из этого не меняет. В Tick нет аллокаций.

## Контекст
- Уже готово:
  - `TickManager`/`ITickable`, `Wallet`, `StationController`/`StationModel`, `Loc`;
  - UI этапов 03–04 и сборщик `Editor/GameUiBuilder.cs` (меню Build Game UI), зона `KioskArea` (заглушка), `ResponsiveLayout`.
- Решения пользователя:
  - **свой мини-твинер** вместо DOTween;
  - звуки и персонажи — **Kenney CC0** (Interface Sounds, Casino Audio, Toon Characters);
  - шрифт — **Rubik (OFL)**.
- Разделы GDD:
  - §4: цикл запускается тапом по станции **или по клиенту**;
  - §10: яркий плоский 2D, тёплые цвета, вылетающие монеты, пульс кнопок, звук «дзынь», юмор в репликах клиентов («Мне без лука, братан»);
  - PLAN этап 05: число клиентов зависит от дохода (визуально).
- Ограничения:
  - Update только в `TickManager`;
  - в Tick без аллокаций: числа форматируются в `char[]` и выводятся через `TMP_Text.SetCharArray`, объекты берутся из пулов;
  - тексты через `Loc`.

## Файлы
- Ассеты (копирую только нужное):
  - `Audio/` — 10 `.ogg`;
  - `Sprites/Customers/` — 4 персонажа × (idle, walk0–7, cheer0, interact);
  - `Fonts/Rubik[wght].ttf` + `OFL.txt`;
  - `Assets/_Game/CREDITS.md`.
- Создать (Core/Tweening): `Ease.cs`, `TweenRunner.cs` (`ITickable`, пул структур; scale, позиция с дугой, альфа; колбэк через `ITweenListener` без аллокаций), `ITweenListener.cs`.
- Создать (Core/Audio): `SoundId.cs`, `AudioConfig.cs` (SO: звук → клипы, громкость, минимальный интервал), `AudioService.cs` (пул AudioSource, случайный питч, пауза по `GameEvents.GamePaused`).
- Создать (Economy): `NumberText.cs` — сборщик текста в `char[]` (`Append(string)`, `AppendNumber(double)`) по правилам `BigNumberFormatter`.
- Создать (Localization): `LocTemplate.cs` — шаблон «префикс{0}суффикс», разбирается один раз на язык.
- Создать (Customers): `CustomerConfig.cs` (SO: скины, скорость, частота кадров, ключи реплик), `CustomerDemand.cs` (чистый C#: размер очереди и время обслуживания по темпу дохода), `IncomeRateTracker.cs` (скользящий доход/сек по фактическому заработку), `CustomerView.cs`, `CustomerQueue.cs` (`ITickable`: пул, очередь, облачко, тап по клиенту).
- Создать (UI): `ButtonFeedback.cs` (отскок и звук), `PulseEffect.cs` + `PulseDriver.cs` (дыхание без Update), `CoinFlyEffect.cs` (пул монет → иконка HUD), `FloatingTextEffect.cs` (пул «+X»), `KioskView.cs` (вертел, продавец).
- Создать (Editor): `SpriteImportSettings.cs` (AssetPostprocessor: PNG из `Sprites/` → Sprite без мипмапов), `ContentSetup.cs` (шрифт Rubik → TMP-ассет и шрифт по умолчанию; создаёт `AudioConfig`/`CustomerConfig`, если их нет; ссылка на аудио в Boot).
- Изменить:
  - `StationModel` — событие `CycleCompleted`;
  - `StationController` — событие `Earned`;
  - `HudView`, `StationRowView`, `ManagerRowView`, `UpgradeRowView`, `BuyModeButton` — тексты без аллокаций, звуки, пульс;
  - `GameBootstrap` — `TweenRunner`, `PulseDriver`, `AudioService`;
  - `GameScreen`;
  - `GameUiBuilder` — нарисованный ларёк, слой эффектов, `ButtonFeedback` на кнопках, вызов `ContentSetup`;
  - `strings.txt` — реплики клиентов, табличка.

## Задачи (по порядку, каждая проверяемая)
1. [x] Скопировать ассеты, `CREDITS.md`, `SpriteImportSettings`.
2. [x] `NumberText` + `LocTemplate` + тесты (совпадение с `BigNumberFormatter`, без аллокаций при повторном вызове).
3. [x] `TweenRunner` + `Ease` + тесты (значения кривых, завершение, колбэк, замена твина того же свойства).
4. [x] `AudioConfig` + `AudioService`.
5. [x] `CycleCompleted`, `Earned`, `IncomeRateTracker`, `CustomerDemand` + тесты.
6. [x] `CustomerConfig`, `CustomerView`, `CustomerQueue`, `KioskView`.
7. [x] `ButtonFeedback`, `PulseEffect`/`PulseDriver`, `CoinFlyEffect`, `FloatingTextEffect`; перевод вью на `NumberText`.
8. [x] `ContentSetup` + обновление `GameUiBuilder`; строки ru/en.
9. [x] Обновить `PROGRESS.md`.

## Критерии приёмки
- [ ] Компилируется, консоль Unity без ошибок и warnings
- [ ] EditMode-тесты зелёные
- [ ] Клиенты подходят к окну, говорят реплику (ru/en), платят и уходят. Чем больше доход, тем длиннее очередь и быстрее обслуживание
- [ ] Тап по клиенту запускает цикл станций без менеджера (GDD §4)
- [ ] Монеты летят от станции в HUD, иконка HUD подпрыгивает, звучит «дзынь» (без каши звуков на быстрых станциях)
- [ ] Кнопки нажимаются с отскоком и звуком, доступные «Открыть / Нанять / Купить улучшение» и «Тапни!» пульсируют
- [ ] Шрифт Rubik, кириллица и цифры читаются
- [ ] 60 FPS в редакторе; в Profiler нет GC Alloc в кадре при обычной игре (кроме момента покупки и смены языка)
- [x] PROGRESS.md обновлён

## Ручная проверка в Unity (для меня)
1. Переключись в редактор и выбери `ShaurmaTycoon → Build Game UI` (подтвердить).
2. Play: смотри на очередь. Тапни клиента — станции стартуют.
3. Найми менеджеров на несколько станций — очередь растёт, монеты летят.
4. `Window → Analysis → Profiler` → CPU, колонка GC Alloc в кадре должна быть 0 B.
5. Проверь портрет и ландшафт, English.

## НЕ делать на этом этапе
- Сохранения (06), офлайн (07), рекламу и кнопку ×2 (09–10), настройки звука в UI (нет в GDD)
- Скины ларька (LiveOps)

## Риски / вопросы ко мне (решено без вопросов — скажи, если не так)
- **Тап по клиенту** запускает **все** станции без менеджера, которые сейчас простаивают («клиент заказал всё»). Альтернатива — только первую простаивающую.
- **Монеты в HUD:**
  - летят от строки станции, если она видна (вкладка «Станции»), иначе — от окна ларька;
  - на быстрых станциях не чаще ~4 раз в секунду на станцию, всего в полёте не больше 24 монет.
- **Звуки** подобраны по названиям файлов Kenney; заменить можно в `AudioConfig` без кода:
  - «дзынь» — `chips-collide-1..3`;
  - тап — `click_002`;
  - покупка — `confirmation_001`.
- Персонажи: клиенты — Male/Female person и Female adventurer, продавец — Male adventurer. Зомби и роботов не берём.
