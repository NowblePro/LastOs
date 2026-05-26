#!/usr/bin/env python3
"""Portfolio risk analyzer for averaging crypto robots.

The script accepts normalized trade CSV files and can also parse OsEngine
tester logs produced by Bot Station/Tester Light. It focuses on overlap risk:
robots entering, holding, and averaging at the same time.
"""

from __future__ import annotations

import argparse
import math
import re
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

import matplotlib

matplotlib.use("Agg")

import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
import seaborn as sns


REQUIRED_REPORTS = [
    "robot_stats.csv",
    "pnl_correlation_matrix.csv",
    "drawdown_correlation_matrix.csv",
    "overlap_matrix.csv",
    "averaging_overlap_matrix.csv",
    "risk_similarity_matrix.csv",
    "recommended_portfolios.csv",
    "portfolio_risk_summary.csv",
    "stress_test_results.csv",
]

DEFAULT_TIMEFRAME = "1H"


def normalize_freq(freq: str) -> str:
    """Normalize pandas aliases to avoid version-specific deprecation noise."""
    aliases = {
        "1H": "1h",
        "H": "h",
        "1D": "1d",
        "D": "d",
        "1W": "1W",
        "W": "1W",
    }
    return aliases.get(freq, freq)


@dataclass(frozen=True)
class MetricStatus:
    name: str
    available: bool
    reason: str = ""


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Analyze portfolio overlap and margin risk for averaging crypto robots."
    )
    parser.add_argument("--input", action="append", help="CSV file with trades. Can be passed multiple times.")
    parser.add_argument("--input_dir", help="Directory with CSV files.")
    parser.add_argument(
        "--osengine_log_dir",
        help="Directory with OsEngine *Log_YYYY_M_D.txt files. Used if CSV is not supplied or together with CSV.",
    )
    parser.add_argument(
        "--latest_osengine",
        action="store_true",
        help="Use latest OsEngine log date from --osengine_log_dir.",
    )
    parser.add_argument(
        "--all_osengine_day",
        action="store_true",
        help="When used with --latest_osengine, keep the whole daily robot logs instead of only the last tester run window.",
    )
    parser.add_argument(
        "--latest_limit",
        type=int,
        default=0,
        help="Limit latest OsEngine log parser to N most active robot logs.",
    )
    parser.add_argument(
        "--robots",
        help="Comma-separated robot names to include. Useful for exact tester-run subsets.",
    )
    parser.add_argument("--portfolios", type=int, default=4, help="Number of recommended portfolios.")
    parser.add_argument("--timeframe", default=DEFAULT_TIMEFRAME, help="Overlap/correlation timeframe, e.g. 15min, 1H.")
    parser.add_argument("--output", default="reports", help="Output reports directory.")
    parser.add_argument(
        "--notional_balance",
        type=float,
        default=100000.0,
        help="Reference balance for exposure/margin warnings if margin data is absent.",
    )
    parser.add_argument(
        "--summary_top",
        type=int,
        default=10,
        help="Number of risky pairs to print in terminal summary.",
    )
    return parser.parse_args()


def normalize_number(value: str | float | int | None) -> float:
    if value is None or (isinstance(value, float) and math.isnan(value)):
        return np.nan
    if isinstance(value, (int, float)):
        return float(value)
    text = str(value).strip().replace("\xa0", "").replace(" ", "")
    if not text:
        return np.nan
    if "," in text and "." in text:
        text = text.replace(",", "")
    else:
        text = text.replace(",", ".")
    try:
        return float(text)
    except ValueError:
        return np.nan


def parse_datetime_series(series: pd.Series) -> pd.Series:
    parsed = pd.to_datetime(series, errors="coerce", dayfirst=True)
    return parsed


