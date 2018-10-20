using DogPlatform;
using DogPlatform.Model;
using DogService;
using DogService.Dao;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DogRunService.Helper
{
    public class KlineUtils
    {
        static ILog logger = LogManager.GetLogger(typeof(KlineUtils));

        public static void Begin()
        {
            logger.Info("----------------------  begin  --------------------------------");
            // 初始化
            var symbols = CoinUtils.GetAllCommonSymbols();

            // 定时任务， 不停的获取最新数据， 以供分析使用
            foreach (var symbol in symbols)
            {
                RunHistoryKline(symbol);
            }
        }

        private static void RunHistoryKline(CommonSymbols symbol)
        {
            Task.Run(() =>
            {
                var countSuccess = 0;
                var countError = 0;
                PlatformApi api = PlatformApi.GetInstance("xx"); // 下面api和角色无关. 随便指定一个xx
                var begin = DateTime.Now;
                while (true)
                {
                    try
                    {
                        var period = "1min";
                        var klines = api.GetHistoryKline(symbol.BaseCurrency + symbol.QuoteCurrency, period);
                        var key = HistoryKlinePools.GetKey(symbol, period);
                        HistoryKlinePools.Init(key, klines);
                        countSuccess++;
                    }
                    catch (Exception ex)
                    {
                        countError++;
                    }
                    if (countSuccess % 20 == 0)
                    {
                        Console.WriteLine($"RunHistoryKline -> {symbol.BaseCurrency}, Success:{countSuccess}, Error:{countError}, AvageSecond:{(DateTime.Now - begin).TotalSeconds / (countSuccess + countError)}");
                    }
                    Thread.Sleep(1000 * 6);
                }
            });
        }

        public static void InitKlineInToPool(CommonSymbols symbol)
        {
            try
            {
                var period = "1min";
                var key = HistoryKlinePools.GetKey(symbol, period);

                var dao = new KlineDao();
                var lastKlines = dao.List24HourKline(symbol.BaseCurrency);
                if (lastKlines.Count < 900)
                {
                    logger.Error($"{symbol.BaseCurrency},{symbol.QuoteCurrency}数据量太少{lastKlines.Count}，无法分析啊：");
                }
                if (lastKlines.Count > 600)
                {
                    HistoryKlinePools.Init(key, lastKlines);
                }
            }
            catch (Exception ex)
            {
                logger.Error("InitOneKine --> " + ex.Message, ex);
            }
        }

        /// <summary>
        /// 获取行情数据
        /// </summary>
        /// <param name="symbol"></param>
        public static void InitOneKine(CommonSymbols symbol)
        {
            try
            {
                var begin = DateTime.Now;
                PlatformApi api = PlatformApi.GetInstance("xx"); // 下面api和角色无关. 随便指定一个xx
                var period = "1min";
                var klines = api.GetHistoryKline(symbol.BaseCurrency + symbol.QuoteCurrency, period, 10);
                var key = HistoryKlinePools.GetKey(symbol, period);
                //HistoryKlinePools.Init(key, klines);

                var totalMilliseconds = (DateTime.Now - begin).TotalMilliseconds;
                if ((DateTime.Now - begin).TotalSeconds > 5)
                {
                    logger.Error("一次请求时间太长,达到：" + totalMilliseconds);
                }
                // 记录到数据库， 记录最近得数据。
                Record(symbol.BaseCurrency, klines[0]);

                var dao = new KlineDao();
                var lastKlines = dao.List24HourKline(symbol.BaseCurrency);
                var findList = lastKlines.FindAll(it => klines.Find(item => item.Id == it.Id) != null).ToList();
                foreach (var kline in klines)
                {
                    var finds = findList.FindAll(it => it.Id == kline.Id);
                    if (finds.Count > 1)
                    {
                        // 删除，新增
                        new KlineDao().DeleteAndRecordKlines(symbol.BaseCurrency, kline);
                    }
                    else if (finds.Count == 1)
                    {
                        if (finds[0].Low != kline.Low || finds[0].High != kline.High || finds[0].Open != kline.Open || finds[0].Close != kline.Close)
                        {
                            // 删除新增
                            new KlineDao().DeleteAndRecordKlines(symbol.BaseCurrency, kline);
                        }
                    }
                    else
                    {
                        // 直接新增
                        Record(symbol.BaseCurrency, kline);
                    }
                }

                if (lastKlines.Count < 900)
                {
                    logger.Error($"{symbol.BaseCurrency}数据量太少{lastKlines.Count}，无法分析啊：" + totalMilliseconds);
                }
                if (lastKlines.Count > 600)
                {
                    HistoryKlinePools.Init(key, lastKlines);
                }

                totalMilliseconds = (DateTime.Now - begin).TotalMilliseconds;
                if ((DateTime.Now - begin).TotalSeconds > 9)
                {
                    logger.Error("一次请求时间太长 含插入数据库,达到：" + totalMilliseconds);
                }
            }
            catch (Exception ex)
            {
                logger.Error("InitOneKine --> " + ex.Message, ex);
            }
        }

        public static void Record(string coin, HistoryKline line)
        {
            var dao = new KlineDao();
            dao.CheckTable(coin);
            dao.Record(coin, line);
        }


        public static void CheckTableExistAndCreate(CommonSymbols symbol)
        {
            new KlineDao().CheckTableExistsAndCreate(symbol.QuoteCurrency, symbol.BaseCurrency);
        }

        /// <summary>
        /// 获取行情数据， 防止频繁rest， 因为api调用次数太多。
        /// </summary>
        /// <param name="symbol"></param>
        public static void InitMarketInDB(CommonSymbols symbol)
        {
            try
            {
                var dao = new KlineDao();
                var dogMoreBuyDao = new DogMoreBuyDao();
                var dogEmptySellDao = new DogEmptySellDao();

                // 去数据库中拉取数据， 判断是否超过3分钟，  或者是否离目标差4%，
                var lastKlines = dao.List24HourKline(symbol.QuoteCurrency, symbol.BaseCurrency);
                var minutesAfterCount = lastKlines.FindAll(it => Utils.GetDateById(it.Id) > DateTime.Now.AddMinutes(-3)).Count;
                if (minutesAfterCount > 0)
                {
                    var smallBuy = dogMoreBuyDao.GetSmallestDogMoreBuy(symbol.QuoteCurrency, symbol.BaseCurrency);
                    var nearSellOrBuy = true;
                    if (smallBuy != null && (lastKlines[0].Close % smallBuy.BuyTradePrice > (decimal)1.036 || smallBuy.BuyTradePrice % lastKlines[0].Close > (decimal)1.042))
                    {
                        nearSellOrBuy = false;
                    }
                    if (nearSellOrBuy)
                    {
                        var bigSell = dogEmptySellDao.GetBiggestDogEmptySell(symbol.QuoteCurrency, symbol.BaseCurrency);
                        if (bigSell != null && (lastKlines[0].Close % bigSell.SellTradePrice > (decimal)1.050 || lastKlines[0].Close % bigSell.SellTradePrice > (decimal)1.035))
                        {
                            nearSellOrBuy = false;
                        }
                    }
                    // 在3分钟内有数据， 并且没有需要做多或做空的。
                    if (!nearSellOrBuy)
                    {
                        return;
                    }
                }

                var begin = DateTime.Now;

                PlatformApi api = PlatformApi.GetInstance("xx"); // 下面api和角色无关. 随便指定一个xx
                var period = "1min";
                var klines = api.GetHistoryKline(symbol.BaseCurrency + symbol.QuoteCurrency, period, 10);

                // 记录下， 获取api数据太长的数据
                var totalMilliseconds = (DateTime.Now - begin).TotalMilliseconds;
                if (totalMilliseconds > 5 * 1000)
                {
                    logger.Error($"一次请求时间太长,达到：{totalMilliseconds}ms");
                }
                begin = DateTime.Now;

                var findList = lastKlines.FindAll(it => klines.Find(item => item.Id == it.Id) != null).ToList();

                klines.Sort((a, b) => (int)(a.Id - b.Id));
                foreach (var kline in klines)
                {
                    var finds = findList.FindAll(it => it.Id == kline.Id);
                    if (finds.Count > 1)
                    {
                        // 删除，新增
                        dao.DeleteAndRecordKlines(symbol.QuoteCurrency, symbol.BaseCurrency, kline);
                    }
                    else if (finds.Count == 1)
                    {
                        if (finds[0].Low != kline.Low || finds[0].High != kline.High || finds[0].Open != kline.Open || finds[0].Close != kline.Close)
                        {
                            // 删除新增
                            dao.DeleteAndRecordKlines(symbol.QuoteCurrency, symbol.BaseCurrency, kline);
                        }
                    }
                    else
                    {
                        // 新增
                        dao.DeleteAndRecordKlines(symbol.QuoteCurrency, symbol.BaseCurrency, kline);
                    }
                }

                totalMilliseconds = (DateTime.Now - begin).TotalMilliseconds;
                if (totalMilliseconds > 3 * 1000)
                {
                    logger.Error("插入数据库时间太长,达到：" + totalMilliseconds);
                }
            }
            catch (Exception ex)
            {
                logger.Error("InitMarketInDB --> " + ex.Message, ex);
            }
        }
    }
}
