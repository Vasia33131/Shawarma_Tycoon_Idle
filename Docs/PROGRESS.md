# PROGRESS

## Текущий этап: 09 — Яндекс Игры (код готов: платформа, стартовый набор, «Без рекламы», «×2»; ждёт Build Game UI, WebGL-билда и проверки на черновике)

## Журнал
### Этап 00 — Каркас (готов, 2026-10-04)
- Проверено: Setup Project выполнен (сцены Boot/Game, Build Settings, Gamma, Brotli + Fallback), Play из Boot → `Bootstrap OK` → Game. WebGL-билд не подтверждён.
- Сделано:
  - создана структура `Assets/_Game/` по PLAN.md;
  - написаны сервисы ядра, заглушка платформы и адаптер Canvas Scaler;
  - сделан пункт меню `ShaurmaTycoon/Setup Project`: он создаёт сцены Boot/Game, прописывает Build Settings и применяет WebGL-настройки;
  - удалены пакеты Visual Scripting, Collab Proxy и Timeline;
  - выполнены `git init` и `.gitignore`.
- Файлы:
  - `Assets/_Game/Scripts/ShaurmaTycoon.asmdef`, `Assets/_Game/Scripts/Editor/ShaurmaTycoon.Editor.asmdef`
  - `Core/`: `Services.cs`, `ITickable.cs`, `TickManager.cs`, `GameEvents.cs`, `GameBootstrap.cs`
  - `Platform/`: `IPlatformService.cs`, `EditorPlatformService.cs`
  - `UI/CanvasScalerAdapter.cs`
  - `Editor/ProjectSetupTool.cs`
- Решения:
  - Color Space = Gamma (совместимость со старыми мобильными браузерами);
  - остаёмся на URP 2D;
  - WebGL: Brotli + Decompression Fallback, Strip Engine Code, Run In Background = выкл., Default Canvas 1080×1920;
  - сцены и настройки создаются редакторским инструментом, а не правкой YAML вручную. Повторный запуск не перезаписывает существующие сцены;
  - `TickManager` ограничивает dt одной секундой. Долгое отсутствие покрывает офлайн-доход (этап 07);
  - `CanvasScalerAdapter`: в портрете референс 1080×1920 и подгонка по ширине, в ландшафте 1920×1080 и подгонка по высоте. Есть событие `OrientationChanged` для раскладки UI на этапе 03;
  - тестовая надпись сделана на встроенном `UI.Text`, потому что TMP Essentials ещё не импортированы.
- Известные проблемы / TODO:
  - `Assets/Scenes/SampleScene.unity` убрана из Build Settings, но файл не удалён;
  - WebGL-билд после Setup Project не подтверждён;
  - ~~Play сразу из сцены Game падал на `Services.Get`~~ — исправлено на этапе 03: `Editor/PlayModeStartScene.cs` всегда стартует Play со сцены Boot;
  - на этапе 03 импортировать TMP Essentials и убрать `DebugLabel_Stage00`.

### Этап 01 — Экономика и форматирование чисел (готов, 2026-10-04)
- Проверено: компилируется в Unity, EditMode-тесты в Test Runner зелёные.
- Сделано:
  - `Wallet`: монеты, событие `CoinsChanged`, защита от NaN, отрицательных значений и переполнения;
  - `BigNumberFormatter`: 3 значащие цифры, суффиксы K/M/B/T/aa…zz, точка в любой локали;
  - `EconomyMath`: цена уровня, ×N через сумму прогрессии, ×Max через логарифм с коррекцией;
  - `Wallet` зарегистрирован в `GameBootstrap`.
- Файлы:
  - `Assets/_Game/Scripts/Economy/`: `Wallet.cs`, `BigNumberFormatter.cs`, `EconomyMath.cs`
  - `Assets/_Game/Tests/EditMode/`: `ShaurmaTycoon.Tests.EditMode.asmdef`, `BigNumberFormatterTests.cs`, `EconomyMathTests.cs`, `WalletTests.cs`
  - изменён `Core/GameBootstrap.cs`
- Решения:
  - отображение отбрасывает лишние цифры, а не округляет (999 999 → `999K`, а не `1.00M`): игрок не видит больше, чем у него есть;
  - числа меньше 1000 показываются целыми, отбрасыванием;
  - `level` в формулах — число уже купленных уровней, цена следующего = `baseCost × growth^level`;
  - ×Max ограничен `EconomyMath.MaxBulkCount` = 1 000 000 (защита от вырожденных конфигов);
  - growth < 1 — ошибка конфига, бросается `ArgumentOutOfRangeException`;
  - экономика (`ShaurmaTycoon.Economy`) не использует UnityEngine.
- Известные проблемы / TODO:
  - нет.

### Этап 02 — Станции (готов, 2026-10-04)
- Проверено в Unity: компиляция, EditMode-тесты зелёные, ручная проверка через отладочную панель (тап, покупка, веха 25, открытие станции 2).
- Сделано:
  - `IStationDefinition` + `StationConfig` (SO), `MilestoneTable`;
  - `StationModel`: уровень, цикл по тапу, автоцикл с подсчётом нескольких циклов за кадр, покупка ×N/×Max;
  - `StationController`: `ITickable`, доход за тик в `Wallet` одним событием;
  - `BalanceConfig` (вехи), `CityConfig` (станции города);
  - `GameSceneInstaller` в сцене Game: создаёт `StationController` и регистрирует его в `Services` и `TickManager`;
  - временная `StationsDebugPanel` (IMGUI);
  - ассеты: `Configs/Balance.asset`, `Configs/Cities/City_01_Home.asset`, `Configs/Stations/City01/Station_01…06.asset` по таблице GDD §4.
- Файлы:
  - `Scripts/Stations/`: `IStationDefinition.cs`, `StationConfig.cs`, `MilestoneTable.cs`, `StationModel.cs`, `StationController.cs`
  - `Scripts/Core/`: `BalanceConfig.cs`, `GameSceneInstaller.cs`
  - `Scripts/Prestige/CityConfig.cs`
  - `Scripts/UI/StationsDebugPanel.cs`
  - `Tests/EditMode/`: `TestStationDefinition.cs`, `MilestoneTableTests.cs`, `StationModelTests.cs`, `StationControllerTests.cs`
  - изменена `Scenes/Game.unity` (объект `GameSceneInstaller`)
- Решения:
  - станция 1 стартует с уровнем 1 (`startLevel = 1`), это «Откр. за 0». Открытие остальных станций = покупка 1-го уровня за base cost;
  - веха ускоряет цикл (время ÷ 2), а доход за цикл не меняет. `milestoneMult` из формулы GDD реализован как скорость;
  - growth = 1.07 у всех станций. Вариант 1.15 для станций 1–2 оставлен на этап 11;
  - прогресс цикла хранится долей (0..1), поэтому при ускорении на вехе доля сохраняется;
  - ручной цикл за один большой dt даёт максимум один цикл, автоцикл — все завершённые;
  - `CityConfig` создан сейчас с одним полем (список станций), этап 08 его расширит;
  - конфиги и правка сцены сделаны вручную в YAML с заранее заданными GUID в `.meta`.