def normalize_trade_columns(df: pd.DataFrame, source_name: str = "") -> pd.DataFrame:
    lower_map = {str(c).strip().lower(): c for c in df.columns}
    aliases = {
        "robot_name": ["robot_name", "robot", "source", "strategy", "bot", "bot_name"],
        "symbol": ["symbol", "security", "instrument", "ticker", "бумага"],
        "side": ["side", "direction", "сторона"],
        "entry_time": ["entry_time", "open_time", "time_open", "время входа"],
        "exit_time": ["exit_time", "close_time", "time_close", "время выхода"],
        "entry_price": ["entry_price", "open_price", "price_open", "цена входа"],
        "exit_price": ["exit_price", "close_price", "price_close", "цена выхода"],
        "qty": ["qty", "quantity", "volume", "max_volume", "максимальный объём", "максимальный объем"],
        "pnl": ["pnl", "profit", "profit_abs", "профит abs"],
        "fee": ["fee", "commission", "комиссия"],
        "order_type": ["order_type", "order"],
        "averaging_level": ["averaging_level", "level", "signal_level"],
        "position_id": ["position_id", "id", "номер"],
        "balance": ["balance"],
        "equity": ["equity"],
        "margin_used": ["margin_used", "margin"],
        "log_time": ["log_time"],
    }

    result = pd.DataFrame()
    for target, names in aliases.items():
        source_col = next((lower_map[n] for n in names if n in lower_map), None)
        if source_col is not None:
            result[target] = df[source_col]

    if "robot_name" not in result:
        result["robot_name"] = source_name or "unknown_robot"
    if "fee" not in result:
        result["fee"] = 0.0
    if "side" not in result:
        result["side"] = "unknown"

    for col in ["entry_time", "exit_time"]:
        if col in result:
            result[col] = parse_datetime_series(result[col])

    for col in ["entry_price", "exit_price", "qty", "pnl", "fee", "averaging_level", "balance", "equity", "margin_used"]:
        if col in result:
            result[col] = result[col].map(normalize_number)

    if "position_id" in result:
        result["position_id"] = result["position_id"].astype(str)
    else:
        result["position_id"] = np.arange(len(result)).astype(str)

    if "symbol" not in result:
        result["symbol"] = "unknown"

    result["side"] = result["side"].astype(str).str.lower().replace({"buy": "long", "sell": "short"})
    return result


def read_csv_files(paths: Iterable[Path]) -> pd.DataFrame:
    frames: list[pd.DataFrame] = []
    for path in paths:
        try:
            raw = pd.read_csv(path)
        except UnicodeDecodeError:
            raw = pd.read_csv(path, encoding="cp1251")
        frames.append(normalize_trade_columns(raw, source_name=path.stem))
    return pd.concat(frames, ignore_index=True) if frames else pd.DataFrame()


def read_text_with_fallback(path: Path) -> str:
    data = path.read_bytes()
    for encoding in ("utf-8-sig", "utf-8", "cp1251"):
        try:
            text = data.decode(encoding)
            if "Позиция" in text or encoding == "cp1251":
                return text
        except UnicodeDecodeError:
            continue
    return data.decode("utf-8", errors="ignore")


def parse_osengine_log(path: Path) -> pd.DataFrame:
    text = read_text_with_fallback(path)
    records = re.split(r"\n;\s*\n?", text)
    rows: list[dict[str, object]] = []

    for block in records:
        if "Позиция закрыта" not in block:
            continue

        header = block.splitlines()[0] if block.splitlines() else ""
        source = re.search(r"Источник:\s*([^\n\r;]+)", block)
        number = re.search(r"Номер:\s*([^,\n\r]+)", block)
        side = re.search(r"Сторона:\s*([^\n\r]+)", block)
        symbol = re.search(r"Бумага:\s*([^\n\r]+)", block)
        pnl = re.search(r"Профит abs:\s*([^\n\r]+)", block)
        prices = re.search(r"Цена входа:\s*([^,\n\r]+),\s*Цена выхода:\s*([^\n\r]+)", block)
        times = re.search(r"Время входа:\s*([^,\n\r]+),\s*Время выхода:\s*([^\n\r]+)", block)
        qty = re.search(r"Максимальный объ[её]м:\s*([^\n\r]+)", block)
        signal = re.search(r"Сигнал на открытие:\s*([^\n\r]+)", block)
        level = re.search(r"Level[: ]+(\d+)", signal.group(1) if signal else "")

        if not times:
            continue

        rows.append(
            {
                "robot_name": source.group(1).strip() if source else path.name.split("Log_")[0],
                "symbol": symbol.group(1).strip().replace(".txt", "") if symbol else "unknown",
                "side": side.group(1).strip().lower().replace("buy", "long").replace("sell", "short") if side else "unknown",
                "entry_time": times.group(1).strip(),
                "exit_time": times.group(2).strip(),
                "entry_price": prices.group(1).strip() if prices else np.nan,
                "exit_price": prices.group(2).strip() if prices else np.nan,
                "qty": qty.group(1).strip() if qty else np.nan,
                "pnl": pnl.group(1).strip() if pnl else np.nan,
                "fee": 0.0,
                "order_type": "osengine_log",
                "averaging_level": level.group(1) if level else np.nan,
                "position_id": number.group(1).strip() if number else f"{path.stem}_{len(rows)}",
                "log_time": header.split(";")[0] if ";" in header else "",
            }
        )

    return normalize_trade_columns(pd.DataFrame(rows), source_name=path.name.split("Log_")[0]) if rows else pd.DataFrame()


def latest_osengine_logs(log_dir: Path, limit: int = 0, robots: set[str] | None = None) -> list[Path]:
    files = sorted(log_dir.glob("*Log_*.txt"), key=lambda p: p.stat().st_mtime, reverse=True)
    if not files:
        return []
    latest_day = files[0].name.split("Log_")[-1]
    same_day = [p for p in files if p.name.endswith(latest_day)]
    if robots:
        same_day = [p for p in same_day if p.name.split("Log_")[0] in robots]
    else:
        same_day = [p for p in same_day if p.stat().st_size > 0 and not p.name.startswith(("PrimeLog", "ServerMasterLog", "TesterServerLog"))]
    same_day = sorted(same_day, key=lambda p: p.stat().st_size, reverse=True)
    return same_day[:limit] if limit and limit > 0 else same_day


