# PROGRESS

## Текущий этап: 03 — UI игрового экрана (в работе)

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
  - Play сразу из сцены Game упадёт на `Services.Get`. Нужно запускать из Boot (можно добавить автопереход в редакторе позже);
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

## Важные решения (архитектура)
- Unity 2022.3.62f1, 2D, URP 2D, Color Space Gamma, старый Input Manager.
- Сборки: `ShaurmaTycoon` (runtime), `ShaurmaTycoon.Editor`, `ShaurmaTycoon.Tests.EditMode` (Assets/_Game/Tests/EditMode). Namespace `ShaurmaTycoon.<Модуль>`, для редакторского кода `ShaurmaTycoon.EditorTools`.
- Сервисы регистрируются в `GameBootstrap` через `Services.Register<T>`, единственный Update в игре — в `TickManager`.

## TODO(YANDEX)
- Выбрать PluginYG2 или свой jslib-мост (этап 09).
- `GameBootstrap.CreatePlatform`: в WebGL создавать `YandexPlatformService`.
- Уточнить сигнатуры лидерборда и покупок в `IPlatformService`.
- WebGL-шаблон на весь экран (дефолтный шаблон Unity даёт канвас фиксированного размера).
