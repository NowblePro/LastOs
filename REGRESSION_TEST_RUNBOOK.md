# OsNewGen Regression Test Runbook

Updated: 2026-04-30

## Purpose

This file is the single source of truth for regression checks in `OsNewGen`.

When asked to "run a test", "check stability", or "verify after changes", use this file as the baseline:

1. determine which change area was touched
2. run the required smoke and regression checks from the relevant sections
3. report `PASS / FAIL / RISK / NOT RUN`
4. explicitly call out whether the build is stable enough for:
   - local testing
   - `Tester Light`
   - `Bot Station Light`
   - server update

## Test Modes

Use two explicit regression modes.

### 1. Code-only Regression

Default mode after every update.

Use when:

- the user asks for a quick verification after code changes
- the goal is to understand whether the update should work in principle
- the changed area can be assessed through source, build, config, logs, and file artifacts

This mode must not invent UI behavior. If the answer depends on a live click path, mark it `RISK` or `NOT RUN`.

### 2. Runtime Regression

Use only when the changed area requires a live application check.

Typical triggers:

- WPF / WinForms UI behavior
- chart rendering and scrolling
- `Tester Light` execution behavior
- bot duplication / clone flow
- optimizer screen behavior

If a change touches both logic and UI, run code-only regression first, then add only the minimum runtime checks required.

## Current Working Model

- Development branch: `dev`
- Stable/server branch: `main`
- Local repo root: `C:\Users\user\Desktop\OsNewGen`
- Main project: `project\OsEngine`
- Main executable: `project\OsEngine\bin\Debug\OsEngine.exe`

Server layout currently assumed:

- stable folder: `OsNewGenGit2` -> `main`
- test folder: `OsNewGenTest` -> `dev`

## Agent Usage

Preferred agent / skill:

- `$update-regression-tester`

Recommended prompts:

- `Use $update-regression-tester and run a code-only regression for the latest update`
- `Use $update-regression-tester and run full runtime regression for Tester Light`
- `Use $update-regression-tester and assess whether the update is ready for server deployment`

## Important Current Context

The following areas were recently changed and must be rechecked often:

1. `WinFormsChartPainter`
   - multiple defensive fixes were added for chart rendering stability
   - chart bugs were old, but recent bot/copy flows can make them surface more often

2. Bot copy / `Tester Light` clone flow
   - `ChartMaster.txt` is now skipped when duplicating bots and when creating tester clones
   - goal: copied bots should not inherit dirty chart state from the source bot

3. Robot parameters window
   - new top button added to switch to a flat "all parameters" view
   - goal: show all parameters in one tab without section split

4. `MRZScoreNatrGrid`
   - used as one of the main regression robots for mean reversion grid behavior
   - important tester caveat: in candle mode, multiple `market` levels may execute at the same candle `open`

## Build Rules

### Normal build

Preferred build:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe" "project\OsEngine.sln" /t:Build /p:Configuration=Debug /p:Platform="Any CPU" /m
```

### Fallback build when `obj\Debug` is locked

If XAML-generated files in `project\OsEngine\obj\Debug` are locked, build through a separate intermediate folder:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe" "project\OsEngine\OsEngine.csproj" /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /p:BaseIntermediateOutputPath="obj_build\\" /p:IntermediateOutputPath="obj_build\\Debug\\" /p:OutputPath="bin\\Debug\\" /m
```

After successful fallback build, note:

