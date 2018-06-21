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

        public static void InitOneKine(CommonSymbols symbol)
        {
            try
            {
                var begin = DateTime.Now;
                PlatformApi api = PlatformApi.GetInstance("xx"); // 下面api和角色无关. 随便指定一个xx
                var period = "1min";
                var klines = api.GetHistoryKline(symbol.BaseCurrency + symbol.QuoteCurrency, period, 50);
                var key = HistoryKlinePools.GetKey(symbol, period);
                //HistoryKlinePools.Init(key, klines);

                var totalMilliseconds = (DateTime.Now - begin).TotalMilliseconds;
                //if ((DateTime.Now - begin).TotalSeconds > 2)
                //{
                //    logger.Error("一次请求时间太长,达到：" + totalMilliseconds);
                //}
                // 记录到数据库
                Record(symbol.BaseCurrency, klines[0]);

                var dao = new KlineDao();
                var lastKlines = dao.List24HourKline(symbol.BaseCurrency);
                if (lastKlines.Count < 900)
                {
                    logger.Error($"{symbol.BaseCurrency}数据量太少，无法分析啊：" + totalMilliseconds);
                }
                if (lastKlines.Count > 600)
                {
                    HistoryKlinePools.Init(key, lastKlines);
                }

                totalMilliseconds = (DateTime.Now - begin).TotalMilliseconds;
                if ((DateTime.Now - begin).TotalSeconds > 4)
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
    }
}