def latest_tester_run_window(log_dir: Path) -> tuple[pd.Timestamp | None, pd.Timestamp | None]:
    tester_logs = sorted(log_dir.glob("TesterServerLog_*.txt"), key=lambda p: p.stat().st_mtime, reverse=True)
    if not tester_logs:
        return None, None

    text = read_text_with_fallback(tester_logs[0])
    start_candidates: list[pd.Timestamp] = []
    fallback_instrument_candidates: list[pd.Timestamp] = []
    end_candidates: list[pd.Timestamp] = []

    for line in text.splitlines():
        match = re.match(r"^(\d{2}\.\d{2}\.\d{4} \d{1,2}:\d{2}:\d{2});", line)
        if not match:
            continue
        ts = pd.to_datetime(match.group(1), dayfirst=True, errors="coerce")
        if pd.isna(ts):
            continue
        if "Включен процесс тестирования" in line:
            start_candidates.append(ts)
        elif "Инструмент " in line and "успешно подключен" in line:
            fallback_instrument_candidates.append(ts)
        elif "Тестирование завершилось" in line:
            end_candidates.append(ts)

    end = max(end_candidates) if end_candidates else None
    starts_before_end = [ts for ts in start_candidates if end is None or ts <= end]
    start = max(starts_before_end) if starts_before_end else None

    if start is None and end is not None:
        instruments_before_end = [ts for ts in fallback_instrument_candidates if ts <= end]
        start = max(instruments_before_end) if instruments_before_end else None

    return start, end


def load_trades(args: argparse.Namespace) -> tuple[pd.DataFrame, list[MetricStatus]]:
    statuses: list[MetricStatus] = []
    frames: list[pd.DataFrame] = []

    csv_paths: list[Path] = []
    if args.input:
        csv_paths.extend(Path(p) for p in args.input)
    if args.input_dir:
        csv_paths.extend(sorted(Path(args.input_dir).glob("*.csv")))
    if csv_paths:
        frames.append(read_csv_files(csv_paths))

    if args.osengine_log_dir:
        log_dir = Path(args.osengine_log_dir)
        robots = set(x.strip() for x in args.robots.split(",")) if args.robots else None
        paths = latest_osengine_logs(log_dir, args.latest_limit, robots) if args.latest_osengine else sorted(log_dir.glob("*Log_*.txt"))
        log_frames = [parse_osengine_log(p) for p in paths]
        log_frames = [f for f in log_frames if not f.empty]
        if log_frames:
            osengine_trades = pd.concat(log_frames, ignore_index=True)
            if args.latest_osengine and not args.all_osengine_day:
                start, end = latest_tester_run_window(log_dir)
                if start is not None and "log_time" in osengine_trades:
                    before_count = len(osengine_trades)
                    log_time = parse_datetime_series(osengine_trades["log_time"])
                    mask = log_time >= start
                    if end is not None:
                        mask &= log_time <= end
                    osengine_trades = osengine_trades[mask].copy()
                    statuses.append(
                        MetricStatus(
                            "latest_osengine_run_window",
                            True,
                            f"{start} -> {end}; kept {len(osengine_trades)} of {before_count} parsed closed positions",
                        )
                    )
                else:
                    statuses.append(MetricStatus("latest_osengine_run_window", False, "tester run window not found"))
            frames.append(osengine_trades)
        statuses.append(MetricStatus("osengine_logs_used", bool(log_frames), f"{len(log_frames)} parsed files"))

    if not frames:
        raise SystemExit("No input trades found. Use --input, --input_dir, or --osengine_log_dir --latest_osengine.")

    trades = pd.concat(frames, ignore_index=True)
    trades = clean_trades(trades)
    statuses.extend(validate_metric_inputs(trades))
    return trades, statuses


def clean_trades(trades: pd.DataFrame) -> pd.DataFrame:
    trades = trades.copy()
    for col in ["entry_time", "exit_time"]:
        if col not in trades:
            trades[col] = pd.NaT
        trades[col] = parse_datetime_series(trades[col])
    if "log_time" in trades:
        trades["log_time"] = parse_datetime_series(trades["log_time"])
    for col in ["pnl", "fee", "qty", "entry_price", "exit_price", "averaging_level", "margin_used"]:
        if col not in trades:
            trades[col] = np.nan if col != "fee" else 0.0
        trades[col] = pd.to_numeric(trades[col], errors="coerce")
    trades["fee"] = trades["fee"].fillna(0.0)
    trades["net_pnl"] = trades["pnl"].fillna(0.0) - trades["fee"].fillna(0.0)
    trades["robot_name"] = trades["robot_name"].astype(str)
    trades["symbol"] = trades["symbol"].astype(str)
    trades["side"] = trades["side"].astype(str).str.lower()
    if "position_id" not in trades:
        trades["position_id"] = np.arange(len(trades)).astype(str)
    trades = trades.dropna(subset=["entry_time", "exit_time"])
    trades = trades[trades["exit_time"] >= trades["entry_time"]]
    return trades.reset_index(drop=True)


