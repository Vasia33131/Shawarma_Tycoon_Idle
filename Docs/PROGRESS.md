# PROGRESS

## Текущий этап: 12 — Оптимизация и релиз (код готов; ждёт WebGL-билда `ShaurmaTycoon/Release/Build Yandex (WebGL)`, настройки консоли и прогона на черновике). V1–V7 и этапы 09–11 ждут проверки в Unity / на черновике

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

### Этап 10 — Удержание (код готов, 2026-10-05; ждёт Build Game UI и проверки в Unity)
- Промт: `Docs/Prompts/Stage_10_Retention.md`. Пользователь сказал «выполни» — решения ниже приняты без отдельного согласования.
- Проверено: runtime, editor и тесты компилируются без ошибок и warnings (вне Unity). Тесты вне Unity: 206 зелёных, 35 падают только из-за среды (netstandard 2.1 / UnityEngine вне редактора). Новые RetentionTests 30/30; StringsFileTests (новые ключи) зелёные. В Unity не запускалось.
- Сделано:
  - `Retention/` (чистый C#):
    - `DailyRewardCalendar`: 7 дней, одна награда за сутки UTC, пропуск дня не сбрасывает, после 7-го — снова 1-й; часы назад не дают повторной награды, сейв «из будущего» больше чем на сутки прижимается к сегодня;
    - `DailyQuests`: 3 задания разных типов из пула на сутки UTC (порядок — от номера дня), невыполнимые (все менеджеры наняты / улучшения куплены) не выдаются; прогресс ограничен целью, награда забирается один раз;
    - `IncomeBoost`: +4 мин за рекламу, копится до 4 ч, тратится только во время игры (на паузе и офлайн — нет);
    - `RewardMath`, `RewardGranter` (монеты = минуты дохода без буста, но не меньше 20 × масштаб города за минуту; купюры; бонусная репутация);
  - сервисы — участники сейва: `DailyRewardService` (скин «Золотой ларёк» на 7-й день, потом вместо него 10 купюр), `DailyQuestService` (смена суток раз в секунду), `BoostService`, `GiftBoxService` (раз в 10 мин, 5 мин дохода, только после досмотра; часы назад прижимаются к now), `QuestTracker` (уровни станций — и за монеты, и за купюры, найм, улучшения, буст, подарок);
  - `RetentionConfig` (SO, `Configs/Retention.asset` создаёт Build Game UI) — все числа этапа;
  - `SaveData.Retention` (`RetentionSave`, `QuestSave`), нормализация в `SaveMigrator`; `PrestigeSave.ReputationBonus`;
  - `PrestigeState.AddBonusReputation` / `ReputationFromCities`: репутация за задания даёт +2% и тратится в магазине, но не уменьшает награду за переезд (`PrestigeController.ForecastGain` считает от `ReputationFromCities`);
  - `GameSceneInstaller`: буст — множитель к доходу вместе с репутацией и «Постоянным ×2»; создаётся после расчёта офлайн-дохода (на него не действует); курс купюр, мешки монет и стартовый набор считают доход без буста;
  - UI: кнопка «×2 доход» в HUD под шестерёнкой (таймер h:mm:ss, пульс), на экране ларька слева — календарь и задания (бейдж «!»), справа — подарок (пульсирует, когда готов, иначе таймер); окна `DailyRewardPopup` (7 клеток, галочки, «Забрать»), `QuestsPopup` (прогресс-полосы, награда, «Новые задания через …»), `AdOfferPopup` (реклама только по кнопке игрока); календарь открывается сам при входе, если награда доступна, — после окна офлайн-дохода (`OfflineIncomePopup.Closed`);
  - меню: `Save/Shift Retention Day −1 d` (сдвиг календаря, заданий и подарка на сутки назад — проверка «подмены даты»), `Debug/Reset Gift Timer`, `Debug/Complete Daily Quests`;
  - строки `retention.*`, `quest.*`, `boost.*`, `gift.*` ru/en.
- Файлы:
  - `Scripts/Retention/`: `RewardKind`, `RewardDefinition`, `RewardGrant`, `RewardMath`, `RewardGranter`, `QuestType`, `QuestTemplate`, `QuestState`, `DailyRewardCalendar`, `DailyRewardService`, `DailyQuests`, `DailyQuestService`, `QuestTracker`, `IncomeBoost`, `BoostService`, `GiftBoxService`, `RetentionConfig`
  - `Scripts/Save/`: `RetentionSave.cs`, `QuestSave.cs`
  - `Scripts/UI/`: `RetentionHud`, `DailyRewardPopup`, `DailyRewardTileView`, `QuestsPopup`, `QuestRowView`, `AdOfferPopup`, `RewardIconView`, `TimerText`
  - `Tests/EditMode/RetentionTests.cs`
  - изменены: `SaveData`, `SaveMigrator`, `PrestigeSave`, `PrestigeState`, `PrestigeSaveParticipant`, `PrestigeController`, `BillWallet` (`BillSource.Reward`), `GameSceneInstaller`, `GameScreen`, `OfflineIncomePopup`, `ContentSetup`, `GameUiBuilder`, `SaveMenu`, `DebugMenu`, `strings.txt`, `StringsFileTests`
- Решения (без согласования, править в `Retention.asset`):
  - сутки — по UTC серверного времени (как купюры за рекламу 08f), т. е. в 03:00 по Москве;
  - календарь: 10 мин дохода / 2 купюры / 30 мин / 3 купюры / 60 мин / 5 купюр / скин; награда без рекламы;
  - задания (пул): 50 уровней → 20 мин дохода; 1 менеджер → 2 репутации; 3 улучшения → 30 мин; буст → 1 репутация; 2 подарка → 15 мин;
  - монеты из наград не засчитываются как заработок города (как мешки монет 08d);
  - скин — цвет ларька (золотой вместо цвета города), выключить нельзя; коллекция скинов — LiveOps.
- Известные проблемы / TODO:
  - выполнить ShaurmaTycoon/Build Game UI (создаст `Retention.asset`, кнопки и окна);
  - незабранные выполненные задания сгорают в полночь UTC вместе с набором дня;
  - прогресс заданий сохраняется автосейвом (раз в 10 с), награды — сразу.

### Этап 11 — Онбординг и баланс (код готов, 2026-10-05; ждёт Build Game UI и проверки в Unity)
- Промт: `Docs/Prompts/Stage_11_Onboarding.md`.
- Проверено: runtime, editor и тесты компилируются без ошибок и warnings (вне Unity). Тесты вне Unity: 385 зелёных, 14 падают только из-за среды (UnityEngine.UI/TestRunner, JsonUtility, AssetDatabase, `Is.InRange` из другой версии NUnit). Новые TutorialTests 10/10, BalanceSimulatorTests 8/9 (`City1Assets_MeetStage11Targets` читает ассеты через AssetDatabase — только в Unity). В Unity не запускалось.
- Сделано:
  - `Balance/BalanceSimulator` — прогон баланса на настоящих моделях (`StationController`, `ManagerController`, `UpgradeService`): виртуальный игрок тапает по клиенту и жадно покупает самое быстро окупаемое (уровень с учётом вех, менеджер, улучшение), копит на лучшую покупку. Профили: «обычный» (решает раз в 10 с, тап раз в 1.5 с) и «внимательный» (1 с / 0.5 с). Без рекламы, купюр, офлайна и репутации;
  - меню `ShaurmaTycoon/Balance/Simulate Cities` — таблицы по всем городам каталога в консоль и буфер обмена;
  - онбординг (GDD §9): `Onboarding/TutorialService` — шаги «тап → уровень → вторая станция → менеджер»; шаг засчитывается по состоянию игры (сделал не по подсказке — тоже засчитано, старый сейв с прогрессом проходит туториал сразу); шаг в `SaveData.TutorialStep`, не сбрасывается при переезде;
  - `UI/TutorialHand`: рука (из примитивов, pivot — кончик пальца) с кольцом-«тапом» и облачком-подсказкой; появляется, только когда на шаг хватает монет (иначе не висит на экране); нужна другая вкладка — на кнопку вкладки; скрыта, пока открыто любое окно; без raycastTarget; появляется через 0.6 с после смены шага;
  - календарь наград новичку открывается после туториала, а не поверх него;
  - `Core/DebugWindow` (только редактор и Development build, `#if`): кнопка «DEV» у левого края или F2 — +1K/+1M/+1B (× масштаб города, засчитываются как заработок), +10 мин дохода, время ×1/×10 (`TickManager.TimeScale`), пропуск туториала, сброс сейва (двойное нажатие; `SaveService.Wipe` пишет пустой сейв и отключает запись; в редакторе Play останавливается, в билде — «перезагрузи страницу»);
  - строки `tutorial.*` ru/en.
- Баланс (выбор пользователя из трёх вариантов): с исходными конфигами (рост 1.07, порог 1e9) город 1 проходился за ~11 мин, все станции открывались за 6 мин. Применено:
  - рост цены: станция 1 — 1.07, станции 2–6 — 1.15 (во всех 5 городах);
  - цены всех улучшений ×10;
  - пороги переезда ×10 (город 1 — 1e10 … город 5 — 1e14), делитель репутации 1e6 → 1e7 (за переезд по-прежнему ~31 репутация);
  - GDD §5 и §7 обновлены.
- Баланс-таблица, город 1 (`Simulate Cities`, без рекламы/офлайна/купюр):

  | # | Станция | Открыта (обычный) | Менеджер (обычный) | Открыта (внимательный) | Менеджер (внимательный) |
  |---|---|---|---|---|---|
  | 1 | Шаурма классик | 0:00 | 2:00 | 0:00 | 1:21 |
  | 2 | Сырный лаваш | 0:40 | 4:11 | 0:24 | 3:17 |
  | 3 | Картошка фри | 1:10 | 7:01 | 0:44 | 6:14 |
  | 4 | Напитки / Айран | 1:50 | 14:42 | 1:15 | 13:48 |
  | 5 | Шаурма XL | 2:51 | 24:25 | 2:16 | 23:26 |
  | 6 | Гриль-тарелка | 6:31 | — | 5:47 | — |

  - порог переезда 1e10: **2:13:50** (обычный), 2:12:51 (внимательный); покупок за первую минуту — 9 / 52; улучшений куплено 11 из 20;
  - доход/с (обычный): 1 мин — 53, 5 мин — 25.1K, 10 мин — 125K, 30 мин — 426K, 1 ч — 1.31M, 2 ч — 2.68M;
  - города 2–5 — масштабированные копии города 1, со старта без репутации дают то же время (~2:14); с репутацией (+62% за ~31) — быстрее.
- Решения:
  - туториал ведёт по состоянию игры, а не по жёстким кликам — нельзя «застрять» или пройти не тем путём;
  - отладочное окно на IMGUI: без сборки UI и префабов, в релиз не попадает; аллокации OnGUI допустимы (только для разработки).
- Известные проблемы / TODO:
  - выполнить ShaurmaTycoon/Build Game UI (рука-подсказка);
  - плато ~0:51–1:55 (обычный игрок): после станций и первых улучшений игрок копит ~1 ч на «Все станции ×2» (5B); симулятор копит строго, живой игрок будет докупать уровни, плюс буст/подарки/офлайн — проверить ощущение в Unity, при необходимости удешевить улучшения 12–13 города 1;
  - менеджер 6-й станции (1.24B) в прогоне не нанимается до порога — его окупаемость хуже улучшений;
  - у существующих сейвов с репутацией новый делитель 1e7 уменьшит будущую награду за переезд (игра не выпущена — некритично);
  - «Рука» GDD — первые 60 с; фактически последний шаг (менеджер, 4000 монет) наступает на ~2-й минуте.
- Файлы:
  - `Scripts/Balance/`: `BalanceSimulator`, `BalanceSimulationSettings`, `BalanceReport`, `BalancePurchase`, `BalancePurchaseKind`, `BalanceSample`, `BalanceReportFormatter`
  - `Scripts/Onboarding/`: `TutorialStep`, `TutorialService`
  - `Scripts/UI/TutorialHand.cs`, `Scripts/Core/DebugWindow.cs`, `Scripts/Editor/BalanceMenu.cs`
  - `Tests/EditMode/TutorialTests.cs`, `BalanceSimulatorTests.cs`
  - изменены: `SaveData`, `SaveService`, `GameSceneInstaller`, `GameBootstrap`, `GameScreen`, `StationRowView`, `StationsPanel`, `ManagerRowView`, `ManagersPanel`, `TabBar`, `GameUiBuilder`, `DebugMenu`, `BalanceConfig`, `CityConfig` (значения по умолчанию), `strings.txt`, `StringsFileTests`, ассеты `Balance`, `Cities/*`, `Stations/*`, `Upgrades/*`, `Docs/GDD.md`

### Этап V1 — Вечерняя палитра и мягкий UI-кит (код готов, 2026-10-05; ждёт Generate UI Sprites + Build Game UI и проверки в Unity)
- Промт: `Docs/Prompts/Stage_V1_PaletteUiKit.md`, арт-дирекция: `Docs/Visual/ART_DIRECTION.md`.
- Проверено: runtime, editor и EditMode-тесты компилируются вне Unity без ошибок и warnings (временный проект с теми же сборками Unity). Тесты не запускались, в Unity не проверялось.
- Сделано:
  - `UI/Theme/UiTheme` (SO, `Configs/UiTheme.asset` создаёт `ContentSetup.EnsureTheme`) — вся палитра «уютного вечера», цвета процедурной сцены до V3, тайминги нажатия, пульса, свечения, попапов, вкладок. `GameUiBuilder` берёт цвета только из темы (нет ни одного `new Color(`), встроенные `UISprite.psd`/`Knob.psd` больше не используются;
  - `Editor/ProceduralSprites` — меню `ShaurmaTycoon/Art/Generate UI Sprites`: `Sprites/UI/Generated/` — `rounded_fill`, `rounded_outline`, `rounded_shade`, `rounded_glow`, `card_fill` (нижняя полоса RowShade), `button_fill` / `button_fill_pressed` (бортик 10 / 4 px), `circle_fill`, `circle_outline`, `glow_soft`, `pill_fill`, `edge_shadow`; белые/серые, края сглажены по SDF, 9-slice border задаётся в импортёре. Build Game UI дорисовывает только недостающие;
  - `UI/SoftButton` (наследник `Button`) — объёмная кнопка: при нажатии спрайт меняется на «нажатый», подписи опускаются на 6 px (отпускание с отскоком); недоступная — бежевый слой `DisabledOverlay` и приглушённые подписи. Цвет по-прежнему в `targetGraphic.color` — `TabBar`, `SettingsPopup`, `BillShopRowView`, `RetentionHud` перекрашивают кнопки как раньше;
  - стили кнопок: Primary (купить, нанять, забрать, настройки вкл.), Secondary (открыть станцию, улучшения, переезд), Neutral (режим покупки, закрыть, отмена, вкладки, купюры в HUD), Reward (×2 за рекламу, буст), Bills, Reputation;
  - карточки строк и панели попапов: `card_fill` + мягкая обводка (альфа 0.35); прогресс-бары — капсулы; кружки иконок, монеты, круглые кнопки — с обводкой; HUD и вкладки — `HudBackground` с мягкой тенью-градиентом у края;
  - `UI/PopupAnimator` — все 7 попапов появляются (затемнение fade 0.2 с, панель из 0.92 и −40 px за 0.25 с с лёгким отскоком) и скрываются (0.12 с, затем корень выключается). Пока идёт анимация, панель не ловит нажатия (`CanvasGroup.blocksRaycasts` панели, затемнение при этом закрывает игру). `IsOpen` = false сразу при `Hide`; `Show` во время скрытия разворачивает анимацию. Сцена, собранная без аниматора, получает его с настройками по умолчанию (`PopupAnimator.For`);
  - `UI/TabBar` — плавная смена цвета (0.15 с), активная вкладка приподнимается на 10 px (двигается её «тело» `Body`, сама кнопка стоит в layout);
  - `UI/AvailableGlow` — мягкое свечение вокруг доступной кнопки (альфа 0.5 ↔ 0.8, период 2 с, через `CanvasRenderer.SetAlpha` — без перестройки меша) вместо пульса у кнопок строк, «Забрать» в календаре, ×2 в офлайн-окне, подтверждения рекламы, стартового набора;
  - `UI/IUiAnimated`: `PulseDriver` теперь тикает любые UI-анимации (пульс, свечение, вкладки); `PulseEffect` по умолчанию amplitude 0.04, speed 2.5; `ButtonFeedback` — настраиваемые масштаб и тайминги (0.94 / 0.06 / 0.18 с; у SoftButton 0.97);
  - TMP-материал `Fonts/Rubik SDF - Outline.mat` (обводка `Outline`, 0.18) — цифры HUD, «+X», подписи на цветных кнопках и вкладках;
  - `StationColors` — та же гамма, приглушённее; цвета рантайм-компонентов (вкладки, настройки, буст, клетки календаря, значки наград, магазин, карта, лампы ларька) задаются сборщиком из темы.
- Решения:
  - бортик кнопки — не отдельный тёмный слой, а часть спрайта (72% яркости лица): так старый код перекраски кнопок работает без изменений. Поэтому токены «… → низ» из ART_DIRECTION реализованы одним множителем `UiTheme.ButtonLipShade`;
  - пульс оставлен у подарка, буста, переезда, следующего города на карте и полоски «Тапни!» ручных станций (подсказка до найма менеджеров) — критерий «пульсирует не больше одного» выполнен не строго: подарок, буст и полоски «Тапни!» могут «дышать» одновременно, но амплитуда и скорость вдвое ниже прежних;
  - ленточка «Супер-выгода!» стартового набора больше не пульсирует (поле `_ribbonPulse` пустое, код это допускает);
  - `rounded_shade` сгенерирован, но пока не используется (пригодится в V3/V5).
- Известные проблемы / TODO:
  - выполнить `ShaurmaTycoon/Art/Generate UI Sprites`, затем `ShaurmaTycoon/Build Game UI`;
  - проверить в Unity читаемость светлого текста с обводкой на кнопке Reward (жёлтая) и вид подписей недоступных кнопок;
  - «Rubik SDF - Outline» — копия материала динамического шрифта; если после очистки динамического атласа обводка пропадёт, пересобрать UI (материал обновляет ссылку на атлас при каждой сборке).
- Файлы:
  - созданы: `Scripts/UI/Theme/UiTheme.cs`, `Scripts/UI/SoftButton.cs`, `Scripts/UI/PopupAnimator.cs`, `Scripts/UI/AvailableGlow.cs`, `Scripts/UI/IUiAnimated.cs`, `Scripts/Editor/ProceduralSprites.cs`
  - изменены: `GameUiBuilder`, `ContentSetup`, `TabBar`, `ButtonFeedback`, `PulseEffect`, `PulseDriver`, `StationColors`, `OfflineIncomePopup`, `DailyRewardPopup`, `QuestsPopup`, `SettingsPopup`, `PrestigeConfirmPopup`, `AdOfferPopup`, `BillOfferPopup`, `Docs/GDD.md` (§10)
  - появятся в Unity: `Configs/UiTheme.asset`, `Fonts/Rubik SDF - Outline.mat`, `Sprites/UI/Generated/*.png`

### Этап V2 — Иконки интерфейса и станций (код и ассеты готовы, 2026-10-05; ждёт Assign Icons + Build Game UI и проверки в Unity)
- Промт: `Docs/Prompts/Stage_V2_Icons.md`.
- Проверено: runtime, editor и EditMode-тесты компилируются вне Unity без ошибок и warnings. Тесты не запускались, в Unity не проверялось.
- Ассеты:
  - пакет A: 21 картинка лежала в корне `Assets/` с именами `image-45641…png` — опознаны по содержимому, почищены и разложены в `Sprites/UI/Icons/` под именами из ASSET_PROMPTS (обрезка полей до 6%, квадрат 512×512, исходники удалены). У `icon_gift` и `icon_ad` ChatGPT нарисовал «шахматку» вместо прозрачности — убрана заливкой от краёв с полупрозрачной кромкой; у `icon_reputation` и `icon_zen` цветная бахрома по краю перекрашена в кремовое свечение;
  - пакет B: 30 картинок из `Assets/new/` опознаны по содержимому и разложены в `Sprites/Stations/City01…City05/` под именами ассетов станций (512×512, поля 6%); у чурчхелы убрана полупрозрачная «дымка» недоудалённого фона; папка `new` удалена;
- Сделано:
  - `UI/Theme/UiIcons` (SO, `Configs/UiIcons.asset`) и `Editor/ArtAssigner` — меню `ShaurmaTycoon/Art/Assign Icons` (вызывается и из Build Game UI): UI-иконки по именам файлов, еда станций → `StationConfig._icon` из `Sprites/Stations/<City0X>/<имя ассета>.png`; отчёт «назначено N/M» и список недостающих одним предупреждением;
  - `SpriteImportSettings`: `Sprites/UI/Icons/` и `Sprites/Stations/` — max 256, сжатие; остальное — 512; версия постпроцессора 2 (Unity переимпортирует текстуры по новым правилам);
  - `UI/StationIcons` — значок станции в строках станций, менеджеров, улучшений: еда поверх светлой подложки цвета станции, без картинки — прежний кружок с номером. Менеджеры — колпак повара в углу; улучшения — множитель бейджем, «все станции» — `tab_upgrades`. Закрытая станция — светлый «призрак» еды и замок, при открытии значок «выпрыгивает» (punch);
  - вкладки — иконка 72 px над подписью, у активной иконка увеличивается до 1.12 с отскоком (`TabBar._icons`);
  - HUD: монета `icon_coin` (и летящие монеты), строка дохода с `icon_income` (HudView прячет строку целиком — `_incomeRoot`), купюры `icon_bill`, репутация `icon_reputation`, шестерёнка `icon_settings`, буст с `icon_boost`;
  - календарь, задания — светлый кружок с `icon_calendar` / `icon_quests`; подарок — `icon_gift`;
  - rewarded-кнопки (×2 в офлайн-окне, подтверждение рекламы) — `icon_ad`; «Закрыть» в настройках, календаре и заданиях — `icon_close`; офлайн-окно — `icon_offline`; «получено» в календаре — `icon_check`; значки наград календаря и заданий — монета / купюры / звезда (`RewardIconView`); значок купюр в кнопках и окне «Не хватает монет» — `icon_bill`; рука туториала — `tutorial_hand` (pivot в кончике пальца, анимация прежняя);
  - каждое место без картинки строит прежнюю заглушку из примитивов;
  - тесты `StationIconsTests`: логика значка станции, имена файлов в `Sprites/Stations` соответствуют ассетам станций, имена в `Sprites/UI/Icons` — из списка пакета A.
- Решения:
  - иконки только кладёт сборщик (статично), кроме еды станций — её строки берут из `StationConfig.Icon` в игре: ассет станции — единственный источник, новый город добавляется без пересборки UI;
  - «силуэт» закрытой станции — полупрозрачная светлая копия картинки, а не залитый силуэт (для заливки нужен свой шейдер);
  - вместо отдельного крестика в углу попапов `icon_close` поставлен на кнопку «Закрыть»: у попапов одно поле `_closeButton`, лишняя кнопка — лишняя логика.
- Известные проблемы / TODO:
  - выполнить `Art/Assign Icons` (ожидается «UI: 21/21, stations: 30/30») + `Build Game UI`;
  - проверить в Unity читаемость мелких иконок (вкладки 72 px, иконка в купюрах HUD).
- Файлы:
  - созданы: `Scripts/UI/Theme/UiIcons.cs`, `Scripts/UI/StationIcons.cs`, `Scripts/Editor/ArtAssigner.cs`, `Tests/EditMode/StationIconsTests.cs`, `Sprites/UI/Icons/*.png` (21)
  - изменены: `GameUiBuilder`, `SpriteImportSettings`, `StationRowView`, `ManagerRowView`, `UpgradeRowView`, `RewardIconView`, `HudView`, `TabBar`, `ShaurmaTycoon.Tests.EditMode.asmdef` (+ `Unity.TextMeshPro`: тест вызывает `StationIcons.Apply` с параметром `TMP_Text`)

### Этап V3 — Вечерняя сцена города и нарисованный ларёк (код и ассеты готовы, 2026-10-05; ждёт Assign Icons + Build Game UI и проверки в Unity)
- Промт: `Docs/Prompts/Stage_V3_CityScene.md`.
- Проверено: runtime, editor и EditMode-тесты компилируются вне Unity без ошибок и warnings; композиция сцены (небо, слои, ларёк, украшения) проверена моделированием кадра 1080×520 и 960×700 по тем же координатам. Тесты не запускались, в Unity не проверялось.
- Ассеты (24 картинки из корня `Assets/`, опознаны по содержимому; исходники удалены):
  - пакет C → `Sprites/Scene/City01…05/bg_far.png`, `bg_near.png`, `Sprites/Scene/Common/cloud_01..03.png`, `moon.png`. Фоны пришли квадратными (а не 3:2): обрезаны сверху по содержимому и снизу по линии улицы (плоская полоса «земли» срезана, оставлено 6 px — без щели над тротуаром). У bg_far родного города, bg_far Сочи и bg_near Москвы ChatGPT нарисовал «шахматку» вместо неба — удалена; в Сочи шахматка смешалась с закатом у горизонта — полоса сглажена в мягкое свечение, тающее кверху (солнце, маяк и склон горы сохранены маской цвета);
  - пакет D → `Sprites/Kiosk/` (10 из 11): `kiosk_body`, `kiosk_counter`, `kiosk_awning` (шахматка удалена), `kiosk_sign`, `kiosk_spit`, `decor_garland`, `decor_menu_board`, `decor_table_umbrella`, `decor_sign_lights`, `decor_flowers`, уменьшены до 512; `decor_flags` пришёл позже (`Assets/image-4565154.png`, шахматка удалена) — две гирлянды флажков по бокам ларька (от навеса к меню-доске и к столику), тонируются цветом города;
  - все размеры кратны 4 (сжатие текстур в WebGL); импорт: `Sprites/Scene/City0X` — до 2048, остальное — 512, со сжатием (`SpriteImportSettings`, версия 3).
- Сделано:
  - `CityConfig`: `_skyTop`, `_skyMiddle`, `_skyHorizon` (бывший `_skyColor`, `FormerlySerializedAs`), `_bgFar`, `_bgNear`, `_showMoon`, `_showStars`; `SkyColor` = горизонт (старый код не менялся);
  - `ArtAssigner` (меню `Art/Assign Icons`) назначает фоны городам по номеру в имени ассета (`City_03_…` → `Scene/City03`) и один раз — вечерние цвета неба и ларька из ART_DIRECTION (Родной город `#D9604A`, Казань `#2F8F6B`, Сочи `#2E9FB5`, Питер `#3F5C9E` + луна, Москва `#B23A48` + луна и звёзды); пока у города задан верх неба, цвета не перезаписываются; в отчёте — слои и части ларька;
  - `UI/GradientImage` — небо: свой меш из трёх цветов, без текстуры, перестраивается только при смене цветов;
  - `Customers/CitySceneView`: небо, луна с «дышащим» ореолом, слои города по ширине зоны на линии тротуара (`AspectRatioFitter`), лёгкое «дыхание» слоёв по X (±8 / ±14 px, период 24 с), плавное проявление слоёв 0.6 с при загрузке сцены; `GameScreen` применяет город и тикает сцену;
  - нарисованный ларёк (`GameUiBuilder.BuildKioskArt`, если есть `kiosk_body`): корпус 250 px в цвете города → вертел и продавец в окне (обрезка по окну) → прилавок → навес в цвете города → вывеска «ШАУРМА / SHAWARMA» (`kiosk.sign`) → 6 лампочек под навесом с ореолами; тёплое пятно света на тротуаре; ларёк стоит на тротуаре (−36 px от линии), с вывеской помещается в портретную зону 520;
  - украшения по менеджерам — картинки пакета D в прежнем порядке и с прежней анимацией: гирлянда на навесе, меню-доска и цветы слева, столик под зонтом справа, рамка из лампочек вокруг вывески, флажки;
  - `KioskView`: нарисованный вертел «вращается» покачиванием ширины и яркости (период 1.6 с), ореолы горящих ламп мягко «дышат» вразнобой;
  - без `kiosk_body` строится прежний процедурный ларёк (код сохранён).
- Решения:
  - кроссфейд между городами не нужен: переезд перезагружает сцену Game, поэтому вместо него — плавное проявление сцены при загрузке;
  - слои — по ширине зоны (не cover): в портрете силуэт города по краям, ларёк по центру, сверху небо; в ландшафте — больше неба;
  - навес и корпус тонируются цветом города; столик под зонтом — нет (тонировка перекрасила бы и деревянный столик);
  - облака из пакета C разложены, но анимируются на этапе V5; звёзды (флаг `_showStars`) — тоже V5;
  - виньетка по краям зоны не сделана: края и так затемняют тени шапки и вкладок (V1).
- Известные проблемы / TODO:
  - выполнить `Art/Assign Icons` (ожидается «city layers: 10/10, kiosk: 11/11») + `Build Game UI`;
  - продавец в окне — пока Kenney (новый — на этапе V4); проверить, как он смотрится в окне;
  - пройти все 5 городов: ближние слои Казани и Москвы высокие — проверить, что ларёк и подарок не теряются на их фоне.
- Файлы:
  - созданы: `Scripts/UI/GradientImage.cs`, `Scripts/Customers/CitySceneView.cs`, `Sprites/Scene/**`, `Sprites/Kiosk/**`
  - изменены: `CityConfig`, `KioskView`, `GameScreen`, `GameUiBuilder`, `ArtAssigner`, `SpriteImportSettings`, `strings.txt` (+ `kiosk.sign`), `StringsFileTests`

### Этап V4 — Клиенты, продавец и кот Лаваш (код и ассеты готовы, 2026-10-05; ждёт Assign Icons + Build Game UI и проверки в Unity)
- Промт: `Docs/Prompts/Stage_V4_Characters.md`.
- Проверено: runtime, editor и EditMode-тесты компилируются вне Unity без ошибок и warnings (временный проект с теми же сборками Unity). `CatStateTests` 9/9 зелёные (вне Unity, NUnit 3). В Unity не запускалось (проект был открыт в редакторе — batchmode не стартовал).
- Ассеты (пакет E, листы нарезаны, исходные листы удалены) → `Sprites/Characters/`:
  - `Customers/` — 10 персонажей × 2 позы (`<имя>.png`, `<имя>_happy.png`): student, babushka, office, courier, tourist, builder, girl_headphones, fisherman, hipster, schoolkid; общий холст 248×504, поэтому дети ниже взрослых без `HeightScale`;
  - `Seller/` — `seller_idle`, `seller_work`, `seller_happy` (344×504), туман вокруг продавца убран;
  - `Cat/` — `cat_sleep`, `cat_sit`, `cat_purr`, `cat_stretch` (240×256) и `cat_bed` (коробка с пледом, 256×192).
- Сделано:
  - `ArtAssigner` (`Art/Assign Icons`): новые клиенты и продавец → `Customers.asset` (Walk пустой, `FacesLeft`, своя реплика `customer.phrase.<имя>`), позы кота → `Configs/Cat.asset` (создаётся сам); без файлов — прежние Kenney;
  - `CustomerView`: без кадров ходьбы — вприпрыжку (подскок |sin|, наклон ±3° в такт шагу, сжатие при «приземлении»), стоя «дышит» со своей фазой; смотрит по ходу движения; тень под ногами сжимается в прыжке. Kenney по-прежнему листает кадры;
  - `CustomerQueue`: после оплаты клиент радуется (`Cheer` 0.8 с), с шансом 0.3 над ним всплывает сердечко; реплика — своя для персонажа (бабушка про укроп, курьер про 12 заказов…) или общая; раз в 2–3 мин клиент из очереди умиляется коту (радуется 1.5 с, над котом сердечко);
  - `KioskView`: продавец стоит / работает (во время заказа) / показывает «класс» (каждый 8-й заказ, найм менеджера, покупка улучшения — `GameScreen`), «дышит», смена позы — с подскоком; в окне уменьшен под новую картинку;
  - кот: `CatBrain` (чистый C#: сон 40–90 с → потягивается 1.5 с → сидит 10–20 с → сон; тап по спящему — мяу и садится, иначе мурчит 2.5 с, повторный тап продлевает не чаще раза в 0.5 с), `CatConfig` (позы и тайминги), `CatView` (во сне дышит и выпускает «z», сидя моргает, мурча покачивается, 3 сердечка на тап; анимации в реальном времени — «время ×10» их не ускоряет); `SoundId.CatPurr` / `CatMeow` (клипы — V6);
  - сердечко — процедурный спрайт `Generated/heart.png` (`ProceduralSprites`), цвет `Badge`;
  - `GameUiBuilder`: клиенты — высота `CharacterHeight` (230), ширина по пропорции холста, тело с опорой у ног (pivot снизу), тень `glow_soft` (`Outline`, альфа 0.25), пул из 6 сердечек на улице; кот на коробке слева от ларька (x = −400, левее меню-доски, в ландшафте тоже целиком в кадре), его «z» и 4 сердечка — в отдельном слое `Fx`, чтобы не качались вместе с телом; кот строится до улицы — уходящие клиенты проходят перед ним; кот передаётся очереди и `GameScreen` (привязка и тик);
  - строки `customer.phrase.*` ru/en (10 реплик), `StringsFileTests`.
- Решения:
  - анимация — только кодом (подскок, дыхание, наклон): покадровую ходьбу нейросеть для 10 персонажей одинаково не нарисует, а процедурная весит ноль байт;
  - коробка рисуется позади кота, кот лежит на пледе (низ кота на 0.42 высоты коробки): у коробки непрозрачный плед, поверх кота она закрыла бы спящего почти наполовину;
  - тап по коробке тоже гладит кота (у коробки `raycastTarget`, кнопка на корне);
  - нет `Cat.asset` или поз сна/сидения — кот не строится, очередь и экран работают без него;
  - монет за кота нет.
- Известные проблемы / TODO:
  - выполнить `Art/Assign Icons` (ожидается «customers: 10/10, seller: yes, cat: 5/5») + `Build Game UI`;
  - проверить в Unity: клиенты стоят ногами на тротуаре (если на холсте есть отступ под ногами — подвинуть корень), кот на пледе не «висит» и не проваливается (коэффициент 0.42 в `BuildCat`), «z» над котом не уходит под шапку;
  - движение и подскок клиентов идут в игровом времени (вместе со скоростью очереди), поэтому при «×10» ускоряются вместе с ходьбой; кот — в реальном.
- Файлы:
  - созданы: `Scripts/Customers/CatBrain.cs`, `CatConfig.cs`, `CatView.cs`, `Tests/EditMode/CatStateTests.cs`, `Sprites/Characters/**`
  - изменены: `CustomerSkin`, `CustomerConfig`, `CustomerView`, `CustomerQueue`, `KioskView`, `GameScreen`, `SoundId`, `GameUiBuilder`, `ArtAssigner`, `ProceduralSprites`, `ContentSetup`, `strings.txt`, `StringsFileTests`

### Этап V5 — Живой мир: облака, пар, птицы, машины, погода (код и ассеты готовы, 2026-10-05; ждёт Assign Icons + Build Game UI и проверки в Unity)
- Промт: `Docs/Prompts/Stage_V5_LivingWorld.md` (там же раздел «Решения»).
- Проверено: runtime, editor и EditMode-тесты компилируются вне Unity без ошибок и warnings (копии csproj Unity + новые файлы). `WeatherScheduleTests` 8/8 зелёные (вне Unity, NUnit 3, .NET Framework 4.8). В Unity не запускалось; Profiler не замерялся.
- Ассеты (пакет F, обрезаны по прозрачности, исходники из корня `Assets/` удалены) → `Sprites/Ambient/`: `car_01` 512×244, `car_02` 512×304, `car_03` 512×324, `trolleybus` 512×404, `scooter_courier` 408×512, `umbrella` 252×256, `bird` 256×248.
- Что живёт на экране (всё — один `Tick` в `AmbientWorldView` и один в `WeatherView`, пулы созданы `Build Game UI`):

| Что | Сколько Image | Когда |
|---|---|---|
| Облака (`cloud_01..03`), между небом и `bg_far` | 4 | всегда |
| Пар над вертелом (`glow_soft`) | 8 | всегда |
| Звёзды (`circle_fill`) | 25 | `_showStars`, ясно, эффекты вкл. |
| Светлячки (`glow_soft`) | 8 (Сочи 14) | ясно, эффекты вкл. |
| Далёкие птицы-«галочки» | 2×2 | раз в 30–60 с, ясно |
| Воробей (`bird`) | 1 | раз в 60–120 с, ясно; тап рядом — улетает |
| Транспорт + свет фар | 2 | раз в 15–40 с, эффекты вкл. |
| Дождь: затемнение, капли, лужи + круги, отскоки на навесе, зонтики | 1 + 50 + 6 + 6 + 3 + до 9 | дождь |
| Снег: снежинки, сугробы на навесе и тротуаре | 40 + 2 | снег (дек–фев, не Сочи) |

- Лимит Image: ясно ≈ 55 (облака, пар, звёзды, светлячки, птицы, воробей, машина); дождь ≈ 88, снег ≈ 58 — на время осадков звёзды, светлячки и птицы прячутся. Максимум ≤ 90.
- Сделано:
  - `Scripts/Ambient/`: `AmbientConfig` (SO, все числа; `Configs/Ambient.asset` создаёт `ContentSetup.EnsureAmbient` при `Assign Icons`/`Build Game UI`), `WeatherKind`, `WeatherSchedule` (чистый C#: UTC-слоты по 10 мин, хеш от слота и id города, осадки 2–4 мин внутри слота, снег только дек–фев и где `_snowAllowed`), `WeatherService` (`ITickable`, событие `WeatherChanged`, `Force` для DebugWindow), `VehicleSize`, `AmbientWorldView`, `WeatherView`;
  - `CityConfig`: `_rainChance`, `_snowAllowed`, `_vehicles`, `_fireflies` (Дом/Казань 0.20, Сочи 0.12 без снега и со светлячками, Питер 0.35, Москва 0.25);
  - транспорт едет справа налево по «дороге» между `bg_near` и тротуаром, 120–220 px/с, подрагивание ±1 px, пятно фар; событие `VehiclePassed(x)` — для звука в V6;
  - клиенты: `CustomerView._umbrella` (дочерний объект тела), `CustomerQueue.SetRaining` — зонтики пастельных цветов раскрываются с «поп», новые клиенты в дождь приходят уже с зонтиком;
  - настройка «Эффекты фона» (`SettingsSave.EffectsOn`, по умолчанию вкл., старые сохранения — вкл.): выкл. → только облака и пар; строка `settings.effects` ru/en, окно настроек выше (3 строки);
  - DebugWindow: «Дождь», «Снег», «Ясно», «Машина сейчас»;
  - время реальное (`deltaTime / TimeScale`, шаг ≤ 0.1 с) — «×10» фон не ускоряет, после паузы и скрытой вкладки рывка нет; альфа — через `CanvasRenderer.SetAlpha`, без аллокаций в `Tick`;
  - `ArtAssigner`: транспорт по городам (всем `car_01..03` + `scooter_courier`, Питеру и Москве ещё `trolleybus`), таблица высот транспорта в `Ambient.asset`, отчёт «ambient: N/7».
- Известные проблемы / TODO:
  - выполнить `Art/Assign Icons` (ожидается «ambient: 7/7») + `Build Game UI`;
  - проверить в Unity: машины едут по дороге за очередью и не перекрывают ларёк, воробей садится на навес, зонтик над головой у детей и взрослых, сугроб на навесе по ширине навеса;
  - замер в Profiler (GC Alloc от `AmbientWorldView`/`WeatherView` = 0 B) — не сделан, сделать при проверке;
  - погода не сохраняется: `Force` из DebugWindow действует до перезапуска.
- Файлы:
  - созданы: `Scripts/Ambient/*.cs` (7), `Tests/EditMode/WeatherScheduleTests.cs`, `Sprites/Ambient/*.png`
  - изменены: `CityConfig` (+ 5 ассетов городов), `GameSettings`, `SettingsSave`, `SettingsSaveParticipant`, `SettingsPopup`, `SettingsTests`, `CustomerView`, `CustomerQueue`, `GameSceneInstaller`, `GameScreen`, `DebugWindow`, `GameUiBuilder`, `ArtAssigner`, `ContentSetup`, `SpriteImportSettings`, `strings.txt`, `StringsFileTests`

### Правки по скриншотам: линия земли, флажки, зонтики, иконки магазина (код готов, 2026-10-05; ждёт Assign Icons + Build Game UI)
- Проверено: runtime, editor и тесты компилируются вне Unity без ошибок и warnings. В Unity не запускалось.
- Сделано:
  - общая линия земли `GroundY` (= основание ларька, 36 px ниже бордюра): улица с клиентами (вместе с сердечками и облачком), кот, меню-доска, столик, цветы и столбики флажков стоят на ней — раньше клиенты стояли на бордюре и «летали» над ларьком;
  - флажки: внутренний конец гирлянды — точно на углу навеса, внешний (шарик-навершие) — на деревянном столбике до земли с тёмной «пяткой»;
  - зонтики: не над головой, а в руке — ручка у кисти чуть впереди, купол завален назад на 38° за плечо; зонтик позади тела (лицо не закрывает), держатель `UmbrellaRig` повторяет подскок и наклон тела (`CustomerView._umbrellaRig`);
  - магазин: `CoinPackConfig.Icon`; `Art/Assign Icons` ставит мешкам монет `Sprites/Shop/<Id>.png`, пока их нет — `icon_coin`; строка «Купюры за рекламу» — `icon_ad` (`ShopPanel._adIcon`). Цветных плашек-заглушек больше нет;
  - карта: промт `Docs/Prompts/Stage_V5b_MapShop.md` и пакет I в `ASSET_PROMPTS.md` (фон-иллюстрация, миниатюры городов, фургон, булавка, мешки монет).
- Известные проблемы / TODO:
  - сгенерировать пакет I и выполнить V5b;
  - если зонтик в руке выглядит не так — числа в `GameUiBuilder`: `UmbrellaHand` (кисть), `UmbrellaGrip` (точка хвата на картинке), `UmbrellaTilt`, `UmbrellaHeight`;
  - левый столбик флажков стоит за коробкой кота (кот перед ним) — проверить, не мешает ли.

### Этап V5b — Нарисованная карта филиалов и иконки магазина (код и ассеты готовы, 2026-10-05; ждёт Assign Icons + Build Game UI и проверки в Unity)
- Проверено: runtime, editor и тесты компилируются вне Unity без ошибок и warnings. В Unity не запускалось.
- Ассеты (пакет I, исходники 1254×1254 удалены из `Assets/`):
  - `Sprites/Shop/coins_small|medium|large.png` (512): нарисованная «шахматка» вместо прозрачности вырезана заливкой от краёв до обводки плитки;
  - `Sprites/Map/map_city_01..05.png`, `map_van.png` (512), `map_pin.png` (256, белёсый ореол вырезан);
  - `Sprites/Map/map_bg.png`: квадрат обрезан до полосы 1024×780 (верх и пляж внизу частично срезаны), чтобы все пять полян поместились под плашкой.
- Сделано:
  - `ArtAssigner`: `MapFiles`, `LoadMapSprite`, отчёт `map: N/8`; `CityConfig._mapIcon` (`MapIcon`) — `map_city_0X` по номеру в имени ассета. Импорт: `map_bg` до 2048, остальное в `Map/` до 512, `Shop/` как иконки (256), всё сжато; версия постпроцессора 5;
  - позиции городов — центры полян на `map_bg` (найдены по песочному цвету): Родной (0.193, 0.313) — слева внизу, Казань (0.202, 0.704) — слева вверху, Сочи (0.484, 0.161) — у моря, Питер (0.745, 0.666) — у заснеженных гор, Москва (0.789, 0.212) — справа внизу;
  - фон: `MapHeight` 760 (карточка 1110) под пропорции картинки; картинка с городами вписана целиком (`AspectRatioFitter` FitInParent, область городов = картинка), поля на широком экране закрывает тот же фон «на обрез», затемнённый; скруглённая маска. Без `map_bg` — прежние холмы, озеро и деревья;
  - узел: миниатюра 120×120, бейдж номера справа внизу (у открытых — галочка), Locked — тон `Disabled` α0.75 и замок, Next — свечение `glow_soft` и пульс, Current — булавка `map_pin` над миниатюрой качается ±6 px за 1.6 с (`BobEffect`, через `PulseDriver`). Без миниатюры — прежний кружок; без `map_pin` — прежняя плашка «Вы здесь»;
  - подпись — на ленточке `pill_fill` (`Paper` α0.9) шириной по имени; длинное имя уменьшается до 22;
  - дорога — квадратичные Безье с изгибом ±12% (знак чередуется), штрихи 22×10 по касательной, шаг 30, у миниатюр штрихов нет (`_nodeClearance` 72). Фургон `map_van` едет по той же кривой, наклон по касательной не больше ±15°, отражается по направлению;
  - плашка выбранного города: миниатюра 72×72, имя, статус справа (`Следующая цель` — `Secondary`), строка порога с монетой;
  - магазин: у всех строк картинки — купюры, мешки монет (после Assign Icons — нарисованные), реклама (`icon_ad`), «Без рекламы», «×2», стартовый набор.
- Файлы: новый `UI/BobEffect.cs`; изменены `UI/CityMapView.cs`, `UI/CityMapNodeView.cs`, `Prestige/CityConfig.cs`, `Editor/ArtAssigner.cs`, `Editor/SpriteImportSettings.cs`, `Editor/GameUiBuilder.cs`, `Configs/Cities/City_01..05.asset`.
- Известные проблемы / TODO:
  - в Unity: `Art/Assign Icons` (отчёт `map: 8/8`, `coin packs: 3/3`) → `Build Game UI` → вкладка «Город»;
  - если булавка у Казани (верхний ряд) задевает плашку — уменьшить `bannerHeight` в `BuildCityMap` или сдвинуть точку чуть ниже;
  - при новой картинке фона позиции городов надо пересчитать (центры полян в долях картинки).

### Этап V6 — Звуковая атмосфера: lo-fi, улица, дождь, кот (код и звуки готовы, 2026-10-06; ждёт Assign Icons + Build Game UI и проверки в Unity)
- Проверено: runtime и editor компилируются вне Unity без ошибок и warnings. В Unity не запускалось, звуки на слух не проверены.
- Звуки (пакет G, все CC0, источники — `Assets/_Game/Audio/LICENSES.md`, `CREDITS.md`):
  - музыка HoliznaCC0: `music_cozy_01` «Poor, But Happy» (2:08), `02` «Blue Skies» (2:56), `03` «Night Driving» (2:11) — OGG стерео, ≈4–5 МБ исходник, в билде Vorbis 50%;
  - эмбиент (Freesound HQ-превью): улица 91 с, море 71 с, дождь 48 с, ветер 60 с, гриль 18 с; эффекты: мурчание, мяу, машина (Freesound), монета и «поп» (Kenney).
- Сделано:
  - `MusicService`: два источника, новый трек — кроссфейд (`Music.asset`: громкость 0.35, 2 с); трек города — `CityConfig._music`, без него — `HappyClappyLoop`;
  - `AmbientAudioService` (Boot, переживает переезд) + `AmbientLayer` (петля на двух источниках: стык проходит кроссфейдом `_loopOverlap` 2 с, т.к. записи не бесшовные) + `AmbientAudioConfig` (`Configs/AmbientAudio.asset`);
  - погода: `WeatherService.WeatherChanged` → дождь/ветер, кроссфейд 3 с; при выключенных «Эффектах фона» погоды не видно — и не слышно;
  - гриль: громкость 0.10–0.18 по `CustomerQueue.Fullness` (догоняет за 4 с);
  - `AudioService`: `SoundEntry.MaxVoices` (монеты — не больше 3 голосов, лишние пропускаются), `PitchOffset` (монета 0.95–1.08), `PlayPanned`/`SetPan` — машина: `AmbientWorldView.VehicleMoved` двигает панораму −0.6…0.6;
  - `SoundId.CarPass`, `PopupOpen` (`PopupAnimator.Show` из скрытого состояния);
  - «Звук» выключает эффекты и эмбиент (слои затухают и встают на паузу), «Музыка» — музыку; пауза игры — `AudioListener.pause`, всё продолжается с того же места;
  - `AudioAssigner` (вызывается из `Art/Assign Icons`, отчёт `sounds: N/13`): музыка и основа по городам (Сочи — море), клипы `AmbientAudio.asset`, записи `Audio.asset`; без `coin_soft` — прежние фишки;
  - `AudioImportSettings`: `Audio/Music` — Streaming, Vorbis 50%; `Audio/Ambient` — Compressed In Memory, 45%; `Audio/Sfx` — Decompress On Load, моно.
- Громкости:

| Что | Где звучит | Громкость |
|---|---|---|
| Музыка | везде; города 1–2 — cozy_01, 3 — cozy_02, 4–5 — cozy_03 | 0.35 |
| Улица / море | основа эмбиента (Сочи — море) | 0.25 |
| Гриль | всегда, громче при длинной очереди | 0.10–0.18 |
| Дождь / ветер | при дожде / снеге | 0.30 |
| Монета | доход | 0.5 × master 0.8, ≤ 3 голоса, ≥ 0.08 с |
| Поп | открытие попапа | 0.5 |
| Мурчание / мяу | кот | 0.6 / 0.5 |
| Машина | проезд, панорама справа налево | 0.2 |

- Файлы: новые `Core/Audio/AmbientAudioService.cs`, `AmbientLayer.cs`, `AmbientAudioConfig.cs`, `Editor/AudioAssigner.cs`, `Editor/AudioImportSettings.cs`, `Audio/Music|Ambient|Sfx/*.ogg`; изменены `MusicService`, `MusicConfig`, `AudioService`, `SoundEntry`, `SoundId`, `GameBootstrap`, `GameSceneInstaller`, `CityConfig`, `GameScreen`, `CustomerQueue`, `AmbientWorldView`, `PopupAnimator`, `ArtAssigner`, `ContentSetup`, `GameUiBuilder`.
- Известные проблемы / TODO:
  - треки и звуки выбраны по описаниям и популярности, на слух не проверены — если что-то не нравится, замени файл с тем же именем (варианты: HoliznaCC0 «New Shoes», «Puppy Love», «Families», «Autumn»; omfgdude «Lofi Hip Hop Loop»);
  - Sizzle 18 с — повтор может быть заметен; при желании взять длиннее (Freesound 642773, 51 с);
  - машина звучит только если в городе есть картинки транспорта (пакет F).

### Правки после V6 (2026-10-06; ждут Build Game UI и проверки в Unity)
- «Эффекты фона» выключают и звуки города (улица/море, гриль, дождь), а не только «Звук» (`AmbientAudioService.ApplySettings`).
- Календарь: 7-й день — 10 купюр вместо скина «Золотой ларёк» (`RetentionConfig`, `Retention.asset`, GDD §9). Ларёк всегда в цвете города, даже если скин остался в старом сейве; модель скина (`RewardKind.KioskSkin`, `KioskSkinOwned`) оставлена для совместимости сейвов.
- Рамка из лампочек вокруг вывески: проём рамки подогнан под видимую доску `kiosk_sign` (замер по альфе: проём 393×268 из 512×396, доска 481×256 из 512×300), заходит на доску на 4 px сверху/снизу и 14 px по бокам — щелей нет; лампочки растянуты по ширине на ~18% (у рамки и вывески разные пропорции: 1.29 и 1.71).
- Флажки: внутренний шарик гирлянды — на скрутке навеса (`AwningRollX` 0.38, `AwningRollY` 0.86 от размеров навеса), раньше висел на 34 px правее и 20 px выше. `FlagsKnobX/Y` уточнены по альфе: 0.46 / 0.90.

### Этап V7 — Режим любования, экран загрузки, атласы (код готов, 2026-10-06; замеры и скриншоты — в Unity)
- Проверено: runtime, editor и тесты компилируются вне Unity без ошибок и warnings; экран загрузки проверен в браузере (портрет 375×812, ландшафт 1280×720). В Unity не запускалось.
- Сделано:
  - `UI/ZenMode.cs`: кнопка `icon_zen` в HUD справа от настроек; вход — HUD уезжает вверх, вкладки и список вниз, всё тает за 0.45 с (InOutSine); `ResponsiveLayout.SetZen` растягивает зону ларька на весь экран с увеличением ×1.3 (портрет) / ×1.2 (ландшафт); подсказка «Нажми, чтобы вернуться» через 3 с на 4 с; выход — тап по сцене (ловушка `ZenExitCatcher` лежит сразу над городом: кот, клиенты, ларёк кликабельны) или любой попап (`PopupAnimator.AnyShown`); во время туториала кнопки нет;
  - в режиме монеты не летят и не звенят (`CoinFlyEffect.Suppressed`), над окном ларька раз в 1.5 с всплывает накопленный «+X»; музыка тише на 15% (`MusicService.VolumeScale`); календарь, задания и подарок в зоне ларька тают вместе с HUD;
  - экран загрузки: `WebGLTemplates/YandexGames/Images/loading_bg.jpg` (1024², 130 КБ, из `Sprites/Promo/loading_bg.png`): размытая копия на весь экран + картинка целиком с мягкими краями, золотая капсула прогресса на `#4A2C1D` 40%, системный шрифт (без Google Fonts — внешние запросы на Яндексе нежелательны). Логика PluginYG не тронута; в `SettingsYG2.asset` градиент до загрузки и цвета полосы — из палитры игры;
  - `Editor/SpriteAtlasSetup.cs` — меню `ShaurmaTycoon/Art/Build Sprite Atlases`: атласы V2 в `Assets/_Game/Atlases`: UI (Generated + Icons + Shop), Stations_City01..05, Kiosk, Characters, Ambient; фоны и карта — без атласа;
  - `SpriteImportSettings` (версия 6): WebGL — DXT5 Crunched, фоны quality 50, остальное 80; `Promo/` не трогается.
- Не сделано (нужен Unity):
  - задача 4: draw calls в Frame Debugger до/после атласов;
  - задача 6: WebGL-билд, размер `Build/*.data` до (ветка `stage-09-yandex-games`) и после, FPS при 4× CPU throttling во время дождя;
  - задача 7: скриншоты в `Docs/Visual/Screens/`.
- Известные проблемы / TODO:
  - если в портретном любовании украшения ларька обрезаются по краям — уменьшить `ResponsiveLayout._zenScale.x` (1.3 → 1.2);
  - старые Kenney-спрайты `Sprites/Customers/*` (запасные клиенты) — если не используются, их можно убрать из билда.

### Этап 12 — Оптимизация и релиз (код готов, 2026-10-06; ждёт билда и прогона на черновике)
- Проверено: runtime (в т.ч. с дефайнами WebGL-плеера), editor и тесты компилируются вне Unity без ошибок и warnings; тесты вне Unity: 270 зелёных, 16 падают только средовые (вызовы движка, замер аллокаций); новые ReleaseChecksTests 6/6, SaveServiceTests +1. В Unity, WebGL-билде и на Яндексе не проверялось.
- Сверка с требованиями Яндекса (`Docs/Release/YANDEX_RELEASE.md` — чек-лист по пунктам, настройка консоли, тексты черновика, ручной прогон). Найдено и исправлено:
  - 1.19.3 GameplayAPI не размечался → `IPlatformService.SetGameplayPaused`: start после Game Ready, stop на любую паузу, start при возврате (`YB_SetGameplay`);
  - 1.13.2/1.13.4/1.13.6 цены были захардкожены → `Shop/PriceCatalog` (Boot) из `payments.getCatalog()`: число + валюта портала (YAN → «ян»/«YAN»); товар, которого нет в каталоге консоли, не показывается; до ответа каталога — `DisplayPrice` из конфига. Применено в `ShopPanel` (паки, «Навсегда») и `StarterPackCard`;
  - 1.3 Alt+Tab не глушил звук (вкладка видна) → мост шлёт `wblur/wfocus` (window blur/focus, возврат и по тапу), `PauseController.SetWindowFocus`;
  - 4.7 пауза на рекламе зависела только от `game_api_pause` → `IPlatformService.AdShowing` + `PauseController.SetAdShowing` на время rewarded/interstitial;
  - 1.9 покупки станций/улучшений/менеджеров ждали автосейва до 10 с → `Wallet.Spent` и трата купюр вызывают `SaveService.RequestSoon` (сейв через 1 с, серия покупок — одна запись); `pagehide` в мосте сохраняет при перезагрузке/закрытии.
- Размер билда:
  - `Editor/YandexRelease.cs`, меню `ShaurmaTycoon/Release`: **Apply WebGL Release Settings** (Brotli без fallback, Managed Stripping High, IL2CPP OptimizeSize + Master, стек в логах только для ошибок, Development Build выкл., очистка динамического атласа Rubik), **Preflight Check** (платформа, Development, шаблон и `/sdk.js`, первая сцена Boot, внешние ссылки в коде, ru/en у всех строк, `autoGRA` выкл.), **Build Yandex (WebGL)** → `Builds/Yandex` + `Builds/Yandex_size_report.txt`; batch: `-executeMethod ShaurmaTycoon.EditorTools.YandexRelease.BuildFromCommandLine`;
  - PluginYG2 перед каждой сборкой применял свой профиль и возвращал стрипинг **Minimal** → в `YandexGames.asset` стрипинг High, `ApplyReleaseSettings` синхронизирует профиль; `Assets/_Game/link.xml` сохраняет `ShaurmaTycoon` и `Assembly-CSharp` (JsonUtility, SendMessage, рефлексия плагина по атрибутам);
  - `HappyClappyLoop.wav` (6 МБ) больше не в билде: запасной трек `Music.asset` → `music_cozy_01` (и в `ContentSetup`);
  - `AudioImportSettings` v2: музыка quality 0.35, эмбиент моно 0.3;
  - `Rubik SDF`: `m_ClearDynamicDataOnBuild = 1`.
- Debug Checker (2026-10-06): неофициальный [yandex-games-debug-checker](https://github.com/Nioris/yandex-games-debug-checker) в `Tools/YandexDebugChecker` (вне Assets), меню **Build Yandex Debug (checker)** → `Builds/YandexDebug` (без сжатия, тег после `/sdk.js`); Preflight и релизная сборка не пропускают `debugcheck.js`. Прогон через `sdk-dev-proxy --dev-mode` (заглушка SDK): Game Ready ~3–4 с, `i18n.lang` читается до ready, rewarded по жесту (4 мс) с наградой, GameplayAPI stop/start на скрытие вкладки. FAIL «ready не найден» / «lang не читается» — ложные (статика не видит `framework.js`, мок отдаёт новый `i18n` на каждое обращение). Исправлено по чекеру: шаблон слушает `orientationchange` и `fullscreenchange` (1.6.1.3/1.10.1). Interstitial при переезде и настоящий SDK не проверены.
- Промо: `Docs/Release/Store/icon_512.png` и `cover_800x470.png` — кадрирование иллюстрации экрана загрузки (не скриншот — п. 5.6).
- Решения:
  - модули PluginYG2 не импортируются — свой мост уже покрывает рекламу, сейвы, покупки, каталог, лидерборд, LoadingAPI, GameplayAPI, язык и время; модули дублировали бы инициализацию/паузу и добавили вес;
  - исключения WebGL остаются `FullWithoutStacktrace`: «только явные» превращают любой NullReferenceException в падение игры (1.14);
  - пауза по потере фокуса окна теперь используется (раньше отказались из-за риска «залипшей» паузы): возврат и по `focus`, и по тапу в игру.
- Известные проблемы / TODO:
  - собрать `ShaurmaTycoon/Release/Build Yandex (WebGL)`, записать размер из `Builds/Yandex_size_report.txt` сюда; если > 20 МБ — смотреть топ ассетов в отчёте;
  - High stripping не проверен в билде: если что-то пропало (сейв, мост) — смотреть консоль браузера, при необходимости дописать `link.xml` или вернуть Medium;
  - консоль: товары и лидерборд (таблица в `YANDEX_RELEASE.md` §2), облачные сохранения, скриншоты;
  - ручной прогон на черновике — `YANDEX_RELEASE.md` §4;
  - `Rubik SDF.asset` и `YandexGames.asset` правились на диске при открытом Unity — если редактор их перезапишет, `Apply WebGL Release Settings` выставит значения заново;
  - `HappyClappyLoop.wav` можно удалить из проекта, если не нужен.

## Важные решения (архитектура)
- Unity 2022.3.62f1, 2D, URP 2D, Color Space Gamma, старый Input Manager.
- Сборки: `ShaurmaTycoon` (runtime), `ShaurmaTycoon.Editor`, `ShaurmaTycoon.Tests.EditMode` (Assets/_Game/Tests/EditMode). Namespace `ShaurmaTycoon.<Модуль>`, для редакторского кода `ShaurmaTycoon.EditorTools`.
- Сервисы регистрируются в `GameBootstrap` через `Services.Register<T>`, единственный Update в игре — в `TickManager`.
- Сервисы Boot: `TickManager` (+ `ApplicationEvents`), `MusicService` (+ единственный `AudioListener` на объекте «Audio»), `GameSettings`, `Wallet`, `BillWallet`, `TweenRunner`, `PulseDriver`, `IPlatformService`, `Loc`, `SaveService`, `PrestigeState`. Сервисы сцены Game (`GameSceneInstaller`): `BalanceConfig`, `CityConfig` (текущий), `CityCatalog`, `StationController`, `ManagerController`, `UpgradeService`, `ReputationShop`, `BillShop`, `BillPurchases`, `AdBillService`, `AudioService`, `CustomerConfig`, `OfflineIncomeService`, `PrestigeController`, `BoostService`, `GiftBoxService`, `DailyRewardService`, `DailyQuestService`, `RetentionConfig`, `TutorialService`; снимаются через `Services.Unregister(instance)`. Вью связываются в `GameScreen.Start`.
- Цвета и тайминги UI — только из `Configs/UiTheme.asset` (`UiTheme`), базовые формы — `Sprites/UI/Generated/` (`ProceduralSprites`); кнопки — `SoftButton`, попапы — `PopupAnimator`; постоянные UI-анимации реализуют `IUiAnimated` и регистрируются в `PulseDriver` (без своего Update).
- Все тексты UI — ключи из `Configs/Localization/strings.txt` (TSV: key, ru, en); новые ключи добавлять туда и в `StringsFileTests.RequiredKeys`.
- Тексты, которые меняются в Tick, выводить через `NumberText` + `TMP_Text.SetNumberText`, шаблоны — `Loc.GetTemplate(key)` (без string.Format).
- Новая система с состоянием → реализовать `ISaveParticipant` и зарегистрировать в `SaveService`; новые поля `SaveData` — со значением по умолчанию, при несовместимых изменениях повышать `SaveData.CurrentVersion` и добавлять шаг в `SaveMigrator`.
- Вью сцены Game, подписанная на сервис Boot (`Wallet`, `BillWallet`, `Loc`, `PrestigeState`, `GameSettings`), реализует `ISceneBinding` и вызывает `SceneBindings.Track(this)` в `Bind` — `OnDestroy` у ни разу не активных объектов не вызывается.
- Новый город = новый `CityConfig` (свои станции и улучшения) в `Configs/Cities/CityCatalog.asset`; переход между городами перезагружает сцену Game.

## TODO(YANDEX)
- Консоль Яндекса: лидерборд `richest`, товары паков купюр, включить Payments (см. этап 09; полный список — `Docs/Release/YANDEX_RELEASE.md` §2).
- В консоли Яндекса (постоянные товары): `starter_pack` 99, `no_ads` 149, `income_x2` 299. Цены в игре уже берутся из каталога платформы (этап 12).
- Проверка на черновике: реклама, облачный сейв между устройствами, язык, пауза, неподтверждённые покупки.
- WebGL-шаблон на весь экран: проверить в билде (шаблон YandexGames из плагина уже подключён).
