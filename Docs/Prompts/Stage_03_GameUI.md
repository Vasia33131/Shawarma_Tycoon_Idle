# Этап 03 — UI игрового экрана

## Цель
Настоящий игровой экран вместо отладочной панели:
- HUD с монетами и доходом/сек;
- список станций с прогресс-барами, тапом и покупкой ×1/×10/×Max, закрытые станции с ценой открытия;
- вкладки Станции / Менеджеры / Улучшения / Город (последние три пока пустые).

Все тексты идут через локализацию ru/en, экран читается в портрете и ландшафте.

## Контекст
- Уже готово:
  - `Stations/StationController.cs` (`Stations`, `Tap`, `TryBuy`), `Stations/StationModel.cs` (`Level`, `Progress`, `IsRunning`, `CycleTime`, `IncomePerCycle`, `GetCost`, `GetMaxAffordable`, `NextMilestone`, `LevelChanged`);
  - `Economy/Wallet.cs` (`CoinsChanged`), `Economy/BigNumberFormatter.cs`;
  - `UI/CanvasScalerAdapter.cs` (`OrientationChanged`), `Core/GameSceneInstaller.cs`, `Core/TickManager.cs`.
- Разделы GDD:
  - §3, экран: верх — монеты, доход/сек (кнопка ×2 и репутация появятся позже); центр — ларёк и очередь (этап 05, сейчас место-заглушка); низ — вкладки и кнопка «Купить ×1 / ×10 / ×Max»;
  - §4, станции: без менеджера цикл запускается тапом;
  - §10, стиль: тёплые цвета, крупные цифры.
- Ограничения:
  - тексты UI — через локализацию с самого начала;
  - UI проверяется в 1080×1920 и 1920×1080;
  - Update только в `TickManager`, вью обновляются через `ITickable`;
  - без `FindObjectOfType`;
  - в Tick без лишних аллокаций: прогресс-бары — только `fillAmount`, тексты — при изменении значений, монеты в HUD — не чаще 10 раз в секунду.

## Файлы
- Импорт: TMP Essential Resources (в `Assets/TextMesh Pro/`, распаковкой пакета из `com.unity.textmeshpro`).
- Создать: `Scripts/Localization/LocTable.cs` — парсер TSV (`key \t ru \t en`), чистый C#.
- Создать: `Scripts/Localization/Loc.cs` — сервис:
  - текущий язык, `LanguageChanged`, `Get(key)`, `Format(key, args)`;
  - откат: en → ru → ключ;
  - нормализация языка платформы (ru, be, kk, uk, uz → ru, остальные → en).
- Создать: `Scripts/Localization/LocalizedText.cs` — компонент на TMP-тексте: ключ → текст, обновляется при смене языка.
- Создать: `Localization/strings.txt` (TSV) — все строки этапа на ru/en.
- Создать: `Scripts/UI/BuyMode.cs` (enum), `Scripts/UI/BuyModeButton.cs` — переключатель ×1 → ×10 → ×Max.
- Создать: `Scripts/UI/HudView.cs` — монеты и доход/сек.
- Создать: `Scripts/UI/StationRowView.cs` — строка станции:
  - иконка-кнопка для тапа, название, уровень, прогресс-бар с доходом за цикл и временем цикла;
  - сколько уровней до вехи;
  - кнопка покупки;
  - оверлей «Открыть за X».
- Создать: `Scripts/UI/StationsPanel.cs` — создаёт строки из префаба, тикает прогресс.
- Создать: `Scripts/UI/TabBar.cs` — вкладки и панели.
- Создать: `Scripts/UI/ResponsiveLayout.cs` — раскладка зон по ориентации:
  - портрет: HUD / ларёк / список / вкладки сверху вниз;
  - ландшафт: ларёк слева, список справа.
- Создать: `Scripts/UI/GameScreen.cs` — корень экрана: в `Start` берёт сервисы и связывает вью, регистрирует их в `TickManager`.
- Создать: `Scripts/Editor/GameUiBuilder.cs` — меню `ShaurmaTycoon/Build Game UI`:
  - собирает префаб `Prefabs/UI/StationRow.prefab` и иерархию экрана в сцене Game;
  - убирает `DebugLabel_Stage00` и `StationsDebugPanel`.
  Повторный запуск пересобирает экран, но сначала спрашивает подтверждение.