def validate_metric_inputs(trades: pd.DataFrame) -> list[MetricStatus]:
    checks = [
        MetricStatus("fees", "fee" in trades and trades["fee"].notna().any(), "net pnl equals pnl if fees absent"),
        MetricStatus("margin_used", "margin_used" in trades and trades["margin_used"].notna().any(), "max margin metrics unavailable"),
        MetricStatus("averaging_level", "averaging_level" in trades and trades["averaging_level"].notna().any(), "averaging inferred from repeated intervals if absent"),
        MetricStatus("qty", "qty" in trades and trades["qty"].notna().any(), "exposure estimated from trade count if absent"),
    ]
    return checks


def max_drawdown(equity: pd.Series) -> float:
    if equity.empty:
        return 0.0
    running_max = equity.cummax()
    dd = equity - running_max
    return float(dd.min())


def pnl_table(trades: pd.DataFrame, freq: str) -> pd.DataFrame:
    freq = normalize_freq(freq)
    return (
        trades.pivot_table(index=pd.Grouper(key="exit_time", freq=freq), columns="robot_name", values="net_pnl", aggfunc="sum")
        .sort_index()
        .fillna(0.0)
    )


def drawdown_table(pnl: pd.DataFrame) -> pd.DataFrame:
    equity = pnl.cumsum()
    return equity - equity.cummax()


def active_matrix(trades: pd.DataFrame, freq: str, averaging_only: bool = False) -> pd.DataFrame:
    freq = normalize_freq(freq)
    start = trades["entry_time"].min().floor(freq)
    end = trades["exit_time"].max().ceil(freq)
    idx = pd.date_range(start, end, freq=freq)
    robots = sorted(trades["robot_name"].unique())
    matrix = pd.DataFrame(0, index=idx, columns=robots, dtype=float)

    source = trades
    if averaging_only:
        source = trades[is_averaging_trade(trades)]
    for _, row in source.iterrows():
        start_i = row["entry_time"].floor(freq)
        end_i = row["exit_time"].ceil(freq)
        matrix.loc[(matrix.index >= start_i) & (matrix.index <= end_i), row["robot_name"]] = 1.0
    return matrix


def is_averaging_trade(trades: pd.DataFrame) -> pd.Series:
    level_signal = trades["averaging_level"].fillna(1) > 1
    group_cols = ["robot_name", "symbol", "side", "entry_time", "exit_time"]
    repeated = trades.groupby(group_cols)["position_id"].transform("count") > 1
    return level_signal | repeated


def overlap_from_active(active: pd.DataFrame) -> pd.DataFrame:
    robots = list(active.columns)
    out = pd.DataFrame(0.0, index=robots, columns=robots)
    for a in robots:
        for b in robots:
            denom = ((active[a] > 0) | (active[b] > 0)).sum()
            out.loc[a, b] = float(((active[a] > 0) & (active[b] > 0)).sum() / denom) if denom else 0.0
    return out


def interval_count_series(trades: pd.DataFrame, freq: str) -> pd.Series:
    if trades.empty:
        return pd.Series(dtype=float)
    freq = normalize_freq(freq)
    start = trades["entry_time"].min().floor(freq)
    end = trades["exit_time"].max().ceil(freq)
    idx = pd.date_range(start, end, freq=freq)
    counts = pd.Series(0.0, index=idx)
    for _, row in trades.iterrows():
        mask = (counts.index >= row["entry_time"].floor(freq)) & (counts.index <= row["exit_time"].ceil(freq))
        counts.loc[mask] += 1.0
    return counts


def pair_symbol_similarity(trades: pd.DataFrame) -> pd.DataFrame:
    robots = sorted(trades["robot_name"].unique())
    symbols = {r: set(trades.loc[trades["robot_name"] == r, "symbol"]) for r in robots}
    out = pd.DataFrame(0.0, index=robots, columns=robots)
    for a in robots:
        for b in robots:
            union = symbols[a] | symbols[b]
            out.loc[a, b] = len(symbols[a] & symbols[b]) / len(union) if union else 0.0
    return out