- Известные проблемы / TODO:
  - `StationsDebugPanel` убрать на этапе 03;
  - доход/сек для HUD ещё не считается (этап 03/04);
  - названия станций пока ключи локализации (`station.*`), таблица локализации появится на этапе 03.

### Этап 03 — UI игрового экрана (готов, 2026-10-04)
- Проверено в Unity: Build Game UI выполнен, Play из Boot без исключений, пользователь подтвердил переход дальше. WebGL-билд не подтверждён.
- Сделано:
  - TMP Essentials распакованы в `Assets/TextMesh Pro/` (из пакета `com.unity.textmeshpro`), ссылка на `Unity.TextMeshPro` в asmdef;
  - локализация: `LocTable` (TSV), `Loc` (язык, откат ru → en → ключ, нормализация кода платформы), `LocalizedText`, таблица `Configs/Localization/strings.txt` (ru/en);
  - `GameBootstrap` создаёт `Loc` после инициализации платформы, таблица привязана в сцене Boot;
  - язык в редакторе: меню `ShaurmaTycoon/Editor Language`, хранится в EditorPrefs и в Play переключается сразу;
  - UI: `HudView`, `StationRowView`, `StationsPanel`, `BuyModeButton`, `TabBar`, `ResponsiveLayout`, `GameScreen`;
  - `StationModel.IncomePerSecond`, `StationController.IncomePerSecond` (только станции с автоциклом), `IStationDefinition.NameKey`;
  - `GameUiBuilder` (меню `ShaurmaTycoon/Build Game UI`) собирает `Prefabs/UI/StationRow.prefab` и `GameScreen` в сцене Game, убирает `DebugLabel_Stage00`;
  - удалён `StationsDebugPanel` (скрипт и компонент в сцене Game).
- Файлы:
  - `Scripts/Localization/`: `LocTable.cs`, `Loc.cs`, `LocalizedText.cs`
  - `Scripts/UI/`: `BuyMode.cs`, `BuyModeButton.cs`, `HudView.cs`, `StationRowView.cs`, `StationsPanel.cs`, `TabBar.cs`, `ResponsiveLayout.cs`, `GameScreen.cs`
  - `Scripts/Editor/`: `GameUiBuilder.cs`, `EditorLanguageMenu.cs`
  - `Configs/Localization/strings.txt`
  - `Tests/EditMode/`: `LocTableTests.cs`, `LocTests.cs`, `StringsFileTests.cs` (+ тест `IncomePerSecond`)
  - изменены: `GameBootstrap.cs`, `EditorPlatformService.cs`, `IStationDefinition.cs`, `StationModel.cs`, `StationController.cs`, `TestStationDefinition.cs`, оба asmdef, `Boot.unity`, `Game.unity`
- Решения:
  - UI собирается редакторским инструментом, а не правкой YAML: результат — обычные объекты и префаб, их можно править руками. Повторная сборка спрашивает подтверждение, потому что перезапишет правки;
  - раскладка: HUD 200 и вкладки 180 по краям. Портрет: ларёк 520 под HUD, ниже список. Ландшафт: ларёк слева, список справа. Ларёк пока цветная заглушка;
  - прогресс-бар меняет `anchorMax.x` у Fill (без спрайта Filled), тексты строк меняются только при изменении уровня, режима покупки, языка или доступности покупки;
  - доход/сек в HUD скрыт, пока нет станций с автоциклом (этап 04);
  - режим ×Max показывает и покупает `max(1, доступно)`, кнопка неактивна, если не хватает даже на 1;
  - коды языка платформы: ru/be/kk/uk/uz → ru, остальные → en, пустой → ru;
  - кириллица — через динамический fallback-атлас LiberationSans из TMP Essentials.
- Известные проблемы / TODO:
  - в редакторе вне Play `LocalizedText` показывает ключи, потому что `Loc` создаётся только в Play;
  - `BigNumberFormatter` и `Loc.Format` создают строки (аллоцируют), хотя и только при изменении значений. Полностью без аллокаций — этап 05;
  - WebGL-билд всё ещё не подтверждён (долг этапа 00).

### Этап 04 — Менеджеры и апгрейды (готов, 2026-10-04)
- Проверено в Unity: Build Game UI выполнен, Play из Boot без ошибок, пользователь подтвердил переход дальше.
- Сделано:
  - `StationModel`: `IncomeMultiplier` (upgradesMult) и событие `Changed` (уровень, автоматизация, множитель); `IsAutomated` теперь сообщает об изменении;
  - `ManagerController`: цена = base cost × `BalanceConfig.ManagerCostMultiplier` (1000), найм только на открытой станции, повторно нельзя, событие `ManagerHired`;
  - `IUpgradeDefinition`, `UpgradeConfig` (SO), `UpgradeService`: покупка, множитель станции = произведение купленных улучшений этой станции и улучшений «для всех»;
  - 20 ассетов `Configs/Upgrades/City01/Upgrade_01…20`, подключены в `City_01_Home._upgrades`;
  - UI: `ManagersPanel` + `ManagerRowView`, `UpgradesPanel` + `UpgradeRowView` (сортировка по цене, купленные скрыты, «Все улучшения куплены!»), общая палитра `StationColors`;
  - `GameUiBuilder`: общий `BuildListPanel`, префабы `ManagerRow` и `UpgradeRow`, рабочие вкладки вместо заглушек (кроме «Город»);
  - строки ru/en для менеджеров и улучшений.
- Файлы:
  - `Scripts/Managers/ManagerController.cs`
  - `Scripts/Upgrades/`: `IUpgradeDefinition.cs`, `UpgradeConfig.cs`, `UpgradeService.cs`
  - `Scripts/UI/`: `StationColors.cs`, `ManagersPanel.cs`, `ManagerRowView.cs`, `UpgradesPanel.cs`, `UpgradeRowView.cs`
  - `Tests/EditMode/`: `TestUpgradeDefinition.cs`, `ManagerControllerTests.cs`, `UpgradeServiceTests.cs` (+ тесты в `StationModelTests`)
  - изменены: `StationModel.cs`, `BalanceConfig.cs`, `CityConfig.cs`, `GameSceneInstaller.cs`, `GameScreen.cs`, `StationsPanel.cs`, `StationRowView.cs`, `GameUiBuilder.cs`, `Balance.asset`, `City_01_Home.asset`, `strings.txt`, `StringsFileTests.cs`
- Решения:
  - отдельного `ManagerModel` нет: «нанят» = `StationModel.IsAutomated` (имён и бонусов у менеджеров в GDD нет);
  - **предварительный баланс апгрейдов** (в GDD цен нет, тюнинг на этапе 11):
    - 3 круга «станция ×3» на каждую станцию по цене base cost × 250 / × 25 000 / × 2 500 000;
    - «Все станции ×2» за 500M и 50B;
  - цель апгрейда, которой нет в городе, — ошибка конфига (`ArgumentException` при создании сервиса);
  - `UpgradeConfig.TargetStation` возвращает настоящий null вместо «фейкового null» Unity.
- Известные проблемы / TODO:
  - менеджеры и апгрейды не сохраняются (этап 06).

