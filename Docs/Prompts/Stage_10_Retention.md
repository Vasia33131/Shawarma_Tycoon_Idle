# Этап 10 — Удержание

## Цель
Игроку есть зачем возвращаться каждый день и каждые 10 минут: 7-дневный календарь наград (день 7 — скин ларька), 3 задания дня, буст «×2 доход» за рекламу с накоплением до 4 ч и подарок-ящик монет раз в 10 мин. Все сутки и таймеры — по серверному времени платформы.

## Контекст
- Уже готово: `IPlatformService.ServerTimeUtc` (серверное время Яндекса, этап 09), `ShowRewarded`; `AdBillReward` (кулдаун по UTC, защита от перевода часов — 08f); `SaveService` + `ISaveParticipant`; `StationController`/`StationModel.LevelChanged`, `ManagerController.ManagerHired`, `UpgradeService.UpgradePurchased`; `PrestigeState` (репутация); `KioskView.ApplyTheme`; `GameUiBuilder`.
- GDD §8: буст «×2 доход на 4 мин» (rewarded, копить до 4 ч); ящик монет = 5 мин дохода, rewarded, раз в 10 мин, иконка-подарок на экране.
- GDD §9: календарь 7 дней, день 7 — скин ларька; 3 задания дня («купи 50 уровней», «посмотри буст», «найми менеджера») → репутация/монеты. GDD §7: ежедневки и скины не сбрасываются при переезде.
- Ограничения: числа — в `RetentionConfig` (SO); время — только через платформу; без аллокаций в Tick; тексты ru/en; новые системы — `ISaveParticipant`; вью сцены на сервисах Boot — `ISceneBinding`.

## Файлы
- Создать `Scripts/Retention/`: `RetentionConfig`, `DailyRewardCalendar`, `DailyRewardService`, `QuestType`, `QuestTemplate`, `DailyQuests`, `DailyQuestService`, `IncomeBoost`, `BoostService`, `GiftBoxService`, `RewardMath`, `RetentionTracker`.
- Создать `Scripts/Save/RetentionSave.cs`, `QuestSave.cs`.
- Создать `Scripts/UI/`: `RetentionHud`, `DailyRewardPopup`, `DailyRewardTileView`, `QuestsPopup`, `QuestRowView`, `AdOfferPopup`.
- Создать `Tests/EditMode/RetentionTests.cs`.
- Изменить: `SaveData` (+`Retention`), `PrestigeSave`/`PrestigeState`/`PrestigeSaveParticipant`/`PrestigeController` (бонусная репутация), `SaveMigrator`, `GameSceneInstaller`, `GameScreen`, `OfflineIncomePopup` (событие закрытия), `ContentSetup` (`Retention.asset`), `GameUiBuilder`, `SaveMenu`, `DebugMenu`, `strings.txt`, `StringsFileTests`.

## Задачи (по порядку, каждая проверяемая)
1. [x] Логика: календарь, задания, буст, подарок (чистый C#), сейв и миграция.
2. [x] Бонусная репутация за задания не уменьшает будущую награду за переезд.
3. [x] Подключение в `GameSceneInstaller`: множитель буста, учёт действий для заданий, скин.
4. [x] UI: кнопка буста в HUD, кнопки календаря/заданий и подарок на экране ларька, окна календаря, заданий и предложения рекламы; авто-показ календаря после окна офлайн-дохода.
5. [x] Меню: сдвиг суток в сейве, отладка подарка/буста.
6. [x] Строки ru/en, тесты.
7. [x] PROGRESS.md.

## Критерии приёмки
- [ ] Компилируется, консоль Unity без ошибок и warnings
- [x] Подмена даты в сейве (меню `Save/Shift Retention Day −1 d`) двигает календарь на следующий день и выдаёт новые задания (RetentionTests; в Unity не проверено)
- [x] Перевод часов назад не даёт награду повторно (RetentionTests)
- [x] Буст копится до 4 ч, идёт только во время игры, удваивает доход (RetentionTests; в Unity не проверено)
- [x] Подарок раз в 10 мин, только после досмотра рекламы (RetentionTests)
- [x] PROGRESS.md обновлён

## Ручная проверка в Unity (для меня)
1. ShaurmaTycoon/Build Game UI → Play из Boot.
2. Сразу (или после окна «Пока тебя не было») открывается календарь → «Забрать» → монеты летят в HUD; повторно забрать нельзя.
3. Стоп → `ShaurmaTycoon/Save/Shift Retention Day −1 d` → Play → календарь на дне 2, задания новые.
4. «×2» в HUD → реклама (в редакторе сразу) → доход/сек удвоен, таймер 4:00.
5. Подарок на экране ларька → «Открыть» → монеты; таймер 10:00.
6. Купи уровни → прогресс задания растёт → «Забрать».

## НЕ делать на этом этапе
- Туториал-рука, отладочное окно, баланс-прогон (этап 11).
- Коллекция скинов (LiveOps).

## Решения (приняты без вопросов — пользователь сказал «выполни»)
- Сутки — по UTC серверного времени (как купюры за рекламу 08f).
- Пропуск дня не сбрасывает календарь: следующий вход — следующий день. После дня 7 — снова день 1; если скин уже есть, день 7 даёт купюры.
- Буст тратится только во время игры (не офлайн), не влияет на офлайн-доход и курс купюр.
- Награды заданий: монеты (минуты дохода) или репутация; репутация из заданий — «бонусная», не уменьшает награду за переезд.
- Монеты из наград не засчитываются как заработок города (как мешки монет 08d).