def side_overlap(trades: pd.DataFrame, freq: str) -> tuple[pd.DataFrame, pd.DataFrame]:
    robots = sorted(trades["robot_name"].unique())
    same = pd.DataFrame(0.0, index=robots, columns=robots)
    opposite = pd.DataFrame(0.0, index=robots, columns=robots)
    active_rows = []
    for _, row in trades.iterrows():
        active_rows.append(row)

    for a in robots:
        ta = trades[trades["robot_name"] == a]
        for b in robots:
            tb = trades[trades["robot_name"] == b]
            overlaps = 0
            same_dir = 0
            opp_dir = 0
            for _, ra in ta.iterrows():
                candidates = tb[(tb["entry_time"] <= ra["exit_time"]) & (tb["exit_time"] >= ra["entry_time"])]
                for _, rb in candidates.iterrows():
                    overlaps += 1
                    if ra["side"] == rb["side"]:
                        same_dir += 1
                    else:
                        opp_dir += 1
            same.loc[a, b] = same_dir / overlaps if overlaps else 0.0
            opposite.loc[a, b] = opp_dir / overlaps if overlaps else 0.0
    return same, opposite


def robot_stats(trades: pd.DataFrame) -> pd.DataFrame:
    rows = []
    for robot, group in trades.groupby("robot_name"):
        g = group.sort_values("exit_time")
        equity = g["net_pnl"].cumsum()
        daily = g.set_index("exit_time")["net_pnl"].resample("1D").sum()
        weekly = g.set_index("exit_time")["net_pnl"].resample("1W").sum()
        wins = g[g["net_pnl"] > 0]
        losses = g[g["net_pnl"] < 0]
        holding = g["exit_time"] - g["entry_time"]
        open_position_count = interval_count_series(g, DEFAULT_TIMEFRAME)
        rows.append(
            {
                "robot_name": robot,
                "symbol_count": g["symbol"].nunique(),
                "symbols": ",".join(sorted(g["symbol"].unique())),
                "total_pnl": g["pnl"].sum(),
                "net_pnl_after_fees": g["net_pnl"].sum(),
                "number_of_trades": len(g),
                "winrate": len(wins) / len(g) if len(g) else np.nan,
                "profit_factor": wins["net_pnl"].sum() / abs(losses["net_pnl"].sum()) if abs(losses["net_pnl"].sum()) > 0 else np.inf,
                "average_trade_pnl": g["net_pnl"].mean(),
                "max_drawdown": max_drawdown(equity),
                "worst_day_pnl": daily.min() if not daily.empty else np.nan,
                "worst_week_pnl": weekly.min() if not weekly.empty else np.nan,
                "average_holding_time_hours": holding.mean().total_seconds() / 3600 if len(holding) else np.nan,
                "max_holding_time_hours": holding.max().total_seconds() / 3600 if len(holding) else np.nan,
                "max_averaging_level": g["averaging_level"].max(),
                "number_of_averaging_events": int(is_averaging_trade(g).sum()),
                "max_simultaneous_open_positions": int(open_position_count.max()) if not open_position_count.empty else 0,
                "max_margin_used": g["margin_used"].max() if g["margin_used"].notna().any() else np.nan,
                "max_trade_notional": (g["entry_price"] * g["qty"]).max() if g[["entry_price", "qty"]].notna().all(axis=1).any() else np.nan,
            }
        )
    return pd.DataFrame(rows).sort_values("robot_name")


def normalized_corr(matrix: pd.DataFrame) -> pd.DataFrame:
    corr = matrix.fillna(0.0).copy()
    return ((corr + 1.0) / 2.0).clip(0.0, 1.0)


def risk_similarity(
    pnl_corr: pd.DataFrame,
    dd_corr: pd.DataFrame,
    position_overlap: pd.DataFrame,
    averaging_overlap: pd.DataFrame,
    symbol_similarity: pd.DataFrame,
) -> pd.DataFrame:
    robots = sorted(set(position_overlap.index) | set(averaging_overlap.index) | set(symbol_similarity.index))
    components = {
        "pnl": (normalized_corr(pnl_corr), 0.25),
        "dd": (normalized_corr(dd_corr), 0.25),
        "position": (position_overlap, 0.20),
        "averaging": (averaging_overlap, 0.20),
        "symbol": (symbol_similarity, 0.10),
    }
    out = pd.DataFrame(0.0, index=robots, columns=robots)
    for a in robots:
        for b in robots:
            value = 0.0
            weight_sum = 0.0
            for matrix, weight in components.values():
                if a in matrix.index and b in matrix.columns:
                    metric = matrix.loc[a, b]
                    if pd.notna(metric):
                        value += float(metric) * weight
                        weight_sum += weight
            out.loc[a, b] = value / weight_sum if weight_sum else np.nan
    np.fill_diagonal(out.values, 1.0)
    return out


