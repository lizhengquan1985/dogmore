using DogPlatform;
using DogPlatform.Model;
using DogService.DateTypes;
using log4net;
using log4net.Config;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RestMarketConsole
{
    class Program
    {
        static ILog logger = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            // 注册日志
            XmlConfigurator.Configure(new FileInfo("log4net.config"));
            logger.Info("-----------------------Main---------------------------");

            CoinUtils.Init();

            var client = new RestClient("http://118.31.44.235/api/Control/listAll");
            RestRequest req = new RestRequest(Method.GET);
            req.AddHeader("content-type", "application/json");
            req.AddHeader("cache-type", "no-cache");
            var response = client.ExecuteTaskAsync(req).Result;

            var controlList = JsonConvert.DeserializeObject<List<DogControl>>(response.Content);

            var commonSymbols = new List<CommonSymbol>();
            foreach (var control in controlList)
            {
                commonSymbols.Add(new CommonSymbol
                {
                    BaseCurrency = control.SymbolName,
                    QuoteCurrency = control.QuoteCurrency
                });
            }

            // 开始
            RunCoin(commonSymbols);

            Console.ReadLine();
        }

        public static List<CommonSymbol> InitUsdtData()
        {
            // 准备好各种对
            var symbols = CoinUtils.GetAllCommonSymbols("usdt");
            return symbols.ToList();
        }

        public static List<CommonSymbol> InitBtcData(List<string> allPre120)
        {
            // 准备好各种对
            var btcSymbols = CoinUtils.GetAllCommonSymbols("btc");
            btcSymbols.RemoveAll(it => !allPre120.Contains(it.BaseCurrency));
            return btcSymbols.ToList();
        }

        public static List<CommonSymbol> InitEthData(List<string> allPre120)
        {
            // 准备好各种对
            var ethSymbols = CoinUtils.GetAllCommonSymbols("eth");
            ethSymbols.RemoveAll(it => !allPre120.Contains(it.BaseCurrency));
            return ethSymbols.ToList();
        }

        public static List<CommonSymbol> InitHtData()
        {
            // 准备好各种对
            var symbols = CoinUtils.GetAllCommonSymbols("ht");
            return symbols.ToList();
        }

        private static void RunCoin(List<CommonSymbol> symbols)
        {
            Task.Run(() =>
            {
                var count = 0;
                while (true)
                {
                    var begin = DateTime.Now;

                    for (var i = 0; i < symbols.Count; i++)
                    {
                        var symbol = symbols[i];

                        try
                        {
                            InitMarketInDB(i, symbol);
                            Console.WriteLine(count++);
                        }
                        catch (Exception ex)
                        {
                            logger.Error("RunCoin:  " + ex.Message, ex);
                        }

                        // 暂停500毫秒
                        Thread.Sleep(1000);
                    }
                }
            });
        }

        public static void InitMarketInDB(int index, CommonSymbol symbol, bool forceUpdate = false)
        {
            try
            {
                PlatformApi api = PlatformApi.GetInstance("xx"); // 下面api和角色无关. 随便指定一个xx
                var period = "1min";
                var count = 12;
                var klines = api.GetHistoryKline(symbol.BaseCurrency + symbol.QuoteCurrency, period, count);
                if (klines == null || klines.Count == 0)
                {
                    return;
                }

                var client = new RestClient("http://118.31.44.235/api/Control/newSymbolData");
                RestRequest req = new RestRequest(Method.POST);
                req.AddHeader("content-type", "application/json");
                req.AddHeader("cache-type", "no-cache");
                req.AddJsonBody(new
                {
                    BaseCurrency = symbol.BaseCurrency,
                    QuoteCurrency = symbol.QuoteCurrency,
                    HistoryKlines = klines
                });
                var response = client.ExecuteTaskAsync(req).Result;
            }
            catch (Exception ex)
            {
                logger.Error("InitMarketInDB --> " + ex.Message, ex);
            }
        }
    }
}
