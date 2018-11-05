using DogPlatform.Model;
using DogService;
using DogService.Dao;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogRunService
{
    public class AnalyzeResult
    {
        static ILog logger = LogManager.GetLogger(typeof(AnalyzeResult));

        /// <summary>
        /// 分析后的拐点数据
        /// </summary>
        public decimal NowPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal MinPrice { get; set; }
        public decimal Minute30MaxPrice { get; set; }
        public decimal Minute30MinPrice { get; set; }
        /// <summary>
        /// 原始数据
        /// </summary>
        public List<HistoryKline> HistoryKlines { get; set; }

        /// <summary>
        /// 当购买或者出售时候,需要一个分析结果, 供判断是否做多,或者做空
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="isBuy"></param>
        /// <returns></returns>
        public static AnalyzeResult GetAnalyzeResult(CommonSymbols symbol)
        {
            var historyKlines = new KlineDao().List24HourKline(symbol.QuoteCurrency, symbol.BaseCurrency);
            var idDate = Utils.GetDateById(historyKlines[0].Id);
            var now = DateTime.Now;
            if (historyKlines == null
                || historyKlines.Count < 100
                || idDate < now.AddMinutes(-1))
            {
                if (idDate.Minute == now.Minute)
                {
                    logger.Error($"----------{symbol.BaseCurrency}{symbol.QuoteCurrency}--------------> analyzeResult 为 null  idDate.Minute == now.Minute, {idDate.Second}, {now.Second}");
                }
                return null;
            }

            var minute30Klines = historyKlines.FindAll(it => it.Id > Utils.GetIdByDate(DateTime.Now.AddMinutes(-30)));

            AnalyzeResult analyzeResult = new AnalyzeResult()
            {
                NowPrice = historyKlines[0].Close,
                MaxPrice = historyKlines.Max(it => it.Close),
                MinPrice = historyKlines.Min(it => it.Close),
                Minute30MaxPrice = minute30Klines.Max(it => it.Close),
                Minute30MinPrice = minute30Klines.Min(it => it.Close),
                HistoryKlines = historyKlines,
            };
            return analyzeResult;
        }
    }
}