def recommend_portfolios(similarity: pd.DataFrame, n: int) -> pd.DataFrame:
    n = max(1, int(n))
    robots = list(similarity.index)
    risk_weight = similarity.sum(axis=1).sort_values(ascending=False)
    buckets: list[list[str]] = [[] for _ in range(n)]

    for robot in risk_weight.index:
        scores = []
        for i, bucket in enumerate(buckets):
            intra = sum(float(similarity.loc[robot, other]) for other in bucket) if bucket else 0.0
            scores.append((intra, len(bucket), i))
        _, _, chosen = min(scores)
        buckets[chosen].append(robot)

    rows = []
    for i, bucket in enumerate(buckets, start=1):
        for robot in bucket:
            strongest = similarity.loc[robot].drop(robot, errors="ignore").sort_values(ascending=False).head(3)
            reason = "separated from high-similarity peers: " + ", ".join(f"{idx}={val:.2f}" for idx, val in strongest.items())
            rows.append({"robot_name": robot, "recommended_portfolio": i, "reason": reason})
    return pd.DataFrame(rows).sort_values(["recommended_portfolio", "robot_name"])


def portfolio_summary(trades: pd.DataFrame, freq: str, pnl: pd.DataFrame, active: pd.DataFrame, averaging: pd.DataFrame) -> pd.DataFrame:
    portfolio_pnl = pnl.sum(axis=1)
    equity = portfolio_pnl.cumsum()
    daily = portfolio_pnl.resample("1D").sum()
    weekly = portfolio_pnl.resample("1W").sum()
    exposure = exposure_timeline(trades, freq)
    margin = margin_timeline(trades, freq)
    return pd.DataFrame(
        [
            {
                "robots_analyzed": trades["robot_name"].nunique(),
                "symbols_analyzed": trades["symbol"].nunique(),
                "trades_analyzed": len(trades),
                "portfolio_total_net_pnl": trades["net_pnl"].sum(),
                "portfolio_max_drawdown": max_drawdown(equity),
                "worst_portfolio_day": daily.min() if not daily.empty else np.nan,
                "worst_portfolio_week": weekly.min() if not weekly.empty else np.nan,
                "maximum_active_robots": active.sum(axis=1).max() if not active.empty else 0,
                "maximum_robots_averaging": averaging.sum(axis=1).max() if not averaging.empty else 0,
                "maximum_simultaneous_exposure": exposure.sum(axis=1).max() if not exposure.empty else np.nan,
                "maximum_simultaneous_margin_usage": margin.sum(axis=1).max() if not margin.empty else np.nan,
            }
        ]
    )


def exposure_timeline(trades: pd.DataFrame, freq: str) -> pd.DataFrame:
    freq = normalize_freq(freq)
    if not trades[["entry_price", "qty"]].notna().all(axis=1).any():
        return pd.DataFrame()
    start = trades["entry_time"].min().floor(freq)
    end = trades["exit_time"].max().ceil(freq)
    idx = pd.date_range(start, end, freq=freq)
    robots = sorted(trades["robot_name"].unique())
    out = pd.DataFrame(0.0, index=idx, columns=robots)
    for _, row in trades.dropna(subset=["entry_price", "qty"]).iterrows():
        mask = (out.index >= row["entry_time"].floor(freq)) & (out.index <= row["exit_time"].ceil(freq))
        out.loc[mask, row["robot_name"]] += abs(float(row["entry_price"]) * float(row["qty"]))
    return out


def margin_timeline(trades: pd.DataFrame, freq: str) -> pd.DataFrame:
    freq = normalize_freq(freq)
    if "margin_used" not in trades or not trades["margin_used"].notna().any():
        return pd.DataFrame()
    start = trades["entry_time"].min().floor(freq)
    end = trades["exit_time"].max().ceil(freq)
    idx = pd.date_range(start, end, freq=freq)
    robots = sorted(trades["robot_name"].unique())
    out = pd.DataFrame(0.0, index=idx, columns=robots)
    for _, row in trades.dropna(subset=["margin_used"]).iterrows():
        mask = (out.index >= row["entry_time"].floor(freq)) & (out.index <= row["exit_time"].ceil(freq))
        out.loc[mask, row["robot_name"]] += abs(float(row["margin_used"]))
    return out


