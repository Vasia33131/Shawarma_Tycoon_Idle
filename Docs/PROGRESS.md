# PROGRESS

## Текущий этап: 05 — Клиенты и «сочность» (в работе)

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

## Важные решения (архитектура)
- Unity 2022.3.62f1, 2D, URP 2D, Color Space Gamma, старый Input Manager.
- Сборки: `ShaurmaTycoon` (runtime), `ShaurmaTycoon.Editor`, `ShaurmaTycoon.Tests.EditMode` (Assets/_Game/Tests/EditMode). Namespace `ShaurmaTycoon.<Модуль>`, для редакторского кода `ShaurmaTycoon.EditorTools`.
- Сервисы регистрируются в `GameBootstrap` через `Services.Register<T>`, единственный Update в игре — в `TickManager`.
- Сервисы Boot: `TickManager`, `Wallet`, `IPlatformService`, `Loc`. Сервисы сцены Game (`GameSceneInstaller`): `StationController`, `ManagerController`, `UpgradeService`. Вью связываются в `GameScreen.Start`.
- Все тексты UI — ключи из `Configs/Localization/strings.txt` (TSV: key, ru, en); новые ключи добавлять туда и в `StringsFileTests.RequiredKeys`.

## TODO(YANDEX)
- Выбрать PluginYG2 или свой jslib-мост (этап 09).
- `GameBootstrap.CreatePlatform`: в WebGL создавать `YandexPlatformService`.
- Уточнить сигнатуры лидерборда и покупок в `IPlatformService`.
- WebGL-шаблон на весь экран (дефолтный шаблон Unity даёт канвас фиксированного размера).
