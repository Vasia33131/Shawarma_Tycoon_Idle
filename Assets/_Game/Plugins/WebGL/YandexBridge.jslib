// Мост Unity <-> Yandex Games SDK. Использует глобальные ysdk / ygGameInstance из WebGL-шаблона YandexGames.
// Ответы на асинхронные вызовы уходят в C#: SendMessage('YandexBridge', 'OnResult', 'id|status|payload'), id 0 — события.
mergeInto(LibraryManager.library,
{
	YB_Init: function (id)
	{
		// YB_Init зовётся из Awake сцены Boot — раньше, чем шаблон получит ygGameInstance (промис createUnityInstance).
		// Ответы до этого момента копим и отправляем, как только экземпляр появится: иначе ответ «нет SDK» или
		// быстрый getPlayer терялись, и игра стартовала только по таймауту 10 с и без облачных сохранений.
		var queue = [];
		var flushTimer = null;
		function ready() { return typeof ygGameInstance !== 'undefined' && ygGameInstance; }
		function flush()
		{
			if (!ready()) return;
			clearInterval(flushTimer);
			flushTimer = null;
			var pending = queue;
			queue = [];
			for (var i = 0; i < pending.length; i++)
				ygGameInstance.SendMessage('YandexBridge', 'OnResult', pending[i]);
		}
		window.ybReply = function (rid, status, payload)
		{
			var message = rid + '|' + status + '|' + (payload || '');
			if (ready() && queue.length === 0)
			{
				ygGameInstance.SendMessage('YandexBridge', 'OnResult', message);
				return;
			}
			queue.push(message);
			if (flushTimer === null)
				flushTimer = setInterval(flush, 50);
		};
		window.ybState = { player: null, payments: null, tokens: {} };

		if (typeof ysdk === 'undefined' || !ysdk)
		{
			window.ybReply(id, 'nosdk', '');
			return;
		}

		try
		{
			ysdk.on('game_api_pause', function () { window.ybReply(0, 'pause', ''); });
			ysdk.on('game_api_resume', function () { window.ybReply(0, 'resume', ''); });
		}
		catch (e) { console.warn('YB: pause events unavailable', e); }

		document.addEventListener('visibilitychange', function () { window.ybReply(0, document.hidden ? 'blur' : 'focus', ''); });
		// Перезагрузка и закрытие страницы: сейв уходит синхронно в localStorage, пока страница ещё жива.
		window.addEventListener('pagehide', function () { window.ybReply(0, 'blur', ''); });
		// Возврат «назад/вперёд» из кэша страницы: снимаем паузу, поставленную на pagehide (требование 1.14).
		window.addEventListener('pageshow', function (e) { if (e.persisted && !document.hidden) window.ybReply(0, 'focus', ''); });
		// Alt+Tab и клик в другое окно не скрывают вкладку — звук глушим и по потере фокуса окна (требование 1.3).
		// Тап по игре после блюра тоже возвращает фокус (событие focus в iframe приходит не всегда).
		var windowBlurred = false;
		function setWindowFocus(focused)
		{
			if (windowBlurred !== focused) return;
			windowBlurred = !focused;
			window.ybReply(0, focused ? 'wfocus' : 'wblur', '');
		}
		window.addEventListener('blur', function () { setWindowFocus(false); });
		window.addEventListener('focus', function () { setWindowFocus(true); });
		window.addEventListener('pointerdown', function () { setWindowFocus(true); });

		ysdk.getPlayer({ scopes: false }).then(function (p)
		{
			window.ybState.player = p;
			window.ybReply(id, 'ok', '');
		}).catch(function (e)
		{
			console.warn('YB: getPlayer failed', e);
			window.ybReply(id, 'fail', '');
		});
	},

	YB_GetLanguage: function ()
	{
		// Язык — только из SDK (environment.i18n.lang): язык браузера и системы не используются.
		// Без SDK (локальный запуск) — пустая строка, игра берёт русский.
		var lang = '';
		try
		{
			if (typeof ysdk !== 'undefined' && ysdk && ysdk.environment && ysdk.environment.i18n && ysdk.environment.i18n.lang)
				lang = String(ysdk.environment.i18n.lang);
		}
		catch (e) { }
		var size = lengthBytesUTF8(lang) + 1;
		var buffer = _malloc(size);
		stringToUTF8(lang, buffer, size);
		return buffer;
	},

	YB_GetServerTimeMs: function ()
	{
		try
		{
			if (typeof ysdk !== 'undefined' && ysdk)
				return ysdk.serverTime();
		}
		catch (e) { }
		return Date.now();
	},

	YB_GameReady: function ()
	{
		try
		{
			if (typeof ysdk !== 'undefined' && ysdk && ysdk.features && ysdk.features.LoadingAPI)
				ysdk.features.LoadingAPI.ready();
		}
		catch (e) { console.warn('YB: LoadingAPI.ready failed', e); }
	},

	YB_SetGameplay: function (active)
	{
		try
		{
			if (typeof ysdk !== 'undefined' && ysdk && ysdk.features && ysdk.features.GameplayAPI)
			{
				if (active) ysdk.features.GameplayAPI.start();
				else ysdk.features.GameplayAPI.stop();
			}
		}
		catch (e) { console.warn('YB: GameplayAPI failed', e); }
	},

	YB_GetCatalog: function (id)
	{
		if (typeof ysdk === 'undefined' || !ysdk) { window.ybReply(id, 'fail', ''); return; }
		function clean(v) { return String(v == null ? '' : v).replace(/[=;|]/g, ''); }
		ysdk.getPayments({ signed: false }).then(function (payments)
		{
			window.ybState.payments = payments;
			return payments.getCatalog();
		}).then(function (products)
		{
			var items = [];
			for (var i = 0; i < products.length; i++)
				items.push(clean(products[i].id) + '=' + clean(products[i].priceValue) + '=' + clean(products[i].priceCurrencyCode));
			window.ybReply(id, 'ok', items.join(';'));
		}).catch(function (e)
		{
			console.warn('YB: getCatalog failed', e);
			window.ybReply(id, 'fail', '');
		});
	},

	YB_LocalSet: function (keyPtr, valuePtr)
	{
		try { window.localStorage.setItem(UTF8ToString(keyPtr), UTF8ToString(valuePtr)); }
		catch (e) { console.warn('YB: localStorage write failed', e); }
	},

	YB_LocalGet: function (keyPtr)
	{
		var value = '';
		try { value = window.localStorage.getItem(UTF8ToString(keyPtr)) || ''; }
		catch (e) { }
		var size = lengthBytesUTF8(value) + 1;
		var buffer = _malloc(size);
		stringToUTF8(value, buffer, size);
		return buffer;
	},

	YB_LoadCloud: function (id)
	{
		var player = window.ybState && window.ybState.player;
		if (!player) { window.ybReply(id, 'fail', ''); return; }
		player.getData(['save']).then(function (data)
		{
			window.ybReply(id, 'ok', (data && typeof data.save === 'string') ? data.save : '');
		}).catch(function (e)
		{
			console.warn('YB: getData failed', e);
			window.ybReply(id, 'fail', '');
		});
	},

	YB_SaveCloud: function (id, jsonPtr)
	{
		var player = window.ybState && window.ybState.player;
		if (!player) { window.ybReply(id, 'fail', ''); return; }
		player.setData({ save: UTF8ToString(jsonPtr) }, false).then(function ()
		{
			window.ybReply(id, 'ok', '');
		}).catch(function (e)
		{
			console.warn('YB: setData failed', e);
			window.ybReply(id, 'fail', '');
		});
	},

	YB_ShowRewarded: function (id)
	{
		// Без SDK награды нет: rewarded только настоящий (требования 1.16, 4.1).
		if (typeof ysdk === 'undefined' || !ysdk) { window.ybReply(id, 'fail', ''); return; }
		var rewarded = false, done = false;
		function finish(status) { if (!done) { done = true; window.ybReply(id, status, ''); } }
		ysdk.adv.showRewardedVideo({
			callbacks: {
				onRewarded: function () { rewarded = true; },
				onClose: function () { finish(rewarded ? 'ok' : 'cancel'); },
				onError: function (e) { console.warn('YB: rewarded error', e); finish('fail'); }
			}
		});
	},

	YB_ShowInterstitial: function (id)
	{
		if (typeof ysdk === 'undefined' || !ysdk) { window.ybReply(id, 'ok', ''); return; }
		var done = false;
		function finish(status) { if (!done) { done = true; window.ybReply(id, status, ''); } }
		ysdk.adv.showFullscreenAdv({
			callbacks: {
				onClose: function (wasShown) { finish(wasShown ? 'ok' : 'skipped'); },
				onError: function (e) { console.warn('YB: interstitial error', e); finish('fail'); }
			}
		});
	},

	YB_SetLeaderboardScore: function (namePtr, score)
	{
		if (typeof ysdk === 'undefined' || !ysdk) return;
		var name = UTF8ToString(namePtr);
		ysdk.isAvailableMethod('leaderboards.setScore').then(function (available)
		{
			if (available)
				return ysdk.leaderboards.setScore(name, Math.floor(score));
		}).catch(function (e) { console.warn('YB: setScore failed', e); });
	},

	YB_Purchase: function (id, productPtr)
	{
		var product = UTF8ToString(productPtr);
		// Оплата только через SDK (требование 1.4): без него покупка не проходит.
		if (typeof ysdk === 'undefined' || !ysdk) { window.ybReply(id, 'fail', ''); return; }
		ysdk.getPayments({ signed: false }).then(function (payments)
		{
			window.ybState.payments = payments;
			return payments.purchase({ id: product });
		}).then(function (purchase)
		{
			window.ybState.tokens[product] = purchase.purchaseToken;
			window.ybReply(id, 'ok', '');
		}).catch(function (e)
		{
			console.warn('YB: purchase failed', e);
			window.ybReply(id, 'fail', '');
		});
	},

	YB_GetPurchases: function (id)
	{
		if (typeof ysdk === 'undefined' || !ysdk) { window.ybReply(id, 'ok', ''); return; }
		ysdk.getPayments({ signed: false }).then(function (payments)
		{
			window.ybState.payments = payments;
			return payments.getPurchases();
		}).then(function (purchases)
		{
			var ids = [];
			for (var i = 0; i < purchases.length; i++)
			{
				window.ybState.tokens[purchases[i].productID] = purchases[i].purchaseToken;
				ids.push(purchases[i].productID);
			}
			window.ybReply(id, 'ok', ids.join(','));
		}).catch(function (e)
		{
			console.warn('YB: getPurchases failed', e);
			window.ybReply(id, 'fail', '');
		});
	},

	YB_ConsumePurchase: function (productPtr)
	{
		var product = UTF8ToString(productPtr);
		var state = window.ybState;
		if (!state || !state.payments || !state.tokens[product]) return;
		var token = state.tokens[product];
		state.payments.consumePurchase(token).then(function ()
		{
			delete state.tokens[product];
		}).catch(function (e) { console.warn('YB: consumePurchase failed', e); });
	}
});