def stress_tests(trades: pd.DataFrame, active: pd.DataFrame, averaging: pd.DataFrame, exposure: pd.DataFrame, balance: float) -> pd.DataFrame:
    rows = []
    robots = trades["robot_name"].nunique()
    max_level_by_robot = trades.groupby("robot_name")["averaging_level"].max()
    max_trade_notional = trades.assign(notional=trades["entry_price"] * trades["qty"]).groupby("robot_name")["notional"].max()

    rows.append(
        {
            "scenario": "all_robots_enter_same_time",
            "robots_active": robots,
            "estimated_exposure": max_trade_notional.sum() if max_trade_notional.notna().any() else np.nan,
            "estimated_exposure_pct_balance": max_trade_notional.sum() / balance if max_trade_notional.notna().any() and balance else np.nan,
            "warning": "all active at once; compare exposure to available margin",
        }
    )
    rows.append(
        {
            "scenario": "all_robots_reach_max_averaging",
            "robots_active": robots,
            "max_averaging_level_sum": max_level_by_robot.sum(),
            "estimated_exposure": max_trade_notional.sum() if max_trade_notional.notna().any() else np.nan,
            "estimated_exposure_pct_balance": max_trade_notional.sum() / balance if max_trade_notional.notna().any() and balance else np.nan,
            "warning": "maximum historical levels happen simultaneously",
        }
    )
    if not active.empty:
        worst_time = active.sum(axis=1).idxmax()
        rows.append(
            {
                "scenario": "worst_historical_overlap_period",
                "timestamp": worst_time,
                "robots_active": active.loc[worst_time].sum(),
                "robots_averaging": averaging.loc[worst_time].sum() if not averaging.empty and worst_time in averaging.index else np.nan,
                "estimated_exposure": exposure.loc[worst_time].sum() if not exposure.empty and worst_time in exposure.index else np.nan,
                "estimated_exposure_pct_balance": exposure.loc[worst_time].sum() / balance if not exposure.empty and worst_time in exposure.index and balance else np.nan,
                "warning": "historical peak overlap",
            }
        )
    if not exposure.empty:
        worst_exposure_time = exposure.sum(axis=1).idxmax()
        rows.append(
            {
                "scenario": "worst_historical_exposure_period",
                "timestamp": worst_exposure_time,
                "robots_active": active.loc[worst_exposure_time].sum() if not active.empty and worst_exposure_time in active.index else np.nan,
                "robots_averaging": averaging.loc[worst_exposure_time].sum() if not averaging.empty and worst_exposure_time in averaging.index else np.nan,
                "estimated_exposure": exposure.loc[worst_exposure_time].sum(),
                "estimated_exposure_pct_balance": exposure.loc[worst_exposure_time].sum() / balance if balance else np.nan,
                "warning": "historical peak exposure",
            }
        )
    return pd.DataFrame(rows)


def save_heatmap(matrix: pd.DataFrame, path: Path, title: str) -> None:
    if matrix.empty:
        return
    plt.figure(figsize=(max(8, len(matrix.columns) * 0.65), max(6, len(matrix.index) * 0.55)))
    sns.heatmap(matrix.astype(float), cmap="RdYlGn_r", vmin=0, vmax=1, annot=False)
    plt.title(title)
    plt.tight_layout()
    plt.savefig(path)
    plt.close()


def save_line(series: pd.Series, path: Path, title: str, ylabel: str) -> None:
    plt.figure(figsize=(12, 5))
    series.plot()
    plt.title(title)
    plt.ylabel(ylabel)
    plt.tight_layout()
    plt.savefig(path)
    plt.close()


def save_timeline(matrix: pd.DataFrame, path: Path, title: str) -> None:
    if matrix.empty:
        return
    plt.figure(figsize=(13, max(4, len(matrix.columns) * 0.35)))
    sns.heatmap(matrix.T, cmap="Blues", cbar=False)
    plt.title(title)
    plt.xlabel("time")
    plt.ylabel("robot")
    plt.tight_layout()
    plt.savefig(path)
    plt.close()


def write_reports(
    output: Path,
    trades: pd.DataFrame,
    stats: pd.DataFrame,
    pnl_corr: pd.DataFrame,
    dd_corr: pd.DataFrame,
    overlap: pd.DataFrame,
    avg_overlap: pd.DataFrame,
    risk: pd.DataFrame,
    recs: pd.DataFrame,
    summary: pd.DataFrame,
    stress: pd.DataFrame,
    pnl: pd.DataFrame,
    active: pd.DataFrame,
    averaging: pd.DataFrame,
) -> None:
    output.mkdir(parents=True, exist_ok=True)
    charts = output / "charts"
    charts.mkdir(parents=True, exist_ok=True)

    stats.to_csv(output / "robot_stats.csv", index=False)
    pnl_corr.to_csv(output / "pnl_correlation_matrix.csv")
    dd_corr.to_csv(output / "drawdown_correlation_matrix.csv")
    overlap.to_csv(output / "overlap_matrix.csv")
    avg_overlap.to_csv(output / "averaging_overlap_matrix.csv")
    risk.to_csv(output / "risk_similarity_matrix.csv")
    recs.to_csv(output / "recommended_portfolios.csv", index=False)
    summary.to_csv(output / "portfolio_risk_summary.csv", index=False)
    stress.to_csv(output / "stress_test_results.csv", index=False)

    portfolio_equity = pnl.sum(axis=1).cumsum()
    portfolio_dd = portfolio_equity - portfolio_equity.cummax()
    save_line(portfolio_equity, charts / "portfolio_equity_curve.png", "Portfolio Equity Curve", "net pnl")
    save_line(portfolio_dd, charts / "portfolio_drawdown_curve.png", "Portfolio Drawdown Curve", "drawdown")
    save_heatmap(normalized_corr(pnl_corr), charts / "heatmap_pnl_correlation.png", "PnL Correlation")
    save_heatmap(normalized_corr(dd_corr), charts / "heatmap_drawdown_correlation.png", "Drawdown Correlation")
    save_heatmap(overlap, charts / "heatmap_position_overlap.png", "Position Overlap")
    save_heatmap(avg_overlap, charts / "heatmap_averaging_overlap.png", "Averaging Overlap")
    save_heatmap(risk, charts / "heatmap_risk_similarity.png", "Risk Similarity")
    save_timeline(active, charts / "timeline_active_robots.png", "Active Robots Timeline")
    save_timeline(averaging, charts / "timeline_robots_in_averaging_mode.png", "Robots In Averaging Mode")


