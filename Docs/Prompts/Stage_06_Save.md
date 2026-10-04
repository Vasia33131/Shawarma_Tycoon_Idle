# Этап 06 — Сохранения

## Цель
Прогресс переживает перезапуск. Сохраняются:
- монеты;
- уровни станций, менеджеры, текущий цикл;
- купленные улучшения;
- время сохранения (нужно офлайн-доходу на этапе 07).

Автосейв идёт каждые 10 секунд, а также при потере фокуса, паузе и выходе. Старое сохранение без новых полей и испорченное сохранение не ломают игру.

## Контекст
- Уже готово:
  - `Platform/IPlatformService` (`Save(json, cb)`, `Load(cb)`, `ServerTimeUtc`), `Platform/EditorPlatformService` (PlayerPrefs);
  - `Core/GameBootstrap` (порядок: платформа → Loc → загрузка сцены), `Core/GameSceneInstaller` (создаёт `StationController`, `ManagerController`, `UpgradeService`);
  - `Economy/Wallet.SetCoins`, `Core/GameEvents`.
- PLAN этап 06:
  - `SaveData` с версией схемы, `SaveService` (автосейв каждые 10 с + при паузе/фокусе), `ISaveStorage` → PlayerPrefs в редакторе;
  - JSON через `JsonUtility`, словари хранить списками.
- Ограничения:
  - платформа только через `IPlatformService` (хранилище — обёртка над ним);
  - логика сохранения тестируется;
  - Update только в `TickManager` (события фокуса — через `GameEvents`).

## Файлы
- Создать (Save/):
  - `SaveData.cs` — версия, монеты, время сохранения UTC (ticks), станции, купленные улучшения;
  - `StationSave.cs` — id, уровень, менеджер, прогресс, идёт ли цикл;
  - `ISaveStorage.cs`, `PlatformSaveStorage.cs` (через `IPlatformService`);
  - `ISaveSerializer.cs`, `JsonSaveSerializer.cs` (`JsonUtility`);
  - `SaveMigrator.cs` — нормализация (null-списки, неверные значения) и миграции по версии;
  - `ISaveParticipant.cs`, `CitySaveParticipant.cs` — снимок и восстановление монет, станций, улучшений;
  - `SaveService.cs` — загрузка, участники, автосейв (`ITickable`), сохранение по событиям приложения.
- Создать: `Core/ApplicationEvents.cs` — MonoBehaviour: `OnApplicationFocus`/`Pause`/`Quit` → `GameEvents`.
- Создать: `Editor/SaveMenu.cs` — меню `ShaurmaTycoon/Save/`: «Print Save», «Delete Save», «Write Legacy Test Save».
- Изменить:
  - `StationModel` — `Restore(level, automated, progress, running)`;
  - `UpgradeService` — `Restore(ids)`, `CollectPurchasedIds(list)`;
  - `GameEvents` — `ApplicationFocusChanged`, `ApplicationPaused`, `ApplicationQuitting`;
  - `GameBootstrap` — `ApplicationEvents`, `SaveService`, загрузка сейва до загрузки сцены Game;
  - `GameSceneInstaller` — регистрация `CitySaveParticipant`;
  - `EditorPlatformService` — публичный ключ сейва для меню.
- Тесты:
  - `CitySaveParticipantTests`, `SaveServiceTests`, `SaveMigratorTests` (чистая логика);
  - `JsonSaveSerializerTests` (только Unity: JsonUtility).

## Задачи (по порядку, каждая проверяемая)
1. [x] `SaveData`, `StationSave`, `SaveMigrator` + тесты.
2. [x] `StationModel.Restore`, `UpgradeService.Restore`/`CollectPurchasedIds` + тесты.
3. [x] `CitySaveParticipant` + тесты (полный круг «снимок → восстановление»).
4. [x] `ISaveStorage`/`PlatformSaveStorage`, `ISaveSerializer`/`JsonSaveSerializer` + тесты.
5. [x] `SaveService` + `ApplicationEvents` + `GameEvents` + тесты (автосейв по таймеру и по потере фокуса, испорченный JSON → новая игра).
6. [x] Подключение в `GameBootstrap` / `GameSceneInstaller`.
7. [x] `SaveMenu`.
8. [x] Обновить `PROGRESS.md`.

## Критерии приёмки
- [x] Компилируется, консоль Unity без ошибок и warnings
- [x] EditMode-тесты зелёные
- [x] Закрыл/открыл (Stop → Play) — монеты, уровни, менеджеры, улучшения, прогресс цикла на месте
- [x] Сохранение через ≤ 10 с игры и сразу при потере фокуса окна
- [x] «Write Legacy Test Save» (JSON без новых полей) → Play: игра запускается, монеты из сейва, остальное по умолчанию
- [x] Испорченный JSON → предупреждение в консоли, новая игра, без исключений
- [x] PROGRESS.md обновлён

## Ручная проверка в Unity (для меня)
1. Play, накопи, купи уровни, найми менеджера, купи улучшение. Подожди 10 с → Stop → Play: всё на месте.
2. `ShaurmaTycoon → Save → Print Save` — в консоли JSON сейва.
3. `ShaurmaTycoon → Save → Write Legacy Test Save` → Play: 500 монет, остальное по умолчанию.
4. `ShaurmaTycoon → Save → Delete Save` → Play: новая игра.

## НЕ делать на этом этапе
- Начисление офлайн-дохода (этап 07) — только сохраняем время
- Облачные сохранения Яндекса (этап 09)
- Престиж и города в сейве (этап 08)

## Риски / вопросы ко мне (решено без вопросов)
- **Автосейв перезапишет испорченное сохранение** через 10 с — резервную копию не делаю, игра просто начнётся заново (в консоли предупреждение). Резерв можно добавить на этапе 09 вместе с облаком.
- Станции и улучшения сохраняются по **id**: переименование id в конфиге = потеря прогресса этой станции. Названия (nameKey) менять можно.
- `TickManager` и платформа живут вне сцен, поэтому сохранение при выходе работает из `ApplicationEvents` на объекте TickManager.
