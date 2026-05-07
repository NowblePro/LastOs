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

## 2026-05-07 | local
- Added recovery-volume mode to `MRZScoreNatrGrid`: after a losing completed series, the next `N` series can start with first-level volume multiplied by `X`.
- Recovery series count is consumed only after the first actual fill of the boosted series; a grid that was built but never filled does not burn the counter.

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
