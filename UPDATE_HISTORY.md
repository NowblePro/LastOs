# Update History

Короткая история значимых обновлений по проекту.

## Rules
- Этот файл ведётся как краткий журнал изменений верхнего уровня.
- Формат записи: дата, статус, краткие пункты по сути.
- `committed` = изменение есть в git-истории.
- `local` = изменение пока только в рабочем дереве и собрано локально.
- После каждого заметного обновления сюда нужно добавлять 1 короткий пункт.

## Entry Template
```text
## YYYY-MM-DD | committed/local
- Краткое описание 1
- Краткое описание 2
```

## Recent Updates

## 2026-05-18 | local
- Restored live `Natr` bot configs from the last valid tester-clone snapshots for `AAVE`, `BNB`, `ICP`, and `XMR L2` after parameter drift in `Grid Size`, `Recovery Volume Multiplier`, `Debug Logging`, and `Ema Filter Reverse`.
- Verified that `Natr Dot L` already matched its tester-clone config; left `Natr DOT L2` unchanged because the latest tester-clone artifacts do not contain a reliable base `Parametrs.txt` snapshot for that bot.

## 2026-05-19 | local
- Added the first `OsEngine.Tests` automation project for `net48` with NUnit-based unit coverage for `MeanReverseVolumeManager`, `PositionStatisticGenerator.GetStabilityScore`, and `BotPanel` parameter loading by saved parameter name.
- Added `scripts/test-code-only.ps1` and `scripts/test-scenarios.ps1` using full Visual Studio MSBuild plus `vstest.console`, because `dotnet test` cannot build the legacy `OsEngine` project due to `ResolveComReference`.
- Added first `ConfigRegression` coverage for `OsTraderMaster` tester-clone connector normalization and included it in `test-code-only.ps1`.
- Added first `Scenario` coverage for `MRZScoreNatrGrid` recovery-threshold logic and consumed-series depth via deterministic reflection-based tests.
- Hardened `test-code-only.ps1` and `test-scenarios.ps1` to stop on failed MSBuild / vstest instead of accidentally running stale test binaries.
- Extracted `MRZScoreNatrGrid` test setup into a reusable helper and expanded scenario coverage with buy/sell grid build, EMA-blocked build, and threshold-unavailable build cases.
- Added lifecycle scenario coverage for `MRZScoreNatrGrid` pending-next-open scheduling and `ClearGrid("Series completed")` state reset plus recovery arming.
- Added core opening-path scenario coverage for `MRZScoreNatrGrid`: immediate level binding, opening-success binding by `SignalTypeOpen`, and fallback binding by awaiting-queue price with recovery consumption on first fill.

## 2026-05-18 | local
- Added `Recovery Loss Level Threshold` to `MRZScoreNatrGrid`: recovery volume boost now arms only after a losing completed series that reached the configured consumed grid depth; `0` keeps the old behavior and reacts to any losing series.
- Fixed tester-clone connector sync to preserve prior valid tester connector state when the source connector file is malformed, and restored the broken `Natr Dot L` source config from its working tester clone.

## 2026-05-07 | local
- Added recovery-volume mode to `MRZScoreNatrGrid`: after a losing completed series, the next `N` series can start with first-level volume multiplied by `X`.
- Recovery series count is consumed only after the first actual fill of the boosted series; a grid that was built but never filled does not burn the counter.
- Replaced the experimental `MarketOHLC` path with `MarketNextOpen` as the candle-based backtest mode for market-style parity research.
- Fixed `MRZScoreNatrGrid` level binding on open so new positions are restored by `SignalTypeOpen` and no longer fall into `opening position is not bound to any grid level`.
- Optimized `Journal -> Closed positions` by removing the heavy manual `O(n^2)` sort.
- Fixed `CanEnterByEmaDecoration` fail-open behavior: with EMA filter enabled, missing EMA context no longer allows entries above EMA.

## 2026-05-06 | local
- `MarketOHLC` убран как основной режим для parity-тестов и заменён на `MarketNextOpen`.
- В `MRZScoreNatrGrid` добавлена миграция старого `OrderType=MarketOHLC` в `MarketNextOpen`.
- Для привязки synthetic/opening-success позиций к grid-level добавлена жёсткая идентификация через `SignalTypeOpen`.
- Во вкладке `Journal -> Closed positions` убрана тяжёлая ручная сортировка `O(n^2)`, заменена на нормальную сортировку списка.

## 2026-05-05 | local
- Добавлен и проверен новый backtest-режим для `MRZScoreNatrGrid`, ориентированный на близость candle-backtest к live market.
- Выполнен разбор расхождений `live vs backtest` по `MyNewBot232` и live-логам `Natr XMR L REV`.
- Пересобран локальный `OsEngine.exe` после внедрения нового режима.

## 2026-04-29 | committed
- Обновлён оптимизатор: убраны лишние колонки, добавлена метрика стабильности, ускорено открытие графиков, убрана лишняя кнопка полного графика.
- Исправлен баг в тестере/журнале, связанный с отображением логов и позиций.

## 2026-04-28 | committed
- Добавлен выбор бумаги через селект в `Tester Light`.
- Расширен debug logging для роботов и заложен шаблон регрессионного теста.
- Добавлен и оформлен регрессионный runbook для пост-апдейт проверок.

## 2026-04-28 | committed
- Выполнен workspace update и зафиксировано состояние рабочей ветки `dev`.
