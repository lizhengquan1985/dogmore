using DogPlatform.Model;
using DogRunService.Helper;
using DogService;
using DogService.Dao;
using DogService.DateTypes;
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
        public static AnalyzeResult GetAnalyzeResult(CommonSymbol symbol, bool refresh = false)
        {
            var historyKlines = new KlineDao().List24HourKline(symbol.QuoteCurrency, symbol.BaseCurrency);
            if (historyKlines == null || historyKlines.Count == 0)
            {
                return null;
            }
            var idDate = Utils.GetDateById(historyKlines[0].Id);
            // 判断最小购买的价格是否接近， 如果接近，再去获取一次
            if (refresh && (idDate > DateTime.Now.AddSeconds(-120) || idDate < DateTime.Now.AddSeconds(-900)))
            {
                KlineUtils.InitMarketInDB(0, symbol);
                historyKlines = new KlineDao().List24HourKline(symbol.QuoteCurrency, symbol.BaseCurrency);
                idDate = Utils.GetDateById(historyKlines[0].Id);
            }

            var now = DateTime.Now;
            if (historyKlines == null
                || historyKlines.Count < 100
                || idDate < now.AddSeconds(-60))
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
                HistoryKlines = historyKlines,
            };
            return analyzeResult;
        }

        public bool CheckCanBuyForHuiDiao(DogEmptySell dogEmptySell)
        {
            // 是否回掉了，可以购买。 肯定要是最低点回掉

            // 找到最近24小时，并且是出售之后的价格数据
            var klines = HistoryKlines.FindAll(it => it.Id > dogEmptySell.Id);
            if (klines.Count == 0)
            {
                return false;
            }

            // 判断是否有最小值，且小于nowPrice
            var min = klines.Min(it => it.Close);
            return NowPrice > min * (decimal)1.005 && NowPrice * (decimal)1.04 < dogEmptySell.SellTradePrice;
        }

        public bool CheckCanBuyForHuiDiao()
        {
            if (HistoryKlines.Count < 100)
            {
                Console.WriteLine($"    由于数据量太少 无法分析是否回掉，不能确定是否可以出售");
                return false;
            }

            var min = HistoryKlines.Min(it => it.Close);

            var minHuidiao = (decimal)1.005;
            var maxHuidiao = (decimal)1.888;

            return NowPrice > min * minHuidiao && NowPrice < min * maxHuidiao;
        }

        public bool CheckCanSellForHuiDiao(DogMoreBuy dogMoreBuy)
        {
            // 是否回掉了，可以出售。 肯定要是最高点回掉

            if (dogMoreBuy == null || dogMoreBuy.BuyTradePrice <= 0)
            {
                return false;
            }

            // 找到最近24小时，并且是出售之后的价格数据
            var klines = HistoryKlines.FindAll(it => it.Id > dogMoreBuy.Id);
            if (klines.Count == 0)
            {
                return false;
            }

            // 判断是否有最小值，且小于nowPrice
            var min = klines.Min(it => it.Close);
            var max = klines.Max(it => it.Close);

            var minHuidiao = (decimal)1.005;
            var maxHuidiao = (decimal)1.03;
            var upPercent = NowPrice / dogMoreBuy.BuyTradePrice;
            if (upPercent <= (decimal)1.02)
            {
                // 这个太差了吧.
                return false;
            }

            var huidiao = 1 + ((NowPrice / dogMoreBuy.BuyTradePrice) - 1) / 10;
            huidiao = Math.Max(huidiao, minHuidiao);
            huidiao = Math.Min(huidiao, maxHuidiao);

            return NowPrice * huidiao < max && NowPrice * upPercent * 2 > max;
        }

        public bool CheckCanSellForHuiDiao()
        {
            if (HistoryKlines.Count < 100)
            {
                Console.WriteLine($"    由于数据量太少 无法分析是否回掉，不能确定是否可以出售");
                return false;
            }

            var max = HistoryKlines.Max(it => it.Close);

            var minHuidiao = (decimal)1.008;
            var maxHuidiao = (decimal)1.125;

            return NowPrice * minHuidiao < max && NowPrice * maxHuidiao > max;
        }
    }
}