### Этап 05 — Клиенты и «сочность» (готов, 2026-10-04)
- Проверено в Unity: Build Game UI выполнен (созданы `Rubik SDF.asset`, `Audio.asset`, `Customers.asset`), Play без исключений, пользователь подтвердил переход дальше. В `AllocationTests` добавлен прогрев делегата: первый вызов JIT-компилирует лямбду и давал ложный GC.Alloc.
- Решения пользователя: свой мини-твинер вместо DOTween; звуки и персонажи — Kenney CC0; шрифт Rubik (OFL). Скачаны 4 файла с разрешения пользователя, в проект скопировано только нужное (≈0.85 МБ), источники и лицензии — в `Assets/_Game/CREDITS.md`.
- Сделано:
  - `NumberText` (сборка текста в `char[]`, единственная реализация форматирования; `BigNumberFormatter` теперь обёртка над ним), `LocTemplate` + `Loc.GetTemplate`, `TextExtensions.SetNumberText` → тексты HUD, строк станций и найма без аллокаций;
  - твинер `Core/Tweening`: `TweenRunner` (scale, позиция с дугой, альфа; пул структур; колбэк через `ITweenListener`), `Ease`/`EaseFunctions`;
  - звук `Core/Audio`: `AudioService` (6 AudioSource по кругу, случайный клип и питч, минимальный интервал, пауза по `GameEvents.GamePaused`), `AudioConfig`/`SoundEntry`/`SoundId`;
  - клиенты `Customers/`: `CustomerQueue` (очередь, реплика в облачке, оплата монетой, уход; тап по клиенту → `StationController.TapAllIdle`), `CustomerView`, `KioskView` (продавец, «вращающийся» вертел), `CustomerConfig`/`CustomerSkin`, `CustomerDemand` и `IncomeRateTracker` (чистый C#);
  - эффекты UI: `CoinFlyEffect` (монеты по дуге в HUD, иконка подпрыгивает, «дзынь»), `FloatingTextEffect` («+X»), `ButtonFeedback` (сжатие/отскок + звук), `PulseEffect` + `PulseDriver`;
  - события: `StationModel.CycleCompleted`, `StationController.Earned`;
  - редактор: `ContentSetup` (TMP-ассет Rubik и шрифт по умолчанию с fallback LiberationSans; `Configs/Audio.asset`, `Configs/Customers.asset`, если их нет), `SpriteImportSettings` (PNG из `Sprites/` → Sprite);
  - `GameUiBuilder`: нарисованный ларёк (тротуар, будка, окно, продавец, вертел, прилавок, полосатый навес, вывеска), улица с пулом из 9 клиентов и облачком, слой эффектов (24 монеты, 12 текстов), звуки и пульс на кнопках;
  - строки: 10 реплик клиентов ru/en; ключ `kiosk.placeholder` удалён.
- Файлы:
  - `Audio/*.ogg` (10), `Sprites/Customers/*/` (44 PNG), `Fonts/Rubik-Variable.ttf` + `Rubik-OFL.txt`, `CREDITS.md`
  - `Scripts/Core/Tweening/`: `Ease.cs`, `EaseFunctions.cs`, `TweenProperty.cs`, `ITweenListener.cs`, `TweenRunner.cs`
  - `Scripts/Core/Audio/`: `SoundId.cs`, `SoundEntry.cs`, `AudioConfig.cs`, `AudioService.cs`
  - `Scripts/Customers/`: `CustomerSkin.cs`, `CustomerConfig.cs`, `CustomerDemand.cs`, `IncomeRateTracker.cs`, `CustomerView.cs`, `CustomerQueue.cs`, `KioskView.cs`
  - `Scripts/Economy/NumberText.cs`, `Scripts/Localization/LocTemplate.cs`
  - `Scripts/UI/`: `TextExtensions.cs`, `CoinFlyEffect.cs`, `FloatingTextEffect.cs`, `ButtonFeedback.cs`, `PulseEffect.cs`, `PulseDriver.cs`
  - `Scripts/Editor/`: `ContentSetup.cs`, `SpriteImportSettings.cs`
  - `Tests/EditMode/`: `NumberTextTests.cs`, `EaseTests.cs`, `TweenRunnerTests.cs`, `AllocationTests.cs`, `CustomerLogicTests.cs` (+ тесты в `StationModelTests`, `StationControllerTests`, `LocTests`)
  - изменены: `BigNumberFormatter`, `Loc`, `StationModel`, `StationController`, `GameBootstrap`, `GameSceneInstaller`, `GameScreen`, `HudView`, `StationsPanel`, `StationRowView`, `ManagerRowView`, `GameUiBuilder`, тестовый asmdef (+ `UnityEngine.UI`), `strings.txt`, `StringsFileTests`
- Решения:
  - `TweenRunner` и `PulseDriver` живут в Boot (нужны до загрузки сцены), `AudioService` и `CustomerConfig` — в сцене Game (`GameSceneInstaller`, ссылки назначает Build Game UI);
  - тап по клиенту запускает **все** простаивающие станции без менеджера;
  - очередь: размер = 1 + ⌊log10(доход/с + 1) × 1.2⌋, не больше 7; обслуживание = 3 с / (1 + 0.6 × log10(доход/с + 1)), от 0.7 до 3 с; доход/с — сглаженный фактический (τ = 5 с). Всё визуально, коэффициенты в `Customers.asset`;
  - монеты от станции не чаще раза в 0.25 с: из иконки строки, если вкладка «Станции» открыта, иначе из окна ларька. Монета от оплаты клиента — только визуал, денег не добавляет;
  - звуки: тап `click_002`, монета `chips-collide-1..3`, покупка `confirmation_001`, открытие `maximize_006`, найм `confirmation_002`, улучшение `confirmation_003`, вкладка `switch_002`, облачко `pluck_001` (меняются в `Audio.asset`);
  - суффиксов aa…zz хватает на весь диапазон double (max ≈ «179dt»), научная запись не нужна;
  - тексты, которые меняются только при покупке (уровень, время цикла, веха), остаются обычными строками: аллокация допустима в момент покупки.
- Известные проблемы / TODO:
  - Rubik — вариативный шрифт, TMP берёт его начертание по умолчанию, жирный — синтетический. Если будет выглядеть тонко, на этапе 12 можно запечь статический атлас;
  - атласы спрайтов (меньше draw calls) — этап 12;
  - WebGL-билд всё ещё не подтверждён.

### Этап 06 — Сохранения (готов, 2026-10-04)
- Проверено в Unity: Play без исключений, пользователь подтвердил переход дальше. Сценарии меню Save (legacy и испорченный сейв) по логу не запускались — покрыты тестами `JsonSaveSerializerTests`, `SaveServiceTests`, `SaveMigratorTests`.
- Сделано:
  - `Save/`:
    - `SaveData` (Version, Coins, SavedAtUtcTicks, Stations, PurchasedUpgrades), `StationSave`;
    - `SaveMigrator` (версия 0 → 1, нормализация значений);
    - `ISaveStorage` + `PlatformSaveStorage` (через `IPlatformService`), `ISaveSerializer` + `JsonSaveSerializer` (JsonUtility);
    - `ISaveParticipant` + `CitySaveParticipant` (монеты, станции по id, улучшения по id);
    - `SaveService` (загрузка, участники, автосейв каждые 10 с, сохранение при потере фокуса, паузе и выходе, события `LoadFailed`/`SaveFailed`);
  - `StationModel.Restore`, `UpgradeService.Restore` и `CollectPurchasedIds`;
  - `Core/ApplicationEvents` (OnApplicationFocus/Pause/Quit → `GameEvents`), новые события в `GameEvents`;
  - `GameBootstrap`: платформа → Loc → `SaveService.Load` → сцена Game; `GameSceneInstaller` регистрирует `CitySaveParticipant` в Awake (до того как UI прочитает модели);
  - меню `ShaurmaTycoon/Save/`: Print, Delete, Write Legacy Test Save (`{"Coins":500}`), Write Corrupt Test Save.
- Файлы:
  - `Scripts/Save/`: `SaveData.cs`, `StationSave.cs`, `SaveMigrator.cs`, `ISaveStorage.cs`, `PlatformSaveStorage.cs`, `ISaveSerializer.cs`, `JsonSaveSerializer.cs`, `ISaveParticipant.cs`, `CitySaveParticipant.cs`, `SaveService.cs`
  - `Scripts/Core/ApplicationEvents.cs`, `Scripts/Editor/SaveMenu.cs`
  - `Tests/EditMode/`: `SaveMigratorTests.cs`, `CitySaveParticipantTests.cs`, `SaveServiceTests.cs`, `JsonSaveSerializerTests.cs`
  - изменены: `StationModel.cs`, `UpgradeService.cs`, `GameEvents.cs`, `GameBootstrap.cs`, `GameSceneInstaller.cs`, `EditorPlatformService.cs` (публичный `SaveKey`)
- Решения:
  - имена полей JSON = имена полей C# (`Coins`, `Stations`…), переименовывать нельзя без миграции;
  - станции и улучшения ищутся по `Id`; станции без записи в сейве остаются как в конфиге; неизвестные id игнорируются;
  - `SaveService` — чистый C#: события Unity подключаются к нему в `GameBootstrap`, логирование — через его события;
  - испорченный сейв → предупреждение в консоли и новая игра; резервной копии нет, автосейв его перезапишет (резерв — этап 09 вместе с облаком);
  - время сохранения берётся из `IPlatformService.ServerTimeUtc` (для этапа 07);
  - автосейв считает игровое время `TickManager`, на паузе (реклама) не идёт; аллокация при сохранении раз в 10 с допустима.
- Известные проблемы / TODO:
  - сейв из более новой версии игры (Version > 1) загрузится, но при записи станет версии 1 — защиту добавим, когда появится версия 2;
  - JsonUtility хранит double с ~15 значащими цифрами: огромные суммы монет после загрузки могут отличаться в 16–17-м знаке (незаметно; тест сравнивает с относительной точностью 1e-14).

### Этап 07 — Офлайн-доход (готов, 2026-10-04)
- Проверено в Unity: Build Game UI выполнен, Play без исключений, пользователь подтвердил переход дальше. Сценарии Shift Save Time по логу не запускались — расчёт покрыт `OfflineIncomeTests`.
- Сделано:
  - `Offline/`:
    - `OfflineSettings` (доля 0.5, лимит, минимум отсутствия), `OfflineIncome` (секунды, сумма, обрезано ли);
    - `OfflineIncomeCalculator` (нет сейва, часы назад, меньше минимума, нет менеджеров → 0; обрезание лимитом);
    - `OfflineIncomeService` (одна выдача, ×1 или ×2 за рекламу, событие `Claimed`);
  - `BalanceConfig` + `Balance.asset`: `offlineIncomeRate` 0.5, `offlineLimitHours` 2, `offlineMinAwaySeconds` 60, `offlineAdMultiplier` 2;
  - `GameSceneInstaller` считает офлайн-доход сразу после применения сейва (`StationController.IncomePerSecond` × время с `SaveData.SavedAtUtcTicks` по `IPlatformService.ServerTimeUtc`);
  - `OfflineIncomePopup`:
    - затемнение, срок отсутствия, «+X», пометка лимита;
    - «Забрать» и «Забрать ×2 за рекламу» через `IPlatformService.ShowRewarded`;
    - монеты в HUD, сразу `SaveNow`;
  - `DurationFormatter` («1 ч 23 мин»), строки ru/en;
  - `SaveMenu`: Shift Save Time −1 h / −3 h / +1 h (часы назад).
- Файлы:
  - `Scripts/Offline/`: `OfflineSettings.cs`, `OfflineIncome.cs`, `OfflineIncomeCalculator.cs`, `OfflineIncomeService.cs`
  - `Scripts/Localization/DurationFormatter.cs`, `Scripts/UI/OfflineIncomePopup.cs`
  - `Tests/EditMode/OfflineIncomeTests.cs`
  - изменены: `BalanceConfig.cs`, `Balance.asset`, `GameSceneInstaller.cs`, `GameScreen.cs`, `GameUiBuilder.cs`, `SaveMenu.cs`, `strings.txt`, `StringsFileTests.cs`
- Решения:
  - порог 60 с (в GDD нет), чтобы окно не всплывало после каждого перезапуска;
  - незабранная награда сгорает при выходе (время сейва обновляется) — так её нельзя получить дважды;
  - если реклама не досмотрена, кнопки снова активны и можно забрать ×1;
  - лимит пока базовый (2 ч), бонус из магазина репутации — этап 08.
- Известные проблемы / TODO:
  - время берётся из `ServerTimeUtc`, а в редакторе это локальные часы: перевод часов вперёд не защищён (TODO(YANDEX): серверное время, этап 09);
  - пауза игры и звука на время рекламы — этап 09 (`AdService`).

### Этап 08 — Престиж и города (код готов, 2026-10-04; ждёт Build Game UI и проверки в Unity)
- Проверено: runtime, editor и тестовая сборка компилируются без warnings (вне редактора). Чистая логика и тесты вне Unity: 252/252 зелёные. В Unity ещё не запускалось.
- Решения пользователя:
  - бонус +2% считается от всей заработанной репутации, траты в магазине его не уменьшают;
  - порог ×10 за город (1e9, 1e10);
  - в последнем городе — перезапуск того же города;
  - набор магазина:
    - офлайн +1…6 ч за 5/10/20/40/80/160;
    - старт 1K/100K/10M × масштаб города за 3/15/75;
    - бесплатные менеджеры 1/2/3 за 10/50/250;
    - циклы −10/−20/−30% за 20/80/320.
- Сделано:
  - `Prestige/`:
    - `PrestigeRules` (sqrt-формула, множитель), `PrestigeState` (CityIndex, CityEarned, LifetimeEarned, ReputationEarned/Spent, уровни магазина), `PrestigeSaveParticipant` (в Boot);
    - `ShopEffect`, `ShopItemConfig`, `ReputationShopConfig`, `ReputationShop`;
    - `PrestigeController` (прогноз, порог, переход: отключить город от сейва → начислить → сбросить станции/улучшения/монеты в сейве → SaveNow → перезагрузить сцену);
    - `CityCatalog`; `CityConfig` + порог, масштаб, цвета;
  - `Save/`: `PrestigeSave`, `ShopLevelSave`, `SaveData.Prestige`, нормализация в `SaveMigrator`;
  - `StationModel`/`StationController.SetGlobalModifiers` (доход × престиж, скорость циклов);
  - `ManagerController.SetFreeManagers` (автонайм, когда станция открыта); `OfflineSettings.WithExtraLimit`;
  - `Services.Unregister(instance)` — сцена снимает только свои сервисы (важно при перезагрузке);
  - `GameSceneInstaller`: город из каталога по `PrestigeState.CityIndex`, магазин, модификаторы (пересчёт при покупке), учёт заработка (станции + полученный офлайн), лимит офлайна + бонус, `PrestigeController`;
  - UI:
    - репутация в HUD;
    - вкладка «Город» (`CityPanel`): прогресс к порогу, прогноз, бонус, свободная репутация, кнопка перехода, магазин (`ShopItemRowView`);
    - `PrestigeConfirmPopup`; цвета неба и ларька по городу (`KioskView.ApplyTheme`);
  - ассеты:
    - `City_02_Kazan` (порог 1e10, масштаб 10, голубое небо, зелёный ларёк);
    - 6 станций Казани (значения ×10), 20 улучшений (цены ×10);
    - `CityCatalog`; `ReputationShop.asset` создаёт `ContentSetup`;
  - `Balance.asset`: `reputationDivisor` 1e6, `incomeBonusPerReputation` 0.02;
  - временное меню `ShaurmaTycoon/Debug/Add 1e9 | 1e10 Earned`;
  - строки ru/en: Казань и станции, престиж, магазин; тест: у всех `_nameKey` в ассетах есть перевод.
- Файлы:
  - `Scripts/Prestige/`: `CityCatalog.cs`, `PrestigeRules.cs`, `PrestigeState.cs`, `PrestigeSaveParticipant.cs`, `ShopEffect.cs`, `ShopItemConfig.cs`, `ReputationShopConfig.cs`, `ReputationShop.cs`, `PrestigeController.cs` (+ изменён `CityConfig.cs`)
  - `Scripts/Save/`: `PrestigeSave.cs`, `ShopLevelSave.cs`
  - `Scripts/UI/`: `CityPanel.cs`, `ShopItemRowView.cs`, `PrestigeConfirmPopup.cs`
  - `Scripts/Editor/DebugMenu.cs`
  - `Configs/Cities/City_02_Kazan.asset`, `Configs/Cities/CityCatalog.asset`, `Configs/Stations/City02/*`, `Configs/Upgrades/City02/*`
  - `Tests/EditMode/PrestigeTests.cs` (+ `StringsFileTests.ConfigNameKeys_HaveBothTranslations`)
- Решения:
  - `totalEarned` в формуле репутации = заработано за всё время (все города); порог — заработано в текущем городе;
  - заработком считаются доход станций и полученный офлайн-доход; стартовый капитал и отладочное меню — нет (меню добавляет и то, и другое явно);
  - переход перезагружает сцену Game, все системы города создаются заново из сейва;
  - у Казани свои ассеты станций и улучшений (не множитель к городу 1): названия и значения можно править по отдельности;
  - `CityConfig` в `GameSceneInstaller._city` остался как запасной вариант, пока `_cities` не назначен.
- Известные проблемы / TODO:
  - interstitial при переходе — этап 09; лидерборд по `LifetimeEarned` — этап 09;
  - города 3+ — LiveOps (добавить `CityConfig` в каталог);
  - `DebugMenu` заменить отладочным окном на этапе 11.

### Этап 08+ — Карта городов, рост ларька, города 3–5 (код готов, 2026-10-04; ждёт Build Game UI и проверки в Unity)
- Проверено: runtime, editor и тесты компилируются без ошибок (вне редактора). Тесты вне Unity: 229 зелёных, 42 падают только из-за среды (String.Split(char) из netstandard 2.1 и JsonUtility недоступны в .NET Framework) — в Unity их нужно прогнать. Новые CityAssetsTests: 6/6. Ключи локализации проверены скриптом.
- Сделано:
  - вкладка «Город» — карта филиалов (`CityMapView`, `CityMapNodeView`): города по `CityConfig.MapPosition`, дорога из точек (пройденные — зелёные, путь к следующему закрашивается по прогрессу), фургон на дороге = прогресс к порогу, метка «Вы здесь», следующий город пульсирует; тап по городу — плашка с названием, статусом, порогом и масштабом;
  - на той же карточке под картой: прогресс «Заработано: X / порог», репутация за переезд, бонус и свободная репутация, кнопка перехода; ниже магазин репутации;
  - ларёк растёт с менеджерами (`KioskView.BindManagers`): 6 ламп над вывеской + украшения по числу нанятых — гирлянда, меню-доска, столик под зонтом, рамка вывески, цветы, флаги (в цвете города); появление с анимацией, отскок ларька и надпись «Ларёк преобразился!»;
  - города 3–5: Сочи (порог 1e11, ×100), Санкт-Петербург (1e12, ×1000), Москва (1e13, ×10000) — по 6 станций и 20 улучшений (значения Казани ×10 за шаг), свои цвета;
  - Debug-меню: «Add City Threshold Earned», «Hire Next Manager».
- Файлы: `UI/CityMapView.cs`, `UI/CityMapNodeView.cs`, `Tests/EditMode/CityAssetsTests.cs`; изменены `CityConfig`, `CityPanel`, `GameScreen`, `KioskView`, `PrestigeController` (+`CityIndex`), `GameUiBuilder`, `DebugMenu`, `strings.txt`; ассеты `Configs/Cities/City_03..05`, `Configs/Stations|Upgrades/City03..05`.
- Решения:
  - раскладка карты — якорями в долях области, без зависимости от размера экрана; шаг точек дороги — по `_referenceSize`;
  - `prestige.city_label` и `prestige.earned_label` удалены, вместо них `prestige.earned_progress`.
- Известные проблемы / TODO:
  - нужно выполнить ShaurmaTycoon/Build Game UI (пересоберёт экран и префабы CityMapNode, RoadDot);
  - баланс городов 3–5 — копия Казани ×10, прогнать на этапе 11.

### Этап 08a — Звук и фоновая музыка (код готов, 2026-10-04; ждёт Build Game UI и проверки в Unity)
- Проверено: runtime, editor и тесты компилируются без ошибок (вне редактора). В Unity не запускалось.
- Причина тишины: ни в одной сцене не было `AudioListener`, а музыки в коде не было совсем.
- Сделано:
  - `GameBootstrap` создаёт `DontDestroyOnLoad`-объект «Audio» с единственным `AudioListener` и `MusicService`;
  - `Core/Audio/MusicService`: зацикленный трек, плавное появление, смена трека через затухание, повторный `Play` того же трека игнорируется (переезд не обрывает музыку), `Muted` для этапа 08b (затухание → пауза → продолжение с того же места), пауза на `GameEvents.GamePaused`;
  - `Core/Audio/MusicConfig` (SO) + `Configs/Music.asset` создаёт `ContentSetup`; `GameSceneInstaller._music` назначает Build Game UI;
  - музыка: `Audio/Music/HappyClappyLoop.wav` (OwlishMedia, CC0, OpenGameArt), импорт Streaming + Vorbis 50%; лицензии — `Audio/LICENSES.md`.
- Решения:
  - слушатель создаётся кодом, а не в сценах: так он ровно один и переживает перезагрузку Game (`ProjectSetupTool` не трогали);
  - отдельный звук заработка не добавлен: каждая монета, прилетевшая в счётчик, уже звенит (`SoundId.Coin`, фишки Kenney). Пользователь одобрил только скачивание музыки;
  - звук нажатия — существующий `click_002` на всех кнопках через `ButtonFeedback`;
  - в WebGL браузер включает звук после первого клика игрока — Unity делает это сама.
- Известные проблемы / TODO:
  - выполнить ShaurmaTycoon/Build Game UI (создаст `Music.asset` и назначит его);
  - трек короткий (~17 с) — если надоест, добавить второй и чередовать.

### Этап 08b — Настройки: звук и музыка (код готов, 2026-10-04; ждёт Build Game UI и проверки в Unity)
- Проверено: runtime, editor и тесты компилируются без ошибок. Тесты вне Unity: 233 зелёных, 42 падают только из-за среды (как на 08+). Новые SettingsTests: 4/4. Ключи строк проверены скриптом.
- Сделано:
  - `Core/Settings/GameSettings` (SoundOn, MusicOn, `Changed`), `SettingsSaveParticipant`, `Save/SettingsSave` (по умолчанию всё включено), `SaveData.Settings` + нормализация в `SaveMigrator` (версия не повышалась — поле совместимое);
  - `GameBootstrap`: `GameSettings` в Boot, музыка слушает `Changed` (`MusicService.Muted`), каждое переключение сразу сохраняется (`SaveNow`);
  - `AudioService` получает `GameSettings` и молчит, когда звуки выключены;
  - UI: шестерёнка в HUD слева сверху, `SettingsPopup` с переключателями «Звуки» и «Музыка» (зелёный Вкл / серый Выкл), кнопка «Готово»;
  - строки `settings.*` ru/en.
- Решения: выключенная музыка затухает и встаёт на паузу; при включении продолжается с того же места.
- Известные проблемы / TODO: выполнить ShaurmaTycoon/Build Game UI.

### Этап 08c — Купюры (код готов, 2026-10-04; ждёт Build Game UI и проверки в Unity)
- Проверено: runtime, editor и тесты компилируются без ошибок. Тесты вне Unity: 245 зелёных, 42 падают только из-за среды. Новые: BillWalletTests 11/11, `PrestigeTests.Controller_TryPrestige_KeepsBills`.
- Сделано:
  - `Economy/BillWallet` (long, без минуса, переполнение → MaxValue, событие `BillsChanged(bills, delta)`), enum `BillSource` / `BillSink` для будущей аналитики;
  - `Economy/BillsSaveParticipant`, `SaveData.Bills` (по умолчанию 0, отрицательное → 0 в `SaveMigrator`), регистрация в Boot — переезд купюры не трогает;
  - HUD: счётчик купюр справа сверху (зелёная банкнота), репутация сдвинута под него; при начислении — отскок и «+N»; тап поднимает `HudView.BillsClicked` (магазин подключит этап 08d);
  - Debug-меню: «Add 100 Bills»; строка `bills.name`.
- Решения: отдельный класс `BillsSave` не заводили — достаточно поля `SaveData.Bills`.
- Известные проблемы / TODO: выполнить ShaurmaTycoon/Build Game UI.

### Этап 08d — Магазин купюр (код готов, 2026-10-04; ждёт Build Game UI и проверки в Unity)
- Проверено: runtime, editor и тесты компилируются без ошибок. Тесты вне Unity: 254 зелёных, 42 падают только из-за среды. Новые BillShopTests: 9/9. Ключи строк проверены скриптом.
- Сделано:
  - `Shop/BillShopConfig` (SO: паки купюр — productId, купюры, цена для показа, «Выгодно!»; мешки монет — цена в купюрах, минуты дохода, минимум) + `Configs/BillShop.asset` создаёт `ContentSetup`;
  - `Shop/BillShop`:
    - мешок = max(доход/с × минуты, минимум × масштаб города);
    - паки — через `IPlatformService.Purchase`, купюры только после успеха, одна покупка за раз;
    - после любой покупки — `SaveNow`;
    - создаётся в `GameSceneInstaller`;
  - 5-я вкладка «Магазин» (`ShopPanel`, префаб `BillShopRow`): секции «Купюры» (20/120/350 за 19/99/249 ян) и «Монеты за купюры» (5/20/60 купюр → доход за 30 мин / 3 ч / 12 ч); награда пересчитывается раз в секунду без аллокаций; при покупке монеты летят в HUD;
  - тап по купюрам в HUD открывает вкладку «Магазин» (`GameScreen._tabBar`, `_shopTabIndex` = 4).
- Решения: купленные монеты идут прямо в `Wallet` и не считаются заработком для престижа (как стартовый капитал).
- Известные проблемы / TODO:
  - выполнить ShaurmaTycoon/Build Game UI;
  - TODO(YANDEX) этап 09: завести товары `bills_small/medium/large` в консоли, брать цены из каталога вместо `DisplayPrice`, обрабатывать неподтверждённые покупки при старте (consume).

### Исправление: падение при переезде в новый город (2026-10-04)
- Симптом: после «Переезжаем!» — `MissingReferenceException` в `UpgradeRowView.RefreshAffordable`, затем `Service PrestigeController is not registered` в `GameScreen.Start` и NRE в `GameSceneInstaller.OnDestroy`.
- Причина (ошибка этапа 08):
  - Unity не вызывает `OnDestroy` у объектов, которые ни разу не были активны (строки закрытых вкладок, скрытые попапы), поэтому их подписки на сервисы Boot (`Wallet`, `Loc`, `PrestigeState`, …) переживали сцену;
  - в новой сцене `CitySaveParticipant.Apply` → `Wallet.SetCoins` вызывал уничтоженную строку, исключение обрывало `GameSceneInstaller.Awake`, и `PrestigeController` с остальными сервисами не регистрировались.
- Исправление:
  - `UI/SceneBindings` + `ISceneBinding`: 10 вью с подписками на Boot-сервисы регистрируются в `Bind`, тело `OnDestroy` вынесено в `Unbind()`;
  - `GameScreen.OnDestroy` (всегда активен) вызывает `SceneBindings.UnbindAll()`;
  - `GameSceneInstaller.OnDestroy` устойчив к недостроенному городу;
  - тест SceneBindingsTests.
- Проверено: компиляция, тесты вне Unity 255 зелёных (+42 средовых). В Unity не проверено — повторить переезд.

### Этап 08e — Покупки за купюры вместо монет (код готов, 2026-10-05; ждёт Build Game UI и проверки в Unity)
- Проверено: runtime, editor и тесты компилируются без ошибок. Тесты вне Unity: 269 зелёных, 42 падают только из-за среды. Новые BillPurchasesTests: 14/14; старые тесты станций, улучшений и менеджеров зелёные после рефакторинга.
- Сделано:
  - `Shop/BillPricing`: 1 купюра = доход за 360 с (как малый мешок: 30 мин / 5 купюр), но не меньше 200 монет × масштаб города; цена = ceil(монеты / курс), минимум 1; курс — поля `BillShopConfig._secondsPerBill` / `_minCoinsPerBill`;
  - `Shop/BillPurchases`: цена и покупка уровней станции (в т. ч. открытие), улучшения, менеджера за купюры; берётся текущая цена, но не больше показанной игроку; после покупки — `SaveNow`;
  - сервисы получили «выдачу без монет», которую используют и обычные покупки: `StationModel.AddLevels`, `StationController.GrantLevels`, `UpgradeService.CanGrant/Grant`, `ManagerController.Grant`;
  - UI:
    - `BillOfferTrigger` на кнопках «купить» (станция, открытие станции, улучшение, найм): когда монет не хватает, на выключенной кнопке значок банкноты, нажатие открывает `BillOfferPopup`;
    - окно «Не хватает монет: купить … за N купюр?»; купюр мало — кнопка «В магазин» (вкладка «Магазин»); «Купить» слева, «Отмена» справа;
  - строки `bills.offer_*`, `bills.item_*`.
- Решения (отступление от промта 08e):
  - вместо отдельной маленькой кнопки «N купюр» — тап по выключенной кнопке и окно подтверждения: в строках нет места без наложения на текст;
  - при частых тапах по «купить» отдельная кнопка тратила бы купюры случайно, поэтому подтверждение теперь всегда, а не только для цен > 10.
- Известные проблемы / TODO: выполнить ShaurmaTycoon/Build Game UI (значки на кнопках строк, окно предложения).

### Этап 08f — Купюры за рекламу (код готов, 2026-10-05; ждёт Build Game UI и проверки в Unity)
- Проверено: runtime, editor и тесты компилируются без ошибок. Тесты вне Unity: 278 зелёных, 42 падают только из-за среды. Новые AdBillRewardTests: 9/9. Ключи строк проверены скриптом.
- Сделано:
  - `Shop/AdBillReward` (чистые правила): +2 купюры, кулдаун 10 мин, лимит 6 просмотров за сутки UTC, сброс в полночь UTC; время назад → «последний просмотр» прижимается к now;
  - `Shop/AdBillService`: `TryWatch` → `IPlatformService.ShowRewarded`; купюры и кулдаун — только при досмотре; повторный тап, пока реклама открыта, игнорируется; сразу `SaveNow`;
    - сам является `ISaveParticipant` → `SaveData.AdBills` (`Save/AdRewardSave`), нормализация в `SaveMigrator`;
    - создаётся в `GameSceneInstaller`, время — `platform.ServerTimeUtc`;
  - `BalanceConfig`: `_adBillsReward` = 2, `_adBillsCooldownMinutes` = 10, `_adBillsDailyLimit` = 6;
  - UI:
    - первая строка вкладки «Магазин»: «Купюры за рекламу», «+2 купюры · сегодня ещё N», кнопка «Смотреть» / «Через m:ss» (раз в секунду, без аллокаций) / «Завтра»;
    - красный бейдж «!» на вкладке «Магазин», когда награда доступна (обновляется и при закрытой вкладке);
  - Debug-меню: «Reset Ad Bills Timer».
- Решения:
  - отдельный вылет купюр не делали — при начислении счётчик в HUD подпрыгивает и показывает «+2» (этап 08c);
  - подарок монет раз в 10 минут (этап 10) — отдельная кнопка, не объединяется (решение пользователя).
- Известные проблемы / TODO:
  - выполнить ShaurmaTycoon/Build Game UI;
  - TODO(YANDEX) этап 09: пауза игры и звука на время rewarded, проверка на черновике.

### Этап 09 — Интеграция Яндекс Игр (код готов, 2026-10-05; ждёт WebGL-билда и проверки на черновике)
- Проверено: runtime компилируется без ошибок и warnings; тесты вне Unity: новые YandexLogicTests 12/12, BillShopTests 12/12 (+5), остальные прежние зелёные (падают только средовые: пути, JsonUtility, UnityEngine). В Unity, WebGL-билде и на Яндексе не проверено.
- Решение по плагину: PluginYG2 v2.0092 стоит **без модулей** (реклама, сейвы, покупки, лидерборд — отдельные unitypackage с max-games.ru), поэтому платформа сделана собственным `.jslib`-мостом поверх `ysdk` из шаблона плагина (`Assets/WebGLTemplates/YandexGames`). От плагина используются только шаблон, WebGL-настройки и сборка; в `SettingsYG2.asset` выключены `autoGRA` и `autoPauseGame` (Game Ready и паузу ведёт игра). Игровой код по-прежнему видит только `IPlatformService`.
- Сделано:
  - `Plugins/WebGL/YandexBridge.jslib` + `Platform/YandexBridge` (ответы через `SendMessage('YandexBridge','OnResult','id|статус|данные')`, id 0 — события паузы и видимости вкладки);
  - `Platform/YandexPlatformService`: язык (`environment.i18n.lang`), серверное время (`ysdk.serverTime()` → смещение к `ServerTimeUtc`, офлайн-доход и таймеры теперь по серверному времени), rewarded, interstitial, Game Ready (один раз за сессию), лидерборд, покупки; `GameBootstrap.CreatePlatform` создаёт его в WebGL-билде, в редакторе — `EditorPlatformService`;
  - сейвы: запись в localStorage сразу и в облако (`player.setData`) не чаще раза в 8 с (лимит платформы 100 запросов / 5 мин), принудительно — при скрытии вкладки и выходе; загрузка берёт более свежий из облака и локального (`Save/SaveSelector` по `SavedAtUtcTicks`); облако недоступно (нет авторизации, ошибка) — работает локальный;
  - `Core/PauseController`: реклама (`game_api_pause`) и скрытая вкладка → `TickManager.IsPaused` + `GameEvents.GamePaused` (звук уже слушает);
  - `Platform/InterstitialService`: не чаще 90 с, не в первые 3 мин сессии, флаг `AdsDisabled` (под «Без рекламы»); показывается при переходе в новый город (после сохранения), игра продолжается после закрытия рекламы;
  - `Platform/LeaderboardService`: «Самая богатая сеть» = `PrestigeState.LifetimeEarned`, раз в минуту и при переходе, только если вырос; id лидерборда `richest`;
  - покупки: `IPlatformService.GetPurchases/ConsumePurchase`; пак купюр выдаётся → сохраняется → только потом подтверждается; `Shop/PurchaseRecovery` при старте выдаёт неподтверждённые паки (требование модерации);
  - `GameScreen.Start` → `GameReady()` после сборки сцены.
- Решения:
  - счёт лидерборда — целое число, обрезанное до 9·10¹⁵ (предел точности JS); выше этого счёт перестанет расти — при необходимости заменить на логарифмическую шкалу в `LeaderboardService.ToScore`;
  - `setScore` работает только для авторизованных игроков, анонимным он молча пропускается; окно авторизации не добавлялось;
  - pause по потере фокуса окна Unity не используется (риск «залипшей» паузы) — только `game_api_pause` и `visibilitychange`.
- Известные проблемы / TODO:
  - **в консоли Яндекса**: создать лидерборд с именем `richest` и товары `bills_small`, `bills_medium`, `bills_large` (id из `BillShop.asset`); включить Payments;
  - собрать WebGL (шаблон YandexGames), загрузить архив черновиком, проверить: реклама, перезагрузка, второе устройство, язык, пауза, покупка;
  - выполнить ShaurmaTycoon/Build Game UI (карточка «Стартовый набор» в начале вкладки «Магазин»);
  - после покупки пака облако может отстать от локального сейва до 8 с — если игра закроется до отправки, на другом устройстве купюры не появятся (локально они сохранены);
  - sticky-баннер не делался (по желанию).

#### Стартовый набор (этап 09, 2026-10-05; ждёт Build Game UI и проверки в Unity)
- Проверено: runtime и editor компилируются без ошибок и warnings; новые StarterPackTests 8/8, BillShopTests 12/12, ключи строк добавлены в `StringsFileTests.RequiredKeys`. UI в Unity не проверялся.
- Сделано:
  - `Shop/StarterPack` (+ `StarterPackConfig` в `BillShop.asset`): 50 купюр + монеты на стоимость 50 купюр по курсу игры (не меньше 10 минут дохода), цена 99 ян, покупка один раз навсегда; постоянный товар — платформе не «подтверждается», факт покупки в `SaveData.StarterPackBought`;
  - выдача неподтверждённого набора при старте: `PurchaseRecovery.Run` теперь принимает общий обработчик (паки купюр + набор);
  - `UI/StarterPackCard` и сборка в `GameUiBuilder`: золотая рамка, красная пульсирующая лента «Супер-выгода!», две плитки наград (купюры + монеты, сумма монет живая), «Только один раз!», большая зелёная пульсирующая кнопка цены; после покупки карточка исчезает, монеты летят в HUD.
- Решения: монеты = ≈ 50 купюр (выбор пользователя); процент выгоды на карточке не показываем — по курсу игры набор стоит ≈ 83 ян против 99 ян цены.
- Известные проблемы / TODO: выполнить ShaurmaTycoon/Build Game UI; товар `starter_pack` создать в консоли Яндекса (постоянный, цена 99).

#### «Без рекламы» и «Постоянный ×2» (этап 09, 2026-10-05; ждёт Build Game UI и проверки в Unity)
- Проверено: runtime, editor и тесты компилируются без ошибок; новые PermanentProductsTests 5/5, StarterPackTests 8/8, BillShopTests и YandexLogicTests зелёные; ключи строк проверены скриптом. UI в Unity не проверялся.
- Сделано:
  - `Shop/PermanentProducts` (участник сейва, `SaveData.NoAdsBought` / `IncomeX2Bought`): покупка один раз, постоянные (не расходуемые) товары, выдача неподтверждённых при старте через `PurchaseRecovery`;
  - «Без рекламы»: `InterstitialService.AdsDisabled` — межстраничная реклама не показывается; rewarded (купюры, офлайн ×2) остаётся по желанию игрока;
  - «Постоянный ×2»: множитель ×2 ко всему доходу вместе с бонусом репутации (`GameSceneInstaller.ApplyPrestigeModifiers`); применяется до расчёта офлайн-дохода, поэтому действует и на него и на курс купюр;
  - цены и id — в `BillShop.asset` (`No Ads`, `Income X2`): `no_ads` — 149, `income_x2` — 299 (цены выбраны без согласования, править в конфиге);
  - UI: секция «Навсегда» с двумя строками во вкладке «Магазин» (после стартового набора), после покупки кнопка «Куплено».
  - иконки товаров (512×512) лежат в `Sprites/Shop/` (`bills_small/medium/large`, `starter_pack`, `no_ads`, `income_x2`), назначены в `BillShop.asset` полем Icon; строки магазина показывают их вместо цветной плашки (138×138), карточка набора — наклонённой иконкой слева.
- Известные проблемы / TODO: выполнить ShaurmaTycoon/Build Game UI; создать товары `no_ads` и `income_x2` в консоли (постоянные); множитель ×2 не показан в HUD.

## Важные решения (архитектура)
- Unity 2022.3.62f1, 2D, URP 2D, Color Space Gamma, старый Input Manager.
- Сборки: `ShaurmaTycoon` (runtime), `ShaurmaTycoon.Editor`, `ShaurmaTycoon.Tests.EditMode` (Assets/_Game/Tests/EditMode). Namespace `ShaurmaTycoon.<Модуль>`, для редакторского кода `ShaurmaTycoon.EditorTools`.
- Сервисы регистрируются в `GameBootstrap` через `Services.Register<T>`, единственный Update в игре — в `TickManager`.
- Сервисы Boot: `TickManager` (+ `ApplicationEvents`), `MusicService` (+ единственный `AudioListener` на объекте «Audio»), `GameSettings`, `Wallet`, `BillWallet`, `TweenRunner`, `PulseDriver`, `IPlatformService`, `Loc`, `SaveService`, `PrestigeState`. Сервисы сцены Game (`GameSceneInstaller`): `BalanceConfig`, `CityConfig` (текущий), `CityCatalog`, `StationController`, `ManagerController`, `UpgradeService`, `ReputationShop`, `BillShop`, `BillPurchases`, `AdBillService`, `AudioService`, `CustomerConfig`, `OfflineIncomeService`, `PrestigeController`; снимаются через `Services.Unregister(instance)`. Вью связываются в `GameScreen.Start`.
- Все тексты UI — ключи из `Configs/Localization/strings.txt` (TSV: key, ru, en); новые ключи добавлять туда и в `StringsFileTests.RequiredKeys`.
- Тексты, которые меняются в Tick, выводить через `NumberText` + `TMP_Text.SetNumberText`, шаблоны — `Loc.GetTemplate(key)` (без string.Format).
- Новая система с состоянием → реализовать `ISaveParticipant` и зарегистрировать в `SaveService`; новые поля `SaveData` — со значением по умолчанию, при несовместимых изменениях повышать `SaveData.CurrentVersion` и добавлять шаг в `SaveMigrator`.
- Вью сцены Game, подписанная на сервис Boot (`Wallet`, `BillWallet`, `Loc`, `PrestigeState`, `GameSettings`), реализует `ISceneBinding` и вызывает `SceneBindings.Track(this)` в `Bind` — `OnDestroy` у ни разу не активных объектов не вызывается.
- Новый город = новый `CityConfig` (свои станции и улучшения) в `Configs/Cities/CityCatalog.asset`; переход между городами перезагружает сцену Game.

## TODO(YANDEX)
- Консоль Яндекса: лидерборд `richest`, товары паков купюр, включить Payments (см. этап 09).
- В консоли Яндекса (постоянные товары): `starter_pack` 99, `no_ads` 149, `income_x2` 299; цены из каталога платформы вместо `DisplayPrice`.
- Проверка на черновике: реклама, облачный сейв между устройствами, язык, пауза, неподтверждённые покупки.
- WebGL-шаблон на весь экран: проверить в билде (шаблон YandexGames из плагина уже подключён).
