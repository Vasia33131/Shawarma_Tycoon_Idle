# Этап 00 — Каркас проекта

## Цель
У проекта появляется рабочий скелет: сцены Boot → Game, загрузчик `GameBootstrap`, service locator `Services`, общий `TickManager` и заглушка платформы `EditorPlatformService`. WebGL-настройки и Canvas Scaler готовы и для портрета, и для ландшафта.

## Контекст
- Уже готово: пустой 2D URP-проект на Unity 2022.3.62f1. Есть `Assets/Scenes/SampleScene.unity`, TextMeshPro 3.0.7 и Test Framework 1.1.33. Кода нет.
- Текущие настройки: Color Space = Linear, WebGL Compression = Brotli, Decompression Fallback = выкл., Strip Engine Code = вкл.
- Разделы GDD: §3 «Экран» (верх — HUD, центр — ларёк, низ — вкладки) задаёт раскладку UI под оба режима. Игровых механик на этом этапе нет.
- Ограничения:
  - код лежит в `Assets/_Game/Scripts/`, namespace `ShaurmaTycoon.*`, один файл — один класс;
  - без `FindObjectOfType` и без аллокаций в Update, всё обновление идёт через `TickManager`;
  - платформа доступна только через `IPlatformService`, игровой код не знает про Яндекс;
  - UI должен работать в портрете 1080×1920 и в ландшафте 1920×1080.

## Файлы
- Создать: `Assets/_Game/` с подпапками из PLAN.md: `Scripts/{Core,Economy,Stations,Managers,Upgrades,Customers,Prestige,Offline,Retention,Save,Platform,Ads,UI,Localization}`, `Configs`, `Prefabs`, `Sprites`, `Audio`, `Scenes`.
- Создать: `Assets/_Game/Scripts/ShaurmaTycoon.asmdef` — сборка игрового кода (понадобится EditMode-тестам на этапе 01).
- Создать: `Assets/_Game/Scripts/Core/Services.cs` — статический service locator: `Register<T>`, `Get<T>`, `TryGet<T>`, `Clear`.
- Создать: `Assets/_Game/Scripts/Core/ITickable.cs` — интерфейс `Tick(float dt)`.
- Создать: `Assets/_Game/Scripts/Core/TickManager.cs` — единственный MonoBehaviour с Update. Хранит список ITickable в заранее выделенном List, безопасно добавляет и удаляет подписчиков во время тика, поддерживает `TimeScale` (пригодится для отладки на этапе 11).
- Создать: `Assets/_Game/Scripts/Core/GameEvents.cs` — заготовка статических C#-событий (`OnGamePaused(bool)`).
- Создать: `Assets/_Game/Scripts/Core/GameBootstrap.cs` — точка входа в сцене Boot:
  - создаёт `TickManager` и платформу (`DontDestroyOnLoad`) и регистрирует их в `Services`;
  - ждёт `platform.Init`;
  - пишет в лог `Bootstrap OK`;
  - загружает сцену Game.
- Создать: `Assets/_Game/Scripts/Platform/IPlatformService.cs` — контракт без Яндекс-специфики:
  - `Init(Action onReady)`, `GameReady()`, `Language`;
  - `ShowRewarded(Action<bool>)`, `ShowInterstitial(Action)`;
  - `Save(string json, Action<bool>)`, `Load(Action<string>)`;
  - `ServerTime` (UTC);
  - лидерборд и покупки пока сигнатурами с `TODO(YANDEX)`.
- Создать: `Assets/_Game/Scripts/Platform/EditorPlatformService.cs` — заглушка:
  - реклама сразу «досмотрена»;
  - сохранения в PlayerPrefs;
  - язык `ru`;
  - время — `DateTime.UtcNow`.
- Создать: `Assets/_Game/Scripts/UI/CanvasScalerAdapter.cs` — компонент на Canvas:
  - Scale With Screen Size, референс 1080×1920;
  - переключает `matchWidthOrHeight` при смене ориентации (портрет — 0, ландшафт — 1);
  - проверяет размер экрана событием или редким тиком, без аллокаций.
