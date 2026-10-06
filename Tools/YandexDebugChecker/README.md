# Yandex Games Debug Checker

Неофициальный чекер требований Яндекс Игр перед модерацией: https://github.com/Nioris/yandex-games-debug-checker (MIT, см. `LICENSE`).

- Версия: public release v1.1.0, коммит `f86d4ebd1d17f92911ff64b373286fc8d85aec8e`.
- SHA-256 `debugcheck.js`: `adb1ca16e9b3172712d5eda9b64d6fd6792b5e00e85102a0bf0a2390b3239e00`.
- Проверен 2026-10-06: сеть только XHR к своему origin (`index.html`, подключённые `<script>`/`<link>`), без eval; обёртки над `YaGames.init`, `LoadingAPI.ready`, `GameplayAPI.start`, `showFullscreenAdv`, `showRewardedVideo` и `environment.i18n.lang` только записывают время и вызывают оригинал.

Лежит вне `Assets`, чтобы Unity не импортировал его и он не попал в релиз. В билд его кладёт только меню
`ShaurmaTycoon/Release/Build Yandex Debug (checker)` → `Builds/YandexDebug` (тег вставляется сразу после `/sdk.js`).
Релизная сборка падает, если в `Builds/Yandex` или шаблоне найдётся `debugcheck`.

Обновить: заменить `debugcheck.js` свежим из репозитория, просмотреть дифф, обновить коммит и хеш выше.

Как пользоваться — `Docs/Release/YANDEX_RELEASE.md`, раздел «Debug Checker».
