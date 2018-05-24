using DogPlatform;
using DogPlatform.Model;
using DogService;
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
                PlatformApi api = PlatformApi.GetInstance("xx"); // 下面api和角色无关. 随便指定一个xx
                var period = "1min";
                var klines = api.GetHistoryKline(symbol.BaseCurrency + symbol.QuoteCurrency, period);
                var key = HistoryKlinePools.GetKey(symbol, period);
                HistoryKlinePools.Init(key, klines);

                // 记录到数据库
                //Record(symbol.BaseCurrency, klines[0]);
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
