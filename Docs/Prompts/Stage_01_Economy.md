# Этап 01 — Экономика и форматирование чисел

## Цель
Появляется ядро экономики: кошелёк монет с событием изменения, форматирование больших чисел и формулы цены уровня, цены ×N и ×Max. Всё покрыто EditMode-тестами.

## Контекст
- Уже готово (этап 00): `Assets/_Game/Scripts/Core/Services.cs`, `Core/GameBootstrap.cs` (здесь регистрируются сервисы), сборка `ShaurmaTycoon`.
- Разделы GDD:
  - §5: цена уровня `cost(n) = baseCost × growth^n`, growth = 1.07 (для станций 1–2 можно 1.15, задаётся в конфиге станции на этапе 02);
  - §5: «Покупка ×Max — геометрическая прогрессия, считать формулой, а не циклом»;
  - §4: base cost станций от 4 до 1 244 160, доходы растут до 1e9+ и выше после престижа.
- Ограничения:
  - числа — `double`;
  - формат `1.23K / 4.56M / 7.89B / 1.00T / 1.00aa / 1.00ab…`;
  - экономика — чистые C#-классы без UnityEngine, чтобы их можно было тестировать;
  - баланс (growth и base cost) в код не зашивается, а передаётся параметрами (SO-конфиги — этап 02).

## Файлы
- Создать: `Assets/_Game/Scripts/Economy/Wallet.cs` — монеты:
  - `Coins`, событие `CoinsChanged(double)`;
  - `Add`, `CanAfford`, `TrySpend`, `SetCoins` (для загрузки сейва);
  - защита от отрицательных значений, NaN и Infinity.
- Создать: `Assets/_Game/Scripts/Economy/BigNumberFormatter.cs`:
  - `Format(double)` → 3 значащие цифры с отбрасыванием лишнего (без округления вверх, игрок не видит больше, чем у него есть);
  - суффиксы K, M, B, T, затем aa…az, ba…zz;
  - InvariantCulture: всегда точка, а не запятая (в русской локали иначе будет `1,23K`).
- Создать: `Assets/_Game/Scripts/Economy/EconomyMath.cs`:
  - `LevelCost(baseCost, growth, level)`;
  - `BulkCost(baseCost, growth, level, count)` — сумма геометрической прогрессии;
  - `MaxAffordable(baseCost, growth, level, money)` — через логарифм, с коррекцией погрешности double.
- Создать: `Assets/_Game/Tests/EditMode/ShaurmaTycoon.Tests.EditMode.asmdef` — тестовая сборка (только Editor, ссылается на `ShaurmaTycoon` и NUnit).
- Создать: `Assets/_Game/Tests/EditMode/BigNumberFormatterTests.cs`, `EconomyMathTests.cs`, `WalletTests.cs`.
- Изменить: `Assets/_Game/Scripts/Core/GameBootstrap.cs` — создать и зарегистрировать `Wallet`.

## Задачи (по порядку, каждая проверяемая)
1. [x] `BigNumberFormatter` + тесты.
2. [x] `EconomyMath` + тесты: совпадение формулы ×N с суммой в цикле, ×Max на границах (ровно хватает или на 1 меньше), growth = 1.
3. [x] `Wallet` + тесты: события, TrySpend при нехватке, защита от некорректных значений.
4. [x] Регистрация `Wallet` в `GameBootstrap`.
5. [ ] Прогнать тесты (Test Runner → EditMode). Вне Unity уже прогнаны: 63/63 зелёные.
6. [x] Обновить `PROGRESS.md`.

## Критерии приёмки
- [ ] Компилируется, консоль Unity без ошибок и warnings
- [ ] Все EditMode-тесты зелёные
- [x] `1234 → 1.23K`, `1e15 → 1.00aa`, `999 → 999`, `999999 → 999K`, `1e6 → 1.00M`
- [x] `BulkCost` равен сумме `LevelCost` в цикле (относительная погрешность < 1e-9)
- [x] `MaxAffordable`: на найденное количество денег хватает, на +1 — уже нет
- [x] Экономика не зависит от UnityEngine
- [x] PROGRESS.md обновлён

## Ручная проверка в Unity (для меня)
1. `Window → General → Test Runner → EditMode → Run All`.
2. Ожидаемый результат: все тесты зелёные.

## НЕ делать на этом этапе
- `StationConfig`, станции, доход, вехи (этап 02)
- UI и HUD (этап 03)
- Сохранение кошелька (этап 06)

## Риски / вопросы ко мне
- Решено без вопроса (взято из PLAN.md и технических правил): 3 значащие цифры (`1.23K`, `12.3K`, `123K`), после T идут aa, ab…
- Числа меньше 1000 показываются целыми, с отбрасыванием дробной части. Если для «доход/сек» понадобятся дроби (`0.5/с`), добавлю отдельный метод на этапе 03.
