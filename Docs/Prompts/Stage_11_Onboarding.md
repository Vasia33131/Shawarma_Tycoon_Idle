# Этап 11 — Онбординг и баланс

## Цель
Новый игрок без слов понимает, что делать в первые минуты (рука-подсказка: тап → уровень → вторая станция → менеджер); у разработчика есть отладочное окно в Development build; баланс города 1 проверен прогоном и рассчитан на ~2–3 ч.

## Контекст
- Уже готово: `StationController`/`StationModel` (тап, покупка, `CycleCompleted`, `Changed`), `ManagerController.ManagerHired`, `UpgradeService`, `Wallet`, `SaveService` + `ISaveParticipant`, `TickManager.TimeScale` (заготовка под ×10), `GameUiBuilder`, `StationsPanel`/`ManagersPanel`/`TabBar`, `Editor/DebugMenu` (временные пункты).
- GDD §9: онбординг — первые 60 с, рука: тапни → купи уровень → купи вторую станцию → найми менеджера. GDD §4–§5, §7: таблица станций, `cost = baseCost × growth^n` (станции 1–2 можно 1.15), менеджер = baseCost × 1000, порог переезда, репутация.
- Ограничения: числа баланса — только в SO-ассетах; без аллокаций в Tick; тексты ru/en; новые системы — `ISaveParticipant`; вью на сервисах Boot — `ISceneBinding`; отладочное окно не должно попасть в релизную сборку.

## Файлы
- Создать `Scripts/Onboarding/`: `TutorialStep`, `TutorialService`.
- Создать `Scripts/UI/TutorialHand.cs`, `Scripts/Core/DebugWindow.cs`.
- Создать `Scripts/Balance/`: `BalanceSimulator`, `BalanceSimulationSettings`, `BalanceReport`, `BalancePurchase`, `BalancePurchaseKind`, `BalanceSample`, `BalanceReportFormatter`; `Scripts/Editor/BalanceMenu.cs`.
- Создать тесты `TutorialTests`, `BalanceSimulatorTests`.
- Изменить: `SaveData` (+`TutorialStep`), `SaveService` (`Wipe`), `GameSceneInstaller`, `GameBootstrap`, `GameScreen`, `StationRowView`/`StationsPanel`/`ManagerRowView`/`ManagersPanel`/`TabBar` (цели подсказки), `GameUiBuilder`, `DebugMenu`, `strings.txt`, `StringsFileTests`; ассеты баланса (по итогам прогона).

## Задачи (по порядку, каждая проверяемая)
1. [x] Симулятор баланса на настоящих моделях + прогон текущих ассетов.
2. [x] Решение по балансу (вопрос пользователю) и правка ассетов.
3. [x] `TutorialService`: шаги по состоянию игры, пропуск выполненных, сохранение шага.
4. [x] `TutorialHand` + сборка в `GameUiBuilder`; календарь не открывается поверх туториала.
5. [x] `DebugWindow` (редактор и Development build): +монеты, время ×10, пропуск туториала, сброс сейва.
6. [x] Меню `ShaurmaTycoon/Balance/Simulate Cities`, тесты, строки.
7. [x] GDD, PROGRESS.md (баланс-таблица).

## Критерии приёмки
- [ ] Компилируется, консоль Unity без ошибок и warnings (вне Unity — да)
- [ ] Новый игрок без подсказок понимает, что делать (проверить в Unity)
- [x] Отладочное окно только в Development build (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`)
- [x] Баланс-таблица в `PROGRESS.md`: вторая станция в первую минуту, город 1 ≈ 2–3 ч
- [x] PROGRESS.md обновлён

## Ручная проверка в Unity (для меня)
1. `ShaurmaTycoon/Build Game UI`, затем `ShaurmaTycoon/Save/Delete Save` → Play из Boot.
2. Рука показывает на первую станцию «Тапни…» → после первой шаурмы — на «Купить» → на «Открыть» второй станции → на вкладку «Менеджеры» → на «Нанять». Пока не хватает монет на шаг — руки нет; она появляется, как только монет хватает. После найма рука пропадает, открывается календарь.
3. Кнопка «DEV» слева (или F2): +1K/+1M/+1B, «Время ×10», «Пропустить туториал», «Сбросить сейв» (два нажатия → Play останавливается, следующий запуск — новая игра).
4. `ShaurmaTycoon/Balance/Simulate Cities` → таблицы в консоли и буфере обмена.
5. Test Runner → EditMode → `BalanceSimulatorTests.City1Assets_MeetStage11Targets` зелёный.

## НЕ делать на этом этапе
- Оптимизация размера билда, чек-лист модерации (этап 12).
- Новые механики сверх GDD.

## Решения
- Баланс (выбор пользователя): рост цены станции 1 — 1.07, станций 2–6 — 1.15 (во всех городах); цены улучшений ×10; пороги переезда ×10 (город 1 — 1e10); делитель репутации 1e6 → 1e7.
- Туториал засчитывает шаг по состоянию игры (уровень, открытая станция, нанятый менеджер), а не по нажатию на подсказку; старые сейвы с прогрессом проходят его сразу.
- Календарь наград новичку открывается после туториала, а не поверх него.
