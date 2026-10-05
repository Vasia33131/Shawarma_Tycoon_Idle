// Мост Unity <-> Yandex Games SDK. Использует глобальные ysdk / ygGameInstance из WebGL-шаблона YandexGames.
// Ответы на асинхронные вызовы уходят в C#: SendMessage('YandexBridge', 'OnResult', 'id|status|payload'), id 0 — события.
mergeInto(LibraryManager.library,
{
	YB_Init: function (id)
	{
		window.ybReply = function (rid, status, payload)
		{
			if (typeof ygGameInstance !== 'undefined' && ygGameInstance)
				ygGameInstance.SendMessage('YandexBridge', 'OnResult', rid + '|' + status + '|' + (payload || ''));
		};
		window.ybIsLocal = function ()
		{
			var h = window.location.hostname;
			return h === 'localhost' || h === '127.0.0.1' || h === '' || h.endsWith('.local');
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
		var lang = 'ru';
		try
		{
			if (typeof ysdk !== 'undefined' && ysdk && ysdk.environment && ysdk.environment.i18n)
				lang = ysdk.environment.i18n.lang;
			else if (navigator.language)
				lang = navigator.language;
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
		if (typeof ysdk === 'undefined' || !ysdk)
		{
			// Локальная проверка билда без SDK: награда выдаётся только на localhost.
			window.ybReply(id, window.ybIsLocal() ? 'ok' : 'fail', '');
			return;
		}
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
		if (typeof ysdk === 'undefined' || !ysdk)
		{
			window.ybReply(id, window.ybIsLocal() ? 'ok' : 'fail', '');
			return;
		}
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
