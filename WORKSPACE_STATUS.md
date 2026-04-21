# OsNewGen Workspace Status

Этот файл нужен как единая точка входа по проекту:
- краткая карта структуры репозитория;
- основные рабочие зоны;
- автоматический снимок текущего состояния;
- журнал локальных push-обновлений.

## Root Structure
- `.git`  
  Git-история и служебные данные репозитория.
- `doc`  
  Документация и сопутствующие текстовые материалы проекта.
- `project`  
  Исходники, пакеты и собираемое приложение.
- `related projects`  
  Связанные вспомогательные проекты и внешние зависимости, лежащие рядом.
- `.gitignore`  
  Правила исключения локальных/runtime-файлов из git.
- `README.md`  
  Общая справка по репозиторию.
- `WORKSPACE_STATUS.md`  
  Этот файл: карта проекта и журнал обновлений.

## Key Project Zones
- `project/OsEngine`  
  Основной WPF-монолит приложения.
- `project/OsEngine/Robots`  
  Торговые роботы и фабрики стратегий.
- `project/OsEngine/Robots/TrigonumCustom`  
  Пользовательские и рабочие боты команды, включая `MeanReverse`.
- `project/OsEngine/Market`  
  Серверы, коннекторы, тестер, исполнение ордеров.
- `project/OsEngine/OsTrader`  
  UI и менеджмент `Bot Station`, `Tester Light`, вкладок и сохранения ботов.
- `project/OsEngine/Common`  
  Общие декорации и reusable-логика стратегий.
- `project/OsEngine/Indicators`  
  Индикаторы, включая `TrigonumCustom`.
- `project/OsEngine/bin/Debug/Engine`  
  Runtime-состояние приложения: параметры ботов, коннекторы, журналы, сделки.
- `project/packages`  
  NuGet-пакеты проекта.

## Current Working Context
- Основная ветка разработки: `dev`
- Основной runtime-сценарий: `Tester Light` и `Bot Station Light`
- Ключевой исследуемый бот последних правок: `MRZScoreNatrGrid`
- Важная цель последних работ: приблизить поведение `Tester Light` к `Bot Station Light`, особенно по сеткам, фильтрам и отменам ордеров.

## Auto Snapshot
<!-- AUTO_SNAPSHOT_START -->
- Updated: 2026-04-21 14:01:20
- Branch: `dev`
- HEAD before push: `5f8289b`
- Pending changes before auto-log:

`	ext
 M project/OsEngine/Common/MeanReverseGrid.cs
 M project/OsEngine/Robots/TrigonumCustom/MeanReversion/LrZZAtrTakeSmaEntr.cs
 M project/OsEngine/Robots/TrigonumCustom/MeanReversion/MRZAtrRrDdr.cs
 M project/OsEngine/Robots/TrigonumCustom/MeanReversion/MRZScoreAtrRR.cs
 M project/OsEngine/Robots/TrigonumCustom/MeanReversion/MRZScoreAtrTakeSMA.cs
 M project/OsEngine/Robots/TrigonumCustom/MeanReversion/MeanReversion1Fix.cs
 M project/OsEngine/Robots/TrigonumCustom/MeanReversion/MeanReversionZScore.cs
 M update_osengine_robots_light_from_github.bat
`"
    "
    
- [D] .git
- [D] doc
- [D] project
- [D] related projects
- [F] .gitignore
- [F] LICENSE
- [F] License_ru.pdf
- [F] nuget.exe
- [F] push_dev_with_log.bat
- [F] push_git_with_log.bat
- [F] push_git_with_log.ps1
- [F] push_main_from_dev.bat
- [F] push_main_from_dev.ps1
- [F] README.md
- [F] update_osengine_robots_light_from_github.bat
- [F] update_osengine_robots_light_from_github.log
- [F] WORKSPACE_STATUS.md
- [F] читаем_маст_рид.txt

### project Snapshot
- [D] project/OsEngine
- [D] project/packages
- [F] project/OsEngine.sln

### project/OsEngine Snapshot
- [D] project/OsEngine/Alerts
- [D] project/OsEngine/bin
- [D] project/OsEngine/Candles
- [D] project/OsEngine/Charts
- [D] project/OsEngine/Common
- [D] project/OsEngine/Entity
- [D] project/OsEngine/Images
- [D] project/OsEngine/Indicators
- [D] project/OsEngine/Journal
- [D] project/OsEngine/Language
- [D] project/OsEngine/Layout
- [D] project/OsEngine/Logging
- [D] project/OsEngine/lua
- [D] project/OsEngine/Market
- [D] project/OsEngine/obj
- [D] project/OsEngine/OsConverter
- [D] project/OsEngine/OsData
- [D] project/OsEngine/OsMiner
- [D] project/OsEngine/OsOptimizer
- [D] project/OsEngine/OsTrader
- [D] project/OsEngine/PrimeSettings
- [D] project/OsEngine/Properties
- [D] project/OsEngine/Resources
- [D] project/OsEngine/Robots
- [D] project/OsEngine/Vendors
- [F] project/OsEngine/App.config
- [F] project/OsEngine/App.xaml
- [F] project/OsEngine/App.xaml.cs
- [F] project/OsEngine/MainWindow.xaml
- [F] project/OsEngine/MainWindow.xaml.cs
- [F] project/OsEngine/OsEngine.csproj
- [F] project/OsEngine/OsEngine.csproj.DotSettings
- [F] project/OsEngine/OsLogo.ico
- [F] project/OsEngine/packages.config
<!-- AUTO_SNAPSHOT_END -->

