using System.Collections.Generic;
using Google.Protobuf.Collections;

namespace OsEngine.OsTrader.Panels.JsonData
{
    public class JsonRunData
    {
        public JsonDataParameters data_parameters;
        public Dictionary<string, string> robot_parameters;
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

    public class JsonCandleStatistics
    {
        public decimal sharp_ratio { get; set; }
        public decimal max_sma_deviation { get; set; }
        public decimal profit_factor { get; set; }
        public decimal recovery { get; set; }
        public decimal max_drow_down { get; set; }
    }

    public class JsonCandlePosition
    {
        public bool opened { get; set; }
        public bool closed { get; set; }
        public string open_side { get; set; }
        public string close_side { get; set; }
        public decimal open_volume { get; set; }
    }

    public class JsonCandleResult
    {
        public string time_close { get; set; }
        public JsonCandle candle_data { get; set; }
        public JsonCandlePosition position {  get; set; }
        public JsonEquity equity { get; set; }
        public JsonCandleStatistics statistics { get; set; }
    }
}