- `project\OsEngine\obj_build\` is temporary and should not be committed

## Universal Code-Level Regression

Run this after any update, even if runtime checks are skipped.

1. Inspect changed files:
   - `git status --short`
   - if needed, targeted `git diff`
2. Classify the change area:
   - UI
   - charting
   - tester / optimizer
   - robot logic
   - update scripts / branch delivery
   - logging / diagnostics
3. Rebuild if the change affects executable behavior.
4. Verify artifact freshness:
   - changed source timestamps should not be newer than the rebuilt `OsEngine.exe`
5. Check that the update is wired correctly in code:
   - new buttons/columns/parameters exist in source
   - changed logic is referenced from the actual execution path
6. Check for obvious regression ripple:
   - search for affected symbols/usages
   - ensure no stale code path still points to old behavior
7. Scan recent logs for same-day exceptions related to the touched area.
   - old historic log noise is not a failure by itself
   - only count it as `FAIL` if it reproduces or if the code still clearly contains the same defect path
8. Check temporary build pollution:
   - nested `project\OsEngine\project\...` should be removed
   - temp `obj_build*` folders must not be treated as release artifacts

Pass criteria:

- changed area identified
- build succeeds or is explicitly `NOT RUN`
- executable freshness is evidence-backed
- code path for the update is connected
- no same-day reproducible error tied to the touched area

## Update Delivery Regression

Use this whenever the user asks whether an update is ready to ship or whether a server folder should be updated.

1. Confirm branch/commit state:
   - local `dev` / `main`
   - server `dev` / `main` if relevant
2. Confirm whether the required source changes are present in the checked-out branch.
3. Confirm whether the rebuilt `OsEngine.exe` is included in the intended branch if the update depends on binary delivery.
4. Confirm update scripts are aligned with the intended branch:
   - `main` updater pulls `main`
   - `dev` updater pulls `dev`
5. Explicitly state:
   - code ready / not ready
   - binary ready / not ready
   - server update recommended / not recommended

## Report Format

Each future test pass should report at least:

- build result
- launch result
- changed area tested
- regressions checked
- result per scenario
- final conclusion

Recommended output format:

```text
Mode: code-only | runtime
Build: PASS
Launch: PASS
Area under test: <feature/fix>

Checks:
- Smoke launch: PASS
- Parameters UI: PASS
- Copy bot flow: PASS
- Tester Light clone flow: PASS
- Chart stability: RISK

