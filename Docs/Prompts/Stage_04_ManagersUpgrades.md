# Этап 04 — Менеджеры и апгрейды

## Цель
Менеджер автоматизирует станцию, и она работает без тапов. Апгрейды (разовые покупки) умножают доход одной станции или всех. Вкладки «Менеджеры» и «Улучшения» рабочие, доход/сек в HUD появляется и считается с учётом апгрейдов.

## Контекст
- Уже готово:
  - `Stations/StationModel.cs` (`IsAutomated`, `IncomePerCycle`, `IncomePerSecond`), `Stations/StationController.cs` (`IncomePerSecond`);
  - `Core/BalanceConfig.cs`, `Prestige/CityConfig.cs`, `Core/GameSceneInstaller.cs`;
  - UI: `UI/GameScreen.cs`, `UI/StationsPanel.cs`, `UI/StationRowView.cs`, `UI/TabBar.cs`, сборщик `Editor/GameUiBuilder.cs`;
  - локализация `Configs/Localization/strings.txt`.
- Разделы GDD:
  - §4: с менеджером цикл запускается автоматически;
  - §5: `income = baseIncome × level × milestoneMult × upgradesMult × …`;
  - §5: цена менеджера = `baseCost × 1000`;
  - §5: улучшения — разовые покупки «станция X доход ×3», «все станции ×2», список в конфиге, ~20 на город.
- Ограничения:
  - числа баланса — в SO (множитель цены менеджера в `BalanceConfig`, апгрейды — ассеты `UpgradeConfig` в `CityConfig`);
  - логика — чистый C# с тестами;
  - тексты — через локализацию.

## Файлы
- Создать: `Scripts/Managers/ManagerController.cs`:
  - цена найма, найм (станция должна быть открыта, повторно нанять нельзя);
  - найм ставит `IsAutomated`;
  - событие `ManagerHired`.
- Создать: `Scripts/Upgrades/IUpgradeDefinition.cs`, `Scripts/Upgrades/UpgradeConfig.cs` (SO: id, цена, целевая станция или «все», множитель).
- Создать: `Scripts/Upgrades/UpgradeService.cs` — покупка, пересчёт множителей станций, событие `UpgradePurchased`.
- Создать: `Scripts/UI/StationColors.cs` — общая палитра кругов-иконок (вынесена из `StationsPanel`).
- Создать: `Scripts/UI/ManagersPanel.cs`, `Scripts/UI/ManagerRowView.cs`, `Scripts/UI/UpgradesPanel.cs`, `Scripts/UI/UpgradeRowView.cs`.
- Создать: `Configs/Upgrades/City01/Upgrade_01…20.asset` — 20 апгрейдов города 1.
- Создать: `Tests/EditMode/ManagerControllerTests.cs`, `UpgradeServiceTests.cs`, `TestUpgradeDefinition.cs`.
- Изменить: `StationModel.cs` — `IncomeMultiplier` и событие `Changed` (уровень, автоматизация или множитель).
- Изменить: `BalanceConfig.cs` + `Balance.asset` — `managerCostMultiplier = 1000`.
- Изменить: `CityConfig.cs` + `City_01_Home.asset` — список апгрейдов.
- Изменить: `GameSceneInstaller.cs` — создать и зарегистрировать `ManagerController` и `UpgradeService`.
- Изменить: `GameScreen.cs` — связать новые панели.
- Изменить: `StationRowView.cs` — подписка на `Changed`.
- Изменить: `GameUiBuilder.cs` — префабы `ManagerRow`, `UpgradeRow`, рабочие панели вместо заглушек.
- Изменить: `strings.txt`, `StringsFileTests.cs` — новые ключи.

## Задачи (по порядку, каждая проверяемая)
1. [x] `StationModel.IncomeMultiplier` + `Changed` + тесты.
2. [x] `ManagerController` + `managerCostMultiplier` в `BalanceConfig` + тесты.
3. [x] `IUpgradeDefinition`, `UpgradeConfig`, `UpgradeService` + тесты: ×3 на одну станцию, ×2 на все, перемножение, повторная покупка запрещена, цель вне города — ошибка конфига.
4. [x] 20 ассетов апгрейдов и список в `City_01_Home`.
5. [x] Регистрация в `GameSceneInstaller`.
6. [x] UI: `StationColors`, панели и строки менеджеров и апгрейдов, `GameScreen`, `StationRowView`.
7. [x] `GameUiBuilder`: новые префабы и панели. Локализация.
8. [x] Обновить `PROGRESS.md`.

## Критерии приёмки
- [x] Компилируется, консоль Unity без ошибок и warnings
- [x] EditMode-тесты зелёные
- [x] Менеджер станции 1 стоит 4K. После найма станция крутит циклы сама, «Тапни!» исчезает
- [x] Нанять менеджера закрытой станции нельзя (кнопка «Открой станцию»)
- [x] После первого менеджера в HUD появляется доход/сек, равный `доход за цикл / время цикла` автоматизированных станций
- [x] Апгрейд «Шаурма классик ×3» утраивает доход за цикл станции 1, «Все станции ×2» удваивает доход всех. Доход/сек в HUD меняется соответственно
- [x] Купленный апгрейд пропадает из списка, список отсортирован по цене
- [x] Тексты на ru/en
- [x] PROGRESS.md обновлён

## Ручная проверка в Unity (для меня)
1. Переключись в редактор и выбери `ShaurmaTycoon → Build Game UI` (подтвердить пересборку).
2. Play. Тапай станцию 1 до 4K, затем вкладка «Менеджеры» → «Нанять». Станция работает сама, в HUD появился доход/сек.
3. Вкладка «Улучшения» → «Шаурма классик: доход ×3» за 1K: доход за цикл в строке станции ×3, доход/сек ×3.
4. Проверь, что у закрытой станции менеджера нанять нельзя.

## НЕ делать на этом этапе
- Сохранения менеджеров и апгрейдов (этап 06)
- Офлайн-доход (этап 07)
- Анимации, звуки, клиенты (этап 05)
- Престиж-множитель и рекламный буст в формуле дохода (этапы 08–10)

## Риски / вопросы ко мне (решено без вопросов — скажи, если не так)
- **Цены и состав 20 апгрейдов в GDD не заданы.** Ставлю предварительный набор, тюнинг — на этапе 11:
  - 3 круга «станция ×3» по каждой из 6 станций, цена = base cost × 250 / × 25 000 / × 2 500 000;
  - 2 апгрейда «все станции ×2» за 500M и 50B.
- **ManagerModel из PLAN.md не нужен:** «нанят» = `StationModel.IsAutomated`. Отдельные данные менеджера (имена, бонусы) в GDD не описаны.
- Имён у менеджеров нет (в GDD их нет). Строка: «Менеджер • <станция>».