- Создать: `Assets/_Game/Scripts/Editor/ProjectSetupTool.cs` (+ Editor asmdef) — пункт меню `ShaurmaTycoon/Setup Project`:
  - создаёт сцены `Assets/_Game/Scenes/Boot.unity` (с GameBootstrap) и `Game.unity` (Camera + Canvas с CanvasScalerAdapter + EventSystem);
  - прописывает их в Build Settings (Boot первой);
  - применяет WebGL-настройки.
  Так надёжнее, чем править YAML сцен и ProjectSettings руками.
- Изменить: `ProjectSettings` (через инструмент выше):
  - WebGL Compression = Brotli + Decompression Fallback = вкл. (работает без настройки заголовков на сервере);
  - Strip Engine Code = вкл.;
  - Color Space — см. вопрос ниже;
  - Default Canvas 1080×1920;
  - Run In Background = выкл.
- Изменить: Build Settings — убрать `SampleScene`. Сам файл не удаляем без подтверждения.

## Задачи (по порядку, каждая проверяемая)
1. [x] Создать структуру папок `Assets/_Game/` и runtime-asmdef.
2. [x] `Services`, `ITickable`, `TickManager`, `GameEvents`.
3. [x] `IPlatformService` + `EditorPlatformService`.
4. [x] `GameBootstrap` (Boot → Game, лог `Bootstrap OK`).
5. [x] `CanvasScalerAdapter`.
6. [x] `ProjectSetupTool`: сцены, Build Settings, WebGL-настройки.
7. [x] Запустить пункт меню, проверить Play из Boot, собрать WebGL-билд.
8. [x] Обновить `PROGRESS.md`.

## Критерии приёмки
- [x] Компилируется (проверено Roslyn из Unity 2022.3.62f1 вне редактора; консоль Unity — проверить после импорта)
- [x] Проект открывается, Play из Boot идёт без ошибок, в логе `Bootstrap OK`, после этого загружается Game
- [x] WebGL-билд собирается
- [x] В Build Settings Boot (index 0) и Game (index 1), SampleScene выключена
- [x] В Game View 1080×1920 и 1920×1080 канвас масштабируется корректно (тестовый Text в центре читается)
- [x] В коде нет `FindObjectOfType` и Update, кроме `TickManager`
- [x] PROGRESS.md обновлён

## Ручная проверка в Unity (для меня)
1. Дождись компиляции и выбери меню `ShaurmaTycoon → Setup Project`.
2. Открой `Assets/_Game/Scenes/Boot.unity` и нажми Play. Ожидается лог `Bootstrap OK` и переход на сцену Game без ошибок.
3. В Game View переключи разрешение 1080×1920 ↔ 1920×1080. Тестовый текст должен оставаться читаемым и в пределах экрана.
4. Выбери `File → Build Settings → WebGL → Build`. Билд должен собраться без ошибок.

## НЕ делать на этом этапе
- Экономику, Wallet, форматирование чисел (этап 01)
- Станции, HUD, вкладки (этапы 02–03)
- Сохранения (этап 06), локализацию с таблицами (вводится вместе с первым UI-текстом на этапе 03)
- Интеграцию Яндекса, PluginYG2, jslib (этап 09)
- Установку DOTween (этап 05)

## Риски / вопросы ко мне
1. **Color Space.** Сейчас стоит Linear. Он требует WebGL 2.0 и не запустится на части старых мобильных браузеров. Для 2D-игры на Яндекс Играх рекомендую **Gamma**: максимальная совместимость и визуальной разницы почти нет. Ставим Gamma?
2. **Render pipeline.** Проект создан на URP 2D. Встроенный рендер даёт билд меньше на ~1–3 МБ, но для 2D с плоским UI особой разницы нет. Рекомендую **оставить URP**, чтобы не пересоздавать проект. Ок?
3. **Лишние пакеты.** `com.unity.visualscripting`, `com.unity.collab-proxy` и `com.unity.timeline` нам не нужны и раздувают проект. Удалить их из `manifest.json`?
4. **SampleScene.** Убрать её из Build Settings и удалить файл `Assets/Scenes/SampleScene.unity` вместе с папкой? Или только убрать из Build Settings?