Conclusion:
- local testing: stable
- Tester Light: stable with notes
- Bot Station Light: stable / not recommended
- server update: yes / no
```

## Universal Smoke Test

Run this after any code change that affects UI, bot state, tester, copying, or charting.

1. Build project.
2. Launch `project\OsEngine\bin\Debug\OsEngine.exe`.
3. Confirm application opens without immediate exception.
4. Open a bot.
5. Open bot parameters window.
6. Confirm no startup error in log.
7. Close application cleanly.

Pass criteria:

- app opens
- app closes
- no immediate unhandled exception

Important:

- do not run this by default if code-only regression is enough
- prefer code-only first, runtime second

## Regression Matrix

### A. Parameters Window Regression

Relevant files:

- `project\OsEngine\Entity\StrategyParemetrsUi.xaml`
- `project\OsEngine\Entity\StrategyParemetrsUi.xaml.cs`
- `project\OsEngine\Language\EntityLocal.cs`

Scenarios:

1. Open parameters for a robot with multiple parameter sections.
2. Confirm top button `Open all parameters` is visible.
3. Click it.
4. Confirm a single tab `All parameters` is shown.
5. Change several parameters from different former sections.
6. Click `Update`.
7. Reopen robot parameters and confirm values persisted.
8. Click `Show sections`.
9. Confirm old section-based tabs return.

Pass criteria:

- button exists
- switching modes does not lose current edits
- saving still works
- no duplicate controls or tab corruption

### B. Bot Duplicate Regression

Relevant files:

- `project\OsEngine\OsTrader\OsTraderMaster.cs`

Core expectation:

- new duplicate bots must not inherit `ChartMaster.txt` from source

Scenarios:

1. Create a source bot with indicators and opened chart.
2. Duplicate the bot.
3. Open duplicate chart.
4. Confirm chart is created cleanly.
5. Confirm duplicate does not inherit broken/dirty chart state.
6. Confirm no chart exception appears when switching tabs or opening parameters.

Pass criteria:

- duplicate opens
- duplicate chart does not glitch immediately
- duplicate has independent chart state

Important note:

- bots duplicated before the fix may still be dirty
- old copies are not proof of failure of the current implementation

### C. Tester Light Clone Regression

Core expectation:

- `Tester Light` clones should be recreated from clean runtime state
- cloned bots should not drag old `ChartMaster.txt`

Scenarios:

1. Start from a fresh build.
2. Trigger `Tester Light` clone generation.
3. Open cloned robot in tester context.
4. Confirm parameters are copied correctly.
5. Confirm chart opens without inherited source-bot corruption.
6. Run a short test interval.
7. Confirm no chart exception is raised while scrolling / switching tabs / opening params.

Pass criteria:

- clone is created
- clone launches
- chart stable
- no inherited broken chart state

### D. Chart Stability Regression

Relevant files:

- `project\OsEngine\Charts\CandleChart\WinFormsChartPainter.cs`

Known risk area:

- this file historically contains multiple crash points

Scenarios:

1. Open bot chart.
2. Run tester on a period with many trades.
3. Scroll chart.
4. Toggle tabs.
5. Open and close parameters during/after run.
6. Reopen chart.
7. Repeat with copied bot and tester clone.

Watch specifically for:

- `NullReferenceException`
- `ArgumentOutOfRangeException`
- `ArgumentException`
- `IndexOutOfRangeException`

Pass criteria:

- no chart exception during rendering
- labels and axis remain usable
- positions are painted without crash

### E. `MRZScoreNatrGrid` Trading Logic Regression

Relevant file:

- `project\OsEngine\Robots\TrigonumCustom\MeanReversion\MRZScoreNatrGrid.cs`

Test goals:

- grid builds correctly
- `Grid Size` limits slot count
- multiple triggered levels behave consistently
- tester behavior matches current model expectations

Scenarios:

1. Run robot in `Tester Light` on a known instrument and interval.
2. Check grid construction in debug/log.
3. Confirm number of levels does not exceed `Grid Size`.
4. Confirm repeated fills do not exceed designed slot count.
5. Check behavior in:
   - `OrderType = Market`
   - `OrderType = Limit`

Expected behavior note:

- In candle tester mode, multiple `market` levels may execute at the same candle `open`.
- This is expected when the candle opens already beyond those levels.
- It is not, by itself, proof of wrong robot logic.

This must be reported explicitly as:

- `expected tester limitation`
or
- `unexpected logic bug`

### F. `Change24` Filter Regression

Relevant file:

- `project\OsEngine\Common\Change24Decoration.cs`

Expected logic:

- `Change` = close-to-close percent change over last 24 hours
- buy is blocked when 24h drop is greater than or equal to threshold
- sell is blocked when 24h rise is greater than or equal to threshold

Scenarios:

1. Run robot with `Change24 Enabled = true`.
2. Use log/debug to confirm blocked entries appear in expected trend conditions.
3. Disable `Change24`.
4. Confirm entries become more permissive.

Pass criteria:

- filter blocks only directional entries
- disabling the filter removes those blocks

### G. Update Scripts Regression

Relevant files:

- `update_osengine_robots_light_from_github.bat`
- `update_osengine_robots_light_from_github_main.bat`
- `update_osengine_robots_light_from_github_dev.bat`

Scenarios:

1. In a `main` repo folder, run the `main` update script.
2. In a `dev` repo folder, run the `dev` update script.
3. Confirm branch checkout matches script purpose.
4. Confirm script does not auto-start OsEngine.
5. Confirm dirty tracked state blocks unsafe update.
6. Confirm user confirmation works correctly on running process warning.

Pass criteria:

- branch target is correct
- no accidental auto-launch
- no false success on failed `git pull`

## Minimum Test Sets By Change Type

### If chart code changed

Run:

- Universal Smoke Test
- D. Chart Stability Regression
- B. Bot Duplicate Regression
- C. Tester Light Clone Regression

### If bot copy / tester clone code changed

Run:

- Universal Smoke Test
- B. Bot Duplicate Regression
- C. Tester Light Clone Regression
- D. Chart Stability Regression

### If robot parameter UI changed

Run:

- Universal Smoke Test
- A. Parameters Window Regression
- D. Chart Stability Regression

### If `MRZScoreNatrGrid` logic changed

Run:

- Universal Smoke Test
- E. `MRZScoreNatrGrid` Trading Logic Regression
- F. `Change24` Filter Regression if touched
- D. Chart Stability Regression

### If update/deploy scripts changed

Run:

- G. Update Scripts Regression

## Current Known Notes

1. Existing old copied bots may still be broken even if new copy logic is fixed.
2. XAML `obj\Debug` may be locked by external tools; fallback build path is acceptable for verification.
3. For tester investigations, logs from `project\OsEngine\bin\Debug\Engine\Log` are part of the test evidence.
4. A test is not complete until the result is classified as:
   - product bug
   - expected tester approximation
   - dirty old runtime state
   - fixed and verified

## How To Use This File In Future Sessions

When asked to run tests:

1. identify changed files
2. map them to sections in this file
3. run only relevant blocks plus universal smoke
4. report concise result with pass/fail/risk
5. if a scenario is not run, say `NOT RUN`

This file should be updated whenever:

- a new fragile area is discovered
- an expected behavior is clarified
- a tester limitation is reclassified as a real bug
- a new regression category appears
