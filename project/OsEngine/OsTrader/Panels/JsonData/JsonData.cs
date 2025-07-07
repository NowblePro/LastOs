using System.Collections.Generic;
using Google.Protobuf.Collections;

namespace OsEngine.OsTrader.Panels.JsonData
{
    public class JsonRunData
    {
        public JsonDataParameters data_parameters;
        public Dictionary<string, string> robot_parameters;
        public JsonStatistics statistics;
        public List<JsonCandleResult> run_results;
    }

    public class JsonDataParameters
    {
        public string ticker { get; set; }
        public string timeframe { get; set; }
        public string strategy_name { get; set; }
        public string strategy_type { get; set; }
        //public string time_start { get; set; }
        //public string time_end { get; set; }
    }

    public class JsonCandle
    {
        public decimal open { get; set; }
        public decimal close { get; set; }
        public decimal high { get; set; }
        public decimal low { get; set; }
    }

    public class JsonEquity
    {
        //public decimal candle_PL { get; set; }
        public decimal unrealized_candle_PL { get; set; }
        public decimal total_PL { get; set; }
        public decimal unrealized_long_PL { get; set; }
        public decimal total_long_PL { get; set; }
        public decimal unrealized_short_PL { get; set; }
        public decimal total_short_PL { get; set; }
    }

    public class JsonStatistics
    {
        public decimal total_profit { get; set; }
        public decimal long_total_profit { get; set; }
        public decimal short_total_profit { get; set; }
        public decimal total_profit_percent { get; set; }
        public decimal long_total_profit_percent { get; set; }
        public decimal short_total_profit_percent { get; set; }
        public int position_count { get; set; }
        public int long_position_count { get; set; }
        public int short_position_count { get; set; }
        public decimal sharp_ratio { get; set; }
        public decimal max_sma_deviation { get; set; }
        public decimal profit_factor { get; set; }
        public decimal recovery { get; set; }
        public decimal max_drow_down { get; set; }
        public int profit_positions { get; set; }
        public int long_profit_positions { get; set; }
        public int short_profit_positions { get; set; }
        public int loss_positions { get; set; }
        public int long_loss_positions { get; set; }
        public int short_loss_positions { get; set; }
    }

    public class JsonCandlePosition
    {
        public int number { get; set; }
        public string side { get; set; }
        public string time { get; set; }
        public decimal open_volume { get; set; }
        public decimal profit { get; set; }
    }

    public class JsonStop
    {
        public int number { get; set; }
        public string side { get; set; }
        public string open_date_time { get; set; }
        public decimal volume { get; set; }
        public decimal activation_price { get; set; }
        public decimal stop_level { get; set; }
    }

    public class JsonCandleResult
    {
        public string time_close { get; set; }
        public JsonCandle candle_data { get; set; }
        public List<JsonStop> stops { get; set; }
        public List<JsonCandlePosition> opened_positions {  get; set; }
        public List<JsonCandlePosition> closed_positions { get; set; }
        public JsonEquity equity { get; set; }
    }
}
