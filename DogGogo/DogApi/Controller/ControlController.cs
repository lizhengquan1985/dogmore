using DogAccount;
using DogPlatform;
using DogPlatform.Model;
using DogRunService;
using DogRunService.Helper;
using DogService;
using DogService.Dao;
using DogService.DateTypes;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace DogApi.Controller
{
    public class ControlController : ApiController
    {
        static ILog logger = LogManager.GetLogger(typeof(ControlController));

        [HttpPost]
        [ActionName("new")]
        public async Task Create([FromBody] DogControl dogControl)
        {
            try
            {
                // 默认 1.05, 
                dogControl.SymbolLevel = Math.Max(0, dogControl.SymbolLevel);
                dogControl.SymbolLevel = Math.Min(20, dogControl.SymbolLevel);
                if (dogControl.LadderBuyPercent < (decimal)(1.06 + 0.001 * dogControl.SymbolLevel))
                {
                    dogControl.LadderBuyPercent = (decimal)(1.06 + 0.001 * dogControl.SymbolLevel);
                }

                dogControl.UpIndex = Math.Max(0, dogControl.UpIndex);
                dogControl.UpIndex = Math.Min(20, dogControl.UpIndex);

                dogControl.DownIndex = Math.Max(0, dogControl.DownIndex);
                dogControl.DownIndex = Math.Min(20, dogControl.DownIndex);

                // 每点增加0.01
                // usdt  1.08起售， 
                // btc   1.05 
                // eth   1.05 
                // ht   1.05
                if (dogControl.QuoteCurrency == "usdt")
                {
                    dogControl.LadderSellPercent = Math.Max((decimal)dogControl.LadderSellPercent, (decimal)(1.08 + dogControl.UpIndex * 0.01));
                }
                else
                {
                    dogControl.LadderSellPercent = Math.Max((decimal)dogControl.LadderSellPercent, (decimal)(1.05 + dogControl.UpIndex * 0.01));
                }
                dogControl.MaxInputPrice = Math.Min(dogControl.MaxInputPrice, dogControl.HistoryMax - (dogControl.HistoryMax - dogControl.HistoryMin) * dogControl.SymbolLevel / 20);

                await new DogControlDao().CreateDogControl(dogControl);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                throw ex;
            }
        }

        [HttpGet]
        [ActionName("list")]
        public async Task<object> List(string quoteCurrency)
        {
            try
            {
                var res = await new DogControlDao().ListDogControl(quoteCurrency);
                res = res.OrderBy(it => it.SymbolName).ToList();
                var nowPriceList = new DogNowPriceDao().ListDogNowPrice(quoteCurrency);
                Dictionary<string, decimal> closeDic = new Dictionary<string, decimal>();
                foreach (var item in nowPriceList)
                {
                    if (item.QuoteCurrency != quoteCurrency)
                    {
                        continue;
                    }
                    closeDic.Add(item.SymbolName, item.NowPrice);
                }
                var pre50 = CoinsPre45.GetPre40Coins();
                var pre80 = CoinsPre45.GetPre80Coins();
                var pre120 = CoinsPre45.GetPre120Coins();

                var dogCoinList = await new DogCoinDao().ListDogCoin();

                foreach (var item in res)
                {
                    var dogCoin = dogCoinList.Find(it => it.SymbolName == item.SymbolName);
                    var levelData = 10000;
                    if (dogCoin != null)
                    {
                        levelData = dogCoin.Level;
                    }
                    if (pre50.Contains(item.SymbolName))
                    {
                        item.Memo = pre50.IndexOf(item.SymbolName) < 20 ? "前20 -- 推荐 2level" : "前40 -- 推荐 4level";
                    }
                    else if (pre80.Contains(item.SymbolName))
                    {
                        item.Memo = pre80.IndexOf(item.SymbolName) < 20 ? "前60 -- 推荐 6level" : "前80 -- 推荐 8level";
                    }
                    else if (pre120.Contains(item.SymbolName))
                    {
                        item.Memo = pre120.IndexOf(item.SymbolName) < 20 ? "前100 -- 推荐 10level" : "前120 -- 推荐 12level";
                    }
                    else
                    {
                        item.Memo = "120开外 -- 推荐 14level";
                    }

                    item.Memo += $" --------------- {levelData}";
                }

                var notInPre50 = res.FindAll(it => pre50.IndexOf(it.SymbolName) < 0);
                var notInPre80 = res.FindAll(it => pre80.IndexOf(it.SymbolName) < 0);
                var notInPre120 = res.FindAll(it => pre120.IndexOf(it.SymbolName) < 0);

                var notInControl50 = pre50.FindAll(coin => res.Find(it => it.SymbolName == coin) == null);
                var notInControl80 = pre80.FindAll(coin => res.Find(it => it.SymbolName == coin) == null);
                var notInControl120 = pre120.FindAll(coin => res.Find(it => it.SymbolName == coin) == null);

                var notInPre = res.FindAll(it => pre50.IndexOf(it.SymbolName) < 0 && pre80.IndexOf(it.SymbolName) < 0 && pre120.IndexOf(it.SymbolName) < 0);

                var commonSymbols = CoinUtils.GetAllCommonSymbols(quoteCurrency);
                var notControlButRun = commonSymbols.FindAll(it => res.Find(item => item.SymbolName == it.BaseCurrency) == null).Select(it => it.BaseCurrency).ToList();

                var commonSymbols22 = CoinUtils.GetAllCommonSymbols22(quoteCurrency);
                pre50.RemoveAll(it => string.IsNullOrEmpty(it) || commonSymbols.Find(item => item.BaseCurrency == it) != null);
                pre50.RemoveAll(it => commonSymbols22.Find(item => item.BaseCurrency == it) == null);

                pre80.RemoveAll(it => string.IsNullOrEmpty(it) || commonSymbols.Find(item => item.BaseCurrency == it) != null);
                pre80.RemoveAll(it => commonSymbols22.Find(item => item.BaseCurrency == it) == null);

                pre120.RemoveAll(it => string.IsNullOrEmpty(it) || commonSymbols.Find(item => item.BaseCurrency == it) != null);
                pre120.RemoveAll(it => commonSymbols22.Find(item => item.BaseCurrency == it) == null);


                notInControl50.RemoveAll(it => string.IsNullOrEmpty(it) || commonSymbols.Find(item => item.BaseCurrency == it) == null);
                notInControl80.RemoveAll(it => string.IsNullOrEmpty(it) || commonSymbols.Find(item => item.BaseCurrency == it) == null);
                notInControl120.RemoveAll(it => string.IsNullOrEmpty(it) || commonSymbols.Find(item => item.BaseCurrency == it) == null);

                var notInControlList = commonSymbols.FindAll(it => res.Find(item => item.SymbolName == it.BaseCurrency) == null).Select(it => it.BaseCurrency).ToList();
                var notInControlDic = new Dictionary<string, int>();
                foreach (var item in notInControlList)
                {
                    try
                    {
                        var dogCoin = dogCoinList.Find(it => it.SymbolName == item);
                        notInControlDic.Add(item, dogCoin.Level);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex.Message, ex);
                        logger.Error(JsonConvert.SerializeObject(item));
                        continue;
                    }
                }

                var usdtCommonSymbols = CoinUtils.GetAllCommonSymbols("usdt");
                var notInControlButUsdtDic = new Dictionary<string, int>();
                foreach (var item in notInControlList)
                {
                    var dogCoin = dogCoinList.Find(it => it.SymbolName == item);
                    var find = usdtCommonSymbols.Find(it => it.BaseCurrency == item);
                    if (find != null)
                    {
                        notInControlButUsdtDic.Add(item, dogCoin.Level);
                    }
                }

                var btcCommonSymbols = await new DogControlDao().ListDogControl("btc");
                var notInControlButBtcDic = new Dictionary<string, int>();
                foreach (var item in notInControlList)
                {
                    var dogCoin = dogCoinList.Find(it => it.SymbolName == item);
                    var find = btcCommonSymbols.Find(it => it.SymbolName == item);
                    if (find != null)
                    {
                        notInControlButBtcDic.Add(item, dogCoin.Level);
                    }
                }

                var ethCommonSymbols = await new DogControlDao().ListDogControl("eth");
                var notInControlButEthDic = new Dictionary<string, int>();
                foreach (var item in notInControlList)
                {
                    var dogCoin = dogCoinList.Find(it => it.SymbolName == item);
                    var find = ethCommonSymbols.Find(it => it.SymbolName == item);
                    if (find != null)
                    {
                        notInControlButEthDic.Add(item, dogCoin.Level);
                    }
                }

                return new
                {
                    list = res,
                    closeDic,
                    noRunPre50 = pre50,
                    noRunPre80 = pre80,
                    noRunPre120 = pre120,
                    notInControl50,
                    notInControl80,
                    notInControl120,
                    notInControl = notInControlDic.OrderBy(it => it.Value),
                    notInControlButUsdt = notInControlButUsdtDic.OrderBy(it => it.Value),
                    notInControlButEth = notInControlButEthDic.OrderBy(it => it.Value),
                    notInControlButBtc = notInControlButBtcDic.OrderBy(it => it.Value),
                    hasControlButNotInPre = notInPre.Select(it => it.SymbolName).ToList(),
                    allItems = res.Select(it => it.SymbolName).ToList()
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                throw ex;
            }
        }

        [HttpGet]
        [ActionName("listAll")]
        public async Task<object> ListAll()
        {
            try
            {
                var res = new List<DogControlMemo>();

                var usdt = await new DogControlDao().ListDogControl("usdt");
                usdt = usdt.OrderBy(it => it.SymbolName).ToList();
                var btc = await new DogControlDao().ListDogControl("btc");
                btc = btc.OrderBy(it => it.SymbolName).ToList();
                var eth = await new DogControlDao().ListDogControl("eth");
                eth = eth.OrderBy(it => it.SymbolName).ToList();
                var ht = await new DogControlDao().ListDogControl("ht");
                ht = ht.OrderBy(it => it.SymbolName).ToList();

                res.AddRange(usdt);
                res.AddRange(btc);
                res.AddRange(eth);
                res.AddRange(ht);
                return res;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                throw ex;
            }
        }

        [HttpPut]
        [ActionName("setUnvalid")]
        public async Task<string> SetUnvalid(string symbolName, string quoteCurrency)
        {
            try
            {
                await new DogControlDao().SetUnvalid(symbolName, quoteCurrency);
                return "修改完成";
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                return ex.Message;
            }
        }

        [HttpGet]
        [ActionName("refreshHistoryMaxMin")]
        public async Task RefreshHistoryMaxMin(string symbolName, string quoteCurrency)
        {
            try
            {
                // 先计算最近500天的数据, 如果数据量少, 则计算4小时数据1000天
                PlatformApi api = PlatformApi.GetInstance("xx");
                var klines = api.GetHistoryKline(symbolName + quoteCurrency, "1day", 500);
                if (klines.Count < 180)
                {
                    klines = api.GetHistoryKline(symbolName + quoteCurrency, "4hour", 1000);
                }
                var min = decimal.MinValue;
                var max = decimal.MaxValue;

                min = klines.Where(it => it.Low > min).Min(it => it.Low);
                min = klines.Where(it => it.Low > min).Min(it => it.Low);
                min = klines.Where(it => it.Low > min).Min(it => it.Low);

                max = klines.Where(it => it.High < max).Max(it => it.High);
                max = klines.Where(it => it.High < max).Max(it => it.High);
                max = klines.Where(it => it.High < max).Max(it => it.High);

                // 判断max
                var maxNotSell = new DogMoreBuyDao().GetMaxPriceOfNotSellFinished(quoteCurrency, symbolName);
                if (maxNotSell > max)
                {
                    max = maxNotSell;
                }

                var inDB = new DogControlDao().GetDogControl(symbolName, quoteCurrency);
                if (inDB == null)
                {
                    inDB = new DogControl()
                    {
                        SymbolName = symbolName,
                        QuoteCurrency = quoteCurrency,
                        HistoryMax = max,
                        HistoryMin = min
                    };
                    await new DogControlDao().CreateDogControl(inDB);
                }
                else
                {
                    inDB.HistoryMax = max;
                    inDB.HistoryMin = min;

                    inDB.MaxInputPrice = Math.Min(inDB.MaxInputPrice, inDB.HistoryMax - (inDB.HistoryMax - inDB.HistoryMin) * inDB.SymbolLevel / 20);
                    await new DogControlDao().CreateDogControl(inDB);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                throw ex;
            }
        }

        [HttpGet]
        [ActionName("refreshEmpty")]
        public async Task RefreshEmpty(string quoteCurrency)
        {
            try
            {
                var nowPriceList = new DogNowPriceDao().ListDogNowPrice(quoteCurrency);
                Dictionary<string, decimal> closeDic = new Dictionary<string, decimal>();
                foreach (var item in nowPriceList)
                {
                    if (item.QuoteCurrency != quoteCurrency)
                    {
                        continue;
                    }
                    if (item.NowTime < Utils.GetIdByDate(DateTime.Now.AddHours(-1)))
                    {
                        continue;
                    }
                    closeDic.Add(item.SymbolName, item.NowPrice);
                }

                var commonSymbols = CoinUtils.GetAllCommonSymbols(quoteCurrency);
                foreach (var item in commonSymbols)
                {
                    try
                    {
                        var inDB = new DogControlDao().GetDogControl(item.BaseCurrency, quoteCurrency);
                        if (inDB == null)
                        {
                            continue;
                        }
                        else if (closeDic.ContainsKey(item.BaseCurrency))
                        {
                            inDB.EmptyPrice = closeDic[item.BaseCurrency] * 3;
                            inDB.EmptyPrice = Math.Min(inDB.EmptyPrice, inDB.HistoryMax * (decimal)1.2);
                            inDB.EmptyPrice = Math.Max(inDB.EmptyPrice, 3 * inDB.HistoryMin);
                            inDB.LadderBuyPercent = Math.Max(inDB.LadderBuyPercent, (decimal)1.07);
                            inDB.LadderBuyPercent = Math.Min(inDB.LadderBuyPercent, (decimal)1.09);
                            inDB.LadderSellPercent = Math.Min(inDB.LadderSellPercent, (decimal)1.25);
                            inDB.LadderSellPercent = Math.Max(inDB.LadderSellPercent, (decimal)1.15);
                            await new DogControlDao().CreateDogControl(inDB);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.IndexOf("管控数据出错") < 0)
                        {
                            logger.Error(ex.Message, ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                throw ex;
            }
        }

        [HttpGet]
        [ActionName("initAccountInfo")]
        public async Task<object> InitAccountInfo(string userName, string quoteCurrency, string sort, bool stat)
        {
            try
            {
                PlatformApi api = PlatformApi.GetInstance(userName);
                var accountInfo = api.GetAccountBalance(AccountConfigUtils.GetAccountConfig(userName).MainAccountId);

                var nowPriceList = new DogNowPriceDao().ListDogNowPrice(quoteCurrency);

                var result = new List<Dictionary<string, object>>();

                foreach (var balanceItem in accountInfo.Data.list)
                {
                    try
                    {
                        if (balanceItem.balance < (decimal)0.00001 || balanceItem.type == "frozen")
                        {
                            continue;
                        }

                        var nowPriceItem = nowPriceList.Find(it => it.SymbolName == balanceItem.currency);

                        if (stat && balanceItem.currency == "usdt")
                        {
                            new DogStatSymbolDao().CreateDogStatSymbol(new DogStatSymbol
                            {
                                Amount = balanceItem.balance,
                                CreateTime = DateTime.Now,
                                EarnAmount = balanceItem.balance,
                                StatDate = DateTime.Now.ToString("yyyy-MM-dd"),
                                SymbolName = balanceItem.currency,
                                UserName = userName
                            });
                        }

                        if (nowPriceItem == null)
                        {
                            continue;
                        }

                        var totalQuantity = new DogMoreBuyDao().GetBuyQuantityOfDogMoreBuyIsNotFinished(userName, balanceItem.currency);
                        var kongAmount = new DogEmptySellDao().GetSellAmountOfDogEmptySellIsNotFinished(userName, balanceItem.currency);
                        kongAmount = Math.Round(kongAmount, 6);

                        Dictionary<string, object> item = new Dictionary<string, object>();
                        item.Add("currency", balanceItem.currency);
                        item.Add("buyQuantity", totalQuantity);
                        item.Add("balance", Math.Round(balanceItem.balance, 6));
                        item.Add("nowPrice", nowPriceItem.NowPrice);
                        item.Add("kongAmount", kongAmount);
                        if (kongAmount > 0)
                        {
                            item.Add("canEmptyQuantity", Math.Round((balanceItem.balance - totalQuantity), 6) + $"({kongAmount})");
                        }
                        else
                        {
                            item.Add("canEmptyQuantity", Math.Round((balanceItem.balance - totalQuantity), 6));
                        }
                        item.Add("canEmptyAmount", Math.Round((balanceItem.balance - totalQuantity - kongAmount) * nowPriceItem.NowPrice, 6));

                        result.Add(item);

                        if (stat)
                        {
                            new DogStatSymbolDao().CreateDogStatSymbol(new DogStatSymbol
                            {
                                Amount = balanceItem.balance,
                                CreateTime = DateTime.Now,
                                EarnAmount = (decimal)Math.Round((balanceItem.balance - totalQuantity - kongAmount), 6),
                                StatDate = DateTime.Now.ToString("yyyy-MM-dd"),
                                SymbolName = balanceItem.currency,
                                UserName = userName
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex.Message, ex);
                        logger.Info(JsonConvert.SerializeObject(balanceItem));
                    }
                }

                if (sort == "canEmptyAmountasc")
                {
                    result.Sort((a, b) => decimal.Compare((decimal)a["canEmptyAmount"], (decimal)b["canEmptyAmount"]));
                }
                if (sort == "canEmptyAmountdesc")
                {
                    result.Sort((a, b) => decimal.Compare((decimal)b["canEmptyAmount"], (decimal)a["canEmptyAmount"]));
                }
                else if (sort == "currencydesc")
                {
                    result.Sort((a, b) => string.Compare(b["currency"].ToString(), a["currency"].ToString()));
                }
                else if (sort == "currencyasc")
                {
                    result.Sort((a, b) => string.Compare(a["currency"].ToString(), b["currency"].ToString()));
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                throw ex;
            }
        }

        private async Task<Dictionary<string, decimal>> GetUsdtPrice()
        {
            // 获取btc价格
            var btcPrice = new decimal(1000);
            PlatformApi api = PlatformApi.GetInstance("xx"); // 下面api和角色无关. 随便指定一个xx
            var period = "1min";
            var count = 3;
            var klines = api.GetHistoryKline("btcusdt", period, count);
            btcPrice = klines[0].Close;

            Dictionary<string, decimal> closeDic = new Dictionary<string, decimal>();
            closeDic.Add("usdt", (decimal)1);

            {
                var nowPriceList = new DogNowPriceDao().ListDogNowPrice("usdt");
                foreach (var item in nowPriceList)
                {
                    if (item.QuoteCurrency != "usdt")
                    {
                        continue;
                    }

                    closeDic.Add(item.SymbolName, item.NowPrice);
                }
            }

            {
                var nowPriceList = new DogNowPriceDao().ListDogNowPrice("btc");
                foreach (var item in nowPriceList)
                {
                    if (item.QuoteCurrency != "btc")
                    {
                        continue;
                    }

                    if (closeDic.ContainsKey(item.SymbolName))
                    {
                        continue;
                    }

                    closeDic.Add(item.SymbolName, item.NowPrice * btcPrice);
                }
            }


            {
                var nowPriceList = new DogNowPriceDao().ListDogNowPrice("eth");
                foreach (var item in nowPriceList)
                {
                    if (item.QuoteCurrency != "eth")
                    {
                        continue;
                    }

                    if (closeDic.ContainsKey(item.SymbolName))
                    {
                        continue;
                    }

                    closeDic.Add(item.SymbolName, item.NowPrice * closeDic["eth"]);
                }
            }


            {
                var nowPriceList = new DogNowPriceDao().ListDogNowPrice("ht");
                foreach (var item in nowPriceList)
                {
                    if (item.QuoteCurrency != "ht")
                    {
                        continue;
                    }

                    if (closeDic.ContainsKey(item.SymbolName))
                    {
                        continue;
                    }

                    closeDic.Add(item.SymbolName, item.NowPrice * closeDic["ht"]);
                }
            }

            return closeDic;
        }

        [HttpGet]
        [ActionName("deleteData")]
        public async Task DeleteData(string quoteCurrency)
        {
            var commonSymbols = CoinUtils.GetAllCommonSymbols(quoteCurrency);
            foreach (var commonSymbol in commonSymbols)
            {
                try
                {
                    await new DogControlDao().DeleteData(commonSymbol.BaseCurrency, commonSymbol.QuoteCurrency);
                }
                catch (Exception ex)
                {

                }
            }
        }

        static int aaa = 0;

        [HttpGet]
        [ActionName("listDogStatCurrency")]
        public async Task<object> ListDogStatCurrency(string userName, int intervalDay = 1)
        {
            aaa++;

            var dateList = new List<string>();

            if (intervalDay < 1)
            {
                intervalDay = 1;
            }

            for (int i = 0; i <= 15 * intervalDay; i = i + intervalDay)
            {
                var date = DateTime.Now.AddDays(0 - i).ToString("yyyy-MM-dd");
                dateList.Add(date);
            }

            var result = new DogStatSymbolDao().ListDogStatSymbol(userName, dateList);
            var symbolList = result.Select(it => it.SymbolName).Distinct().ToList();
            symbolList.Sort((a, b) =>
            {
                if (a == "usdt" || a == "btc" || a == "eth" || a == "ht" || a == "hpt")
                {
                    return -1;
                }
                if (b == "usdt" || b == "btc" || b == "eth" || b == "ht" || b == "hpt")
                {
                    return 1;
                }

                return string.Compare(a, b);
            });

            List<Dictionary<string, string>> data = new List<Dictionary<string, string>>();
            foreach (var symbol in symbolList)
            {
                Dictionary<string, string> item = new Dictionary<string, string>();
                item.Add("symbolName", symbol);
                for (int i = 0; i <= 30; i++)
                {
                    var date = DateTime.Now.AddDays(0 - i).ToString("yyyy-MM-dd");
                    item.Add(date, result.Find(it => it.SymbolName == symbol && it.StatDate == date)?.EarnAmount.ToString() ?? "");
                }
                data.Add(item);
            }

            var closeDic = await GetUsdtPrice();

            if (aaa % 2 == 0)
            {
                try
                {

                    var date = DateTime.Now.ToString("yyyy-MM-dd");
                    data.Sort((a, b) =>
                    {
                        if (a["symbolName"] == "usdt" || a["symbolName"] == "btc" || a["symbolName"] == "eth" || a["symbolName"] == "ht" || a["symbolName"] == "hpt")
                        {
                            return -1;
                        }
                        if (b["symbolName"] == "usdt" || b["symbolName"] == "btc" || b["symbolName"] == "eth" || b["symbolName"] == "ht" || a["symbolName"] == "hpt")
                        {
                            return 1;
                        }

                        if (!closeDic.ContainsKey(a["symbolName"]) || !closeDic.ContainsKey(a["symbolName"]))
                        {
                            return 0;
                        }

                        try
                        {
                            if (decimal.Parse(a[date]) * closeDic[a["symbolName"]] > decimal.Parse(b[date]) * closeDic[b["symbolName"]])
                            {
                                return 1;
                            }
                            return -1;
                        }
                        catch (Exception ex)
                        {
                            logger.Error("-----> " + ex.Message, ex);
                            Console.WriteLine(closeDic[a["symbolName"]]);
                            Console.WriteLine(closeDic[b["symbolName"]]);
                            Console.WriteLine(a[date]);
                            Console.WriteLine(b[date]);
                            return 0;
                        }
                    });
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message, ex);
                }
            }

            return new { data, dateList, closeDic };
        }

        public class NewSymbolForm
        {
            public List<HistoryKline> HistoryKlines { get; set; }
            public string BaseCurrency { get; set; }
            public string QuoteCurrency { get; set; }
        }

        [HttpPost]
        [ActionName("newSymbolData")]
        public async Task NewSymbolData([FromBody] NewSymbolForm form)
        {
            try
            {
                KlineUtils.InitMarketInDBFromOut(new CommonSymbol { BaseCurrency = form.BaseCurrency, QuoteCurrency = form.QuoteCurrency }, form.HistoryKlines);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                throw ex;
            }
        }
    }
}
