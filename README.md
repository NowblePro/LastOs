# OsNewGen

Рабочая форка `OsEngine` под текущий процесс разработки, бектестов и запуска в реальную торговлю.

Этот `README` описывает именно текущий репозиторий, а не апстрим-проект.

## Назначение

В репозитории ведётся:

- разработка и доработка роботов под `Tester Light` и `Bot Station Light`;
- выравнивание логики `tester/live`;
- поддержка пользовательских mean reversion стратегий;
- подготовка обновлений для сервера через `git`.

## Основные пути

Ниже указаны ключевые пути внутри репозитория и типовые рабочие пути на локальной машине.

### Локальный репозиторий

- Корень репозитория: `C:\Users\user\Desktop\OsNewGen`
- Основной проект: `C:\Users\user\Desktop\OsNewGen\project\OsEngine`
- Основной exe после сборки: `C:\Users\user\Desktop\OsNewGen\project\OsEngine\bin\Debug\OsEngine.exe`

### Важные runtime-папки

- Состояние ботов, настройки, контроллеры сделок: `project\OsEngine\bin\Debug\Engine`
- Исторические данные для тестов: `project\OsEngine\bin\Debug\Data`
- Логи: `project\OsEngine\bin\Debug\Engine\Log`
- Временный hotfix-билд: `project\OsEngine\bin\Debug_hotfix`

### Важные исходники

- Роботы: `project\OsEngine\Robots`
- Пользовательские mean reversion роботы: `project\OsEngine\Robots\TrigonumCustom\MeanReversion`
- Коннекторы и серверы: `project\OsEngine\Market`
- UI и управление ботами: `project\OsEngine\OsTrader`
- Графики: `project\OsEngine\Charts`
- Общие декораторы/фильтры: `project\OsEngine\Common`

### Сопутствующие файлы в корне

- [WORKSPACE_STATUS.md](C:/Users/user/Desktop/OsNewGen/WORKSPACE_STATUS.md) — карта структуры и журнал локальных пушей
- [push_git_with_log.bat](C:/Users/user/Desktop/OsNewGen/push_git_with_log.bat) — локальный bat для коммита и пуша
- [push_git_with_log.ps1](C:/Users/user/Desktop/OsNewGen/push_git_with_log.ps1) — логика bat-скрипта
- [update_osengine_robots_light_from_github.bat](C:/Users/user/Desktop/OsNewGen/update_osengine_robots_light_from_github.bat) — серверный update helper

## Структура репозитория

- `project/`
  - основной исходный код и проект `OsEngine`
- `doc/`
  - документы и вспомогательные материалы
- `related projects/`
  - связанные проекты и внешние артефакты
- `.gitignore`
  - правила исключения локальных и runtime-файлов

## Установка и первый запуск

### 1. Клонирование

```powershell
git clone git@github.com:NowblePro/OsNewGen.git
cd OsNewGen
```

### 2. Сборка

Через Visual Studio или через `MSBuild`.

Пример для PowerShell:

```powershell
msbuild .\project\OsEngine\OsEngine.csproj /p:Configuration=Debug
```

### 3. Запуск

Запускать:

```text
project\OsEngine\bin\Debug\OsEngine.exe
```

### 4. Что важно не перетирать

При обновлении рабочей/серверной версии нельзя бездумно затирать:

- `project\OsEngine\bin\Debug\Engine`
- `project\OsEngine\bin\Debug\Data`

Именно там живут runtime-состояние, настройки ботов, журналы, тестовые наборы и история.

## Git workflow

Текущий процесс:

- рабочая ветка разработки: `dev`
- стабильная ветка для сервера: `main`

### Рекомендуемый цикл

1. Вносить изменения в `dev`
2. Проверять локально
3. Пушить в `dev`
4. После проверки переносить в `main`
5. На сервере делать `git pull` по `main`

### Быстрый локальный пуш

Для локального пуша есть:

- [push_git_with_log.bat](C:/Users/user/Desktop/OsNewGen/push_git_with_log.bat)

Он:

1. Обновляет [WORKSPACE_STATUS.md](C:/Users/user/Desktop/OsNewGen/WORKSPACE_STATUS.md)
2. Показывает `git status`
3. Просит сообщение коммита
4. Делает `commit`
5. Делает `push` в текущую ветку

Важно:

- сам `push_git_with_log.bat` и `push_git_with_log.ps1` не должны пушиться в репозиторий;
- это уже настроено через `.gitignore`.

## Обновление сервера

Рекомендуемая схема:

1. Остановить `OsEngine`
2. Обновить код из `main`
3. Не трогать `Engine` и `Data`
4. Запустить новую сборку