## Push History
<!-- PUSH_LOG_START -->
- 2026-04-21 14:01:20 | branch `dev` | message: `Изменение грида на всех MR`
  Files before commit:
  -  M project/OsEngine/Common/MeanReverseGrid.cs
  -  M project/OsEngine/Robots/TrigonumCustom/MeanReversion/LrZZAtrTakeSmaEntr.cs
  -  M project/OsEngine/Robots/TrigonumCustom/MeanReversion/MRZAtrRrDdr.cs
  -  M project/OsEngine/Robots/TrigonumCustom/MeanReversion/MRZScoreAtrRR.cs
  -  M project/OsEngine/Robots/TrigonumCustom/MeanReversion/MRZScoreAtrTakeSMA.cs
  -  M project/OsEngine/Robots/TrigonumCustom/MeanReversion/MeanReversion1Fix.cs
  -  M project/OsEngine/Robots/TrigonumCustom/MeanReversion/MeanReversionZScore.cs
  -  M update_osengine_robots_light_from_github.bat
- 2026-04-20 17:54:14 | branch `dev` | message: `Узнал, что пушил лишь дев, а на мейне торговая, переношу`
  Files before commit:
  -  M .gitignore
  -  M WORKSPACE_STATUS.md
  -  M update_osengine_robots_light_from_github.bat
- 2026-04-20 17:52:42 | branch `dev` | message: `n `
  Files before commit:
  -  M .gitignore
  -  M update_osengine_robots_light_from_github.bat
- 2026-04-20 15:35:32 | branch `dev` | message: `Обновление readme`
  Files before commit:
  -  M README.md
- 2026-04-20 15:26:56 | branch `dev` | message: `Workspace update 2026-04-20 15:26:56`
  Files before commit:
  -  M .gitignore
  -  M project/OsEngine/Charts/CandleChart/WinFormsChartPainter.cs
  -  M project/OsEngine/Charts/ClusterChart/ChartClusterPainter.cs
  -  M project/OsEngine/Common/CanEnterByEmaDecoration.cs
  -  M project/OsEngine/Common/Change24Decoration.cs
  -  M project/OsEngine/Common/VolatileStopDecoration.cs
  -  M project/OsEngine/Market/Connectors/ConnectorCandles.cs
  -  M project/OsEngine/Market/Servers/Tester/TesterServerUi.xaml.cs
  -  M project/OsEngine/OsEngine.csproj
  -  M project/OsEngine/OsTrader/Gui/BotTabsPainter.cs
  -  M project/OsEngine/OsTrader/OsTraderMaster.cs
  -  M project/OsEngine/Robots/TrigonumCustom/BotPanelSimple.cs
- 2026-04-20 15:16:31 | branch `dev` | message: `test commit `
  Files before commit:
  -  M .gitignore
  -  M project/OsEngine/Charts/CandleChart/WinFormsChartPainter.cs
  -  M project/OsEngine/Charts/ClusterChart/ChartClusterPainter.cs
  -  M project/OsEngine/Common/CanEnterByEmaDecoration.cs
  -  M project/OsEngine/Common/Change24Decoration.cs
  -  M project/OsEngine/Common/VolatileStopDecoration.cs
  -  M project/OsEngine/Market/Connectors/ConnectorCandles.cs
  -  M project/OsEngine/Market/Servers/Tester/TesterServerUi.xaml.cs
  -  M project/OsEngine/OsEngine.csproj
  -  M project/OsEngine/OsTrader/Gui/BotTabsPainter.cs
  -  M project/OsEngine/OsTrader/OsTraderMaster.cs
  -  M project/OsEngine/Robots/TrigonumCustom/BotPanelSimple.cs
- 2026-04-20 15:16:14 | branch `dev` | message: `test commit `
  Files before commit:
  -  M .gitignore
  -  M project/OsEngine/Charts/CandleChart/WinFormsChartPainter.cs
  -  M project/OsEngine/Charts/ClusterChart/ChartClusterPainter.cs
  -  M project/OsEngine/Common/CanEnterByEmaDecoration.cs
  -  M project/OsEngine/Common/Change24Decoration.cs
  -  M project/OsEngine/Common/VolatileStopDecoration.cs
  -  M project/OsEngine/Market/Connectors/ConnectorCandles.cs
  -  M project/OsEngine/Market/Servers/Tester/TesterServerUi.xaml.cs
  -  M project/OsEngine/OsEngine.csproj
  -  M project/OsEngine/OsTrader/Gui/BotTabsPainter.cs
  -  M project/OsEngine/OsTrader/OsTraderMaster.cs
  -  M project/OsEngine/Robots/TrigonumCustom/BotPanelSimple.cs
Журнал push-обновлений пока пуст. Первый запуск `push_git_with_log.bat` добавит сюда запись.
<!-- PUSH_LOG_END -->







