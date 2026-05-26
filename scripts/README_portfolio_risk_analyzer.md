# Robot Portfolio Risk Analyzer

Python-инструмент для анализа портфеля крипто-роботов с усреднением позиций.

Скрипт считает:
- индивидуальную статистику роботов;
- корреляции PnL и drawdown;
- совпадение позиций во времени;
- совпадение усреднений;
- портфельную просадку и peak exposure;
- Risk Similarity Score между роботами;
- рекомендованное разбиение роботов на несколько портфелей;
- stress-test сценарии.

## Требования

Python `3.11+`.

Нужные библиотеки:

```powershell
pip install pandas numpy matplotlib seaborn
```

`networkx` не требуется: разбиение по портфелям сделано greedy-алгоритмом.

## Запуск по CSV

Один файл:

```powershell
python .\scripts\analyze_robot_portfolio.py --input data\trades.csv --portfolios 4 --timeframe 1H --output reports
```

Несколько файлов из папки:

```powershell
python .\scripts\analyze_robot_portfolio.py --input_dir data\robots --portfolios 4 --timeframe 1H --output reports
```

## Запуск по последнему OsEngine Tester Light

Пример для локальной OSA:

```powershell
python .\scripts\analyze_robot_portfolio.py `
  --osengine_log_dir ".\project\OsEngine\bin\Debug\Engine\Log" `
  --latest_osengine `
  --portfolios 4 `
  --timeframe 1H `
  --output reports\portfolio_latest
```

Если нужно взять конкретных роботов:

```powershell
python .\scripts\analyze_robot_portfolio.py `
  --osengine_log_dir ".\project\OsEngine\bin\Debug\Engine\Log" `
  --latest_osengine `
  --robots "aave,avax,BNB,inj,Sol,Testt,321321321312,dasasdas,TLClone Natr DOT L2,TLClone Natr ICP L" `
  --portfolios 4 `
  --timeframe 1H `
  --output reports\portfolio_latest_10
```

## Выходные файлы

Скрипт сохраняет в `reports`:

- `robot_stats.csv`
- `pnl_correlation_matrix.csv`
- `drawdown_correlation_matrix.csv`
- `overlap_matrix.csv`
- `averaging_overlap_matrix.csv`
- `risk_similarity_matrix.csv`
- `recommended_portfolios.csv`
- `portfolio_risk_summary.csv`
- `stress_test_results.csv`

Графики сохраняются в `reports/charts`:

- `portfolio_equity_curve.png`
- `portfolio_drawdown_curve.png`
- `heatmap_pnl_correlation.png`
- `heatmap_drawdown_correlation.png`
- `heatmap_position_overlap.png`
- `heatmap_averaging_overlap.png`
- `heatmap_risk_similarity.png`
- `timeline_active_robots.png`
- `timeline_robots_in_averaging_mode.png`

## Важные ограничения

Если во входных данных нет `fee`, net PnL считается равным PnL.

Если нет `margin_used`, margin-метрики будут помечены как недоступные.

Если нет `qty` или цен, exposure считается частично или не считается.

Для OsEngine-логов parser берёт закрытые позиции из блоков `Позиция закрыта`.