Если сервер работает из git-папки:

```powershell
git checkout main
git pull --ff-only origin main
```

Рекомендуемый серверный путь для git-версии:

- `C:\Users\Administrator\Desktop\OsNewGenGit`

Если старая ручная папка ещё существует, её лучше держать отдельно как backup.

## Ключевые рабочие сценарии

### Tester Light

Используется для:

- одиночного прогона роботов;
- сверки поведения новых фильтров и сеток;
- сравнения с `Bot Station Light`.

### Bot Station Light

Используется для:

- реальной торговли;
- проверки того, насколько логика live близка к `Tester Light`;
- переноса настроенных ботов в `Tester Light`.

## Что уже сделано в этой форке

Ниже не история апстрима, а список того, что было сделано в рамках этой ветки и рабочего чата.

### Инфраструктура и workflow

- [x] Репозиторий переведён на GitHub как основной remote
- [x] Настроена схема `dev -> main -> server pull`
- [x] Добавлен локальный push helper с автологом в `WORKSPACE_STATUS.md`
- [x] Добавлен root-документ [WORKSPACE_STATUS.md](C:/Users/user/Desktop/OsNewGen/WORKSPACE_STATUS.md)
- [x] Подготовлен bat для серверного обновления `robots light`

### Tester Light / Bot Station Light

- [x] Добавлена кнопка переноса ботов из `Bot Station Light` в `Tester Light`
- [x] Исправлено подтягивание бумаг в `Tester Light` без активного коннекта к бирже
- [x] Исправлена привязка tester-бумаг к локальным `.txt` датасетам
- [x] Исправлен сценарий, когда после перезапуска тестера нужно было заново выбирать бумагу
- [x] Добавлена маленькая кнопка `Копия` у каждого бота
- [x] Реализовано создание копии бота с новым именем

### UI и стабильность

- [x] Исправлен краш WinForms Chart при движении мыши по графику через `SafeWinFormsChart`
- [x] Исправлены `NullReference` при ранней инициализации `ZScore`-индикаторов
- [x] Улучшена устойчивость тестового runtime-кэша после пересборок

### Mean Reversion и старые боты

- [x] Исправлена логика rollback уровней в `MeanReversionZScore`
- [x] Закрыт сценарий `Sequence contains no elements` в `ZScoreGrid.Deal`
- [x] Исправлены источники расхождения live/tester в старых mean reversion роботах
- [x] Перенастроена логика открытия/сетки в части `MeanReversionSma2`, `MeanReversion1Fix`, `MRZScoreAtrRR`, `MRZAtrRrDdr`

### Новый робот MRZScoreNatrGrid

- [x] Создан новый робот [MRZScoreNatrGrid.cs](C:/Users/user/Desktop/OsNewGen/project/OsEngine/Robots/TrigonumCustom/MeanReversion/MRZScoreNatrGrid.cs)
- [x] Реализована NATR-сетка с порогом по `z-score`
- [x] Добавлена поддержка `Limit`
- [x] Добавлена поддержка `Market`
- [x] Добавлен `EMA filter`
- [x] Добавлен `Ema Filter Reverse`
- [x] Добавлен `EMA Stop`
- [x] Добавлен `Change24`
- [x] Добавлен `DDR`
- [x] Добавлен `Volatile Stop`
- [x] Добавлен `ZScore Channel TP`
- [x] Разделены `ATR SL`, `RR` и абсолютный `Stop Loss Limit Percent`
- [x] Исправлены проблемы привязки открытия позиции к уровню грида
- [x] Улучшено поведение `Volatile Stop` ближе к live-логике отмен

## Что ещё нужно добить

- [ ] Финально сверить parity `Tester Light` vs `Bot Station Light` на одном и том же отрезке истории
- [ ] Дожать финальную модель поведения `Volatile Stop` в live/tester parity
- [ ] Подготовить отдельный release-flow для сервера без ручных шагов
- [ ] Зафиксировать финальные пресеты параметров для `MRZScoreNatrGrid`

## Практические замечания

- Если после изменений кажется, что бот “не подтянул новую логику”, сначала перезапусти `OsEngine`.
- Для чистой проверки логики нового робота лучше тестировать его отдельно.
- Если цель — parity с реальной торговлей, любые механики массовой отмены ордеров нужно проверять отдельно.

## Лицензия

Смотри:

- [LICENSE](C:/Users/user/Desktop/OsNewGen/LICENSE)
- [License_ru.pdf](C:/Users/user/Desktop/OsNewGen/License_ru.pdf)
