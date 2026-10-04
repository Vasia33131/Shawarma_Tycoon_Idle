# PROGRESS

## Текущий этап: 02 — Станции (в работе)

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

## Важные решения (архитектура)
- Unity 2022.3.62f1, 2D, URP 2D, Color Space Gamma, старый Input Manager.
- Сборки: `ShaurmaTycoon` (runtime), `ShaurmaTycoon.Editor`, `ShaurmaTycoon.Tests.EditMode` (Assets/_Game/Tests/EditMode). Namespace `ShaurmaTycoon.<Модуль>`, для редакторского кода `ShaurmaTycoon.EditorTools`.
- Сервисы регистрируются в `GameBootstrap` через `Services.Register<T>`, единственный Update в игре — в `TickManager`.

## TODO(YANDEX)
- Выбрать PluginYG2 или свой jslib-мост (этап 09).
- `GameBootstrap.CreatePlatform`: в WebGL создавать `YandexPlatformService`.
- Уточнить сигнатуры лидерборда и покупок в `IPlatformService`.
- WebGL-шаблон на весь экран (дефолтный шаблон Unity даёт канвас фиксированного размера).
