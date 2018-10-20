using DogPlatform.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogRunService
{
    public class HistoryKlinePools
    {
        /// <summary>
        /// symbol, 1min, DateTime.Now().toString("yyyyMMddHHmm");
        /// </summary>
        private static ConcurrentDictionary<string, HistoryKlineData> historyKlines = new ConcurrentDictionary<string, HistoryKlineData>();

        public static string GetKey(CommonSymbols symbol, string period)
        {
            return symbol.BaseCurrency + "-" + symbol.QuoteCurrency + "-" + period;
        }

        public static void Init(string key, List<HistoryKline> data)
        {
            historyKlines.TryAdd(key, new HistoryKlineData()
            {
                Data = data,
                Date = DateTime.Now
            });
            historyKlines[key] = new HistoryKlineData()
            {
                Data = data,
                Date = DateTime.Now
            };
        }

        public static HistoryKlineData Get(string key)
        {
            historyKlines.TryGetValue(key, out HistoryKlineData value);
            return value;
        }
    }

    public class HistoryKlineData
    {
        public DateTime Date { get; set; }
        public List<HistoryKline> Data { get; set; }
    }
}