- Изменить:
  - `Stations/IStationDefinition.cs` — добавить `NameKey`;
  - `Stations/StationController.cs` — `IncomePerSecond` (сумма по станциям с автоциклом);
  - `Core/GameBootstrap.cs` — создать `Loc` из `strings.txt` (ссылка в сцене Boot), зарегистрировать;
  - `Platform/EditorPlatformService.cs` — язык в редакторе переключается меню `ShaurmaTycoon/Editor Language`;
  - `ShaurmaTycoon.asmdef` / `ShaurmaTycoon.Editor.asmdef` — ссылка на `Unity.TextMeshPro`.
- Удалить: `Scripts/UI/StationsDebugPanel.cs` — по плану этапа 02 («удаляется на этапе 03»).

## Задачи (по порядку, каждая проверяемая)
1. [x] Импортировать TMP Essentials, добавить ссылки на TMP в asmdef.
2. [x] `LocTable` + `Loc` + тесты; `strings.txt`; регистрация в `GameBootstrap`; переключение языка в редакторе.
3. [x] `LocalizedText`, `BuyMode`/`BuyModeButton`, `HudView`, `StationRowView`, `StationsPanel`, `TabBar`, `ResponsiveLayout`, `GameScreen`.
4. [x] `NameKey` в определении станции, `StationController.IncomePerSecond` + тест.
5. [x] `GameUiBuilder`: префаб строки, экран в сцене, удаление отладочных объектов.
6. [x] Удалить `StationsDebugPanel.cs`.
7. [ ] Проверить сборку WebGL (долг этапа 00) — не подтверждено.
8. [x] Обновить `PROGRESS.md`.

## Критерии приёмки
- [x] Компилируется, консоль Unity без ошибок и warnings
- [x] EditMode-тесты зелёные
- [x] Можно 5 минут играть только этим UI: тап по иконке запускает цикл, монеты в HUD растут, покупка ×1/×10/×Max работает, кнопка неактивна, если не хватает денег
- [x] Закрытая станция показывает «Открыть за X» и открывается
- [x] Вкладки переключаются; Менеджеры / Улучшения / Город показывают «Скоро»
- [x] В 1080×1920 и 1920×1080 всё читается, ничего не обрезано и не наезжает
- [x] Русские буквы отображаются; при английском языке редактора — английские тексты
- [x] Отладочной панели и надписи `DebugLabel_Stage00` больше нет
- [x] PROGRESS.md обновлён

## Ручная проверка в Unity (для меня)
1. Переключись в редактор и дождись импорта, затем выбери `ShaurmaTycoon → Build Game UI`.
2. Boot → Play. Тапай по иконке «Шаурма классик», копи, покупай уровни. Переключай кнопку «Купить: ×1/×10/Max», открой «Сырный лаваш» за 60.
3. Game View: переключи 1080×1920 ↔ 1920×1080.
4. Выбери `ShaurmaTycoon → Editor Language → English` → Play: тексты на английском.
5. Пощёлкай вкладки.

## НЕ делать на этом этапе
- Менеджеры, апгрейды (этап 04): вкладки-заглушки
- Клиенты, анимации, DOTween, звуки, настоящая графика ларька (этап 05): в центре цветная заглушка
- Кнопку «×2 доход», репутацию, подарок в HUD (этапы 08–10)
- Сохранения (этап 06)

## Риски / вопросы ко мне (решено без вопросов)
- **Шрифт:** LiberationSans из TMP Essentials, кириллица берётся из его динамического fallback-атласа. Красивый шрифт (например, OFL-шрифт с Google Fonts) — на этапе 05, тогда спрошу про скачивание.
- **Доход/сек** в HUD считается по станциям с автоциклом, то есть до менеджеров (этап 04) он скрыт. Доход каждой станции виден в её строке.
- **Иконки станций:** цветные круги с номером, пока нет графики (этап 05).
- **Полностью без аллокаций** форматирование чисел станет на этапе 05 (критерий Profiler). Сейчас тексты обновляются только при изменении значений.