def print_summary(
    trades: pd.DataFrame,
    risk: pd.DataFrame,
    avg_overlap: pd.DataFrame,
    summary: pd.DataFrame,
    recs: pd.DataFrame,
    stress: pd.DataFrame,
    statuses: list[MetricStatus],
    top: int,
) -> None:
    robots = trades["robot_name"].nunique()
    print("\n=== Portfolio Risk Summary ===")
    print(f"Robots analyzed: {robots}")
    print(f"Trades analyzed: {len(trades)}")
    print(f"Date range: {trades['entry_time'].min()} -> {trades['exit_time'].max()}")

    pairs = []
    for i, a in enumerate(risk.index):
        for b in risk.columns[i + 1 :]:
            pairs.append((a, b, risk.loc[a, b]))
    pairs = sorted(pairs, key=lambda x: x[2], reverse=True)[:top]
    print("\nMost dangerous robot pairs by Risk Similarity:")
    for a, b, score in pairs:
        print(f"- {a} / {b}: {score:.3f}")

    avg_pairs = []
    for i, a in enumerate(avg_overlap.index):
        for b in avg_overlap.columns[i + 1 :]:
            avg_pairs.append((a, b, avg_overlap.loc[a, b]))
    avg_pairs = sorted(avg_pairs, key=lambda x: x[2], reverse=True)[:top]
    print("\nPairs most often averaging together:")
    for a, b, score in avg_pairs:
        print(f"- {a} / {b}: {score:.3f}")

    if not summary.empty:
        row = summary.iloc[0]
        print("\nHistorical portfolio peak risk:")
        print(f"- max active robots: {row.get('maximum_active_robots', np.nan)}")
        print(f"- max robots averaging: {row.get('maximum_robots_averaging', np.nan)}")
        print(f"- portfolio max drawdown: {row.get('portfolio_max_drawdown', np.nan):.2f}")
        print(f"- worst portfolio day: {row.get('worst_portfolio_day', np.nan):.2f}")

    print("\nRecommended portfolio split:")
    for _, row in recs.sort_values(["recommended_portfolio", "robot_name"]).iterrows():
        print(f"- P{int(row['recommended_portfolio'])}: {row['robot_name']}")

    warnings = []
    if not stress.empty:
        for _, row in stress.iterrows():
            pct = row.get("estimated_exposure_pct_balance", np.nan)
            if pd.notna(pct) and pct > 1:
                warnings.append(f"{row['scenario']}: estimated exposure {pct:.1%} of balance")
    unavailable = [s for s in statuses if not s.available and s.reason]
    if unavailable:
        print("\nUnavailable metrics:")
        for s in unavailable:
            print(f"- {s.name}: {s.reason}")
    if warnings:
        print("\nRisk warnings:")
        for warning in warnings:
            print(f"- {warning}")


def main() -> int:
    args = parse_args()
    output = Path(args.output)
    trades, statuses = load_trades(args)
    if trades.empty:
        raise SystemExit("No valid trades after parsing.")

    freq = normalize_freq(args.timeframe)
    stats = robot_stats(trades)
    pnl = pnl_table(trades, freq)
    dd = drawdown_table(pnl)
    pnl_corr = pnl.corr().fillna(0.0)
    dd_corr = dd.corr().fillna(0.0)
    active = active_matrix(trades, freq)
    averaging = active_matrix(trades, freq, averaging_only=True)
    overlap = overlap_from_active(active)
    avg_overlap = overlap_from_active(averaging)
    symbol_similarity = pair_symbol_similarity(trades)
    risk = risk_similarity(pnl_corr, dd_corr, overlap, avg_overlap, symbol_similarity)
    recs = recommend_portfolios(risk, args.portfolios)
    exposure = exposure_timeline(trades, freq)
    summary = portfolio_summary(trades, freq, pnl, active, averaging)
    stress = stress_tests(trades, active, averaging, exposure, args.notional_balance)

    write_reports(output, trades, stats, pnl_corr, dd_corr, overlap, avg_overlap, risk, recs, summary, stress, pnl, active, averaging)
    print_summary(trades, risk, avg_overlap, summary, recs, stress, statuses, args.summary_top)
    print(f"\nReports written to: {output.resolve()}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
