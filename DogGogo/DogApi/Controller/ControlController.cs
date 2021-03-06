﻿using DogAccount;
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

                //var outList = new List<string> { "mtl", "ncash", "phx", "sbtc", "adx", "mtl", "mtl", "mtl", "mtl", "mtl" };
                PlatformApi api = PlatformApi.GetInstance("xx");
                var tickers = api.GetTickers();

                var notInControl = new List<string>();
                foreach (var ticker in tickers)
                {
                    if (ticker.symbol.EndsWith(quoteCurrency))
                    {
                        var find = res.Find(it => ticker.symbol.StartsWith(it.SymbolName));
                        if (find == null)
                        {
                            notInControl.Add(ticker.symbol);
                        }
                    }
                }

                return new
                {
                    list = res.Select(it => new
                    {
                        it.HistoryMin,
                        it.HistoryMax,
                        it.EmptyPrice,
                        it.MaxInputPrice,
                        it.QuoteCurrency,
                        it.SymbolName,
                        AvgPrice = decimal.Round(it.AvgPrice, 9).ToString() + (it.MaxInputPrice > it.AvgPrice ? " 大于加权平均 " : "") + " -- " + decimal.Round(it.Min8, 9).ToString() + (it.MaxInputPrice > it.Min8 ? " 大于8阶层" : ""),
                        it.WillDelist,
                    }).ToList(),
                    closeDic,
                    notInControl,
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

                var avgPrice = (decimal)0;
                foreach (var item in klines)
                {
                    avgPrice += (item.Open + item.Close) / 2;
                }
                avgPrice = avgPrice / klines.Count;

                var dogControl = new DogControl()
                {
                    HistoryMax = max,
                    HistoryMin = min,
                    SymbolName = symbolName,
                    QuoteCurrency = quoteCurrency,
                    AvgPrice = avgPrice
                };

                new DogControlDao().UpdateDogControlMaxAndMin(dogControl);
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


        [HttpGet]
        [ActionName("resetDogStatCurrency")]
        public async Task ResetDogStatCurrency(string userName, string symbolName)
        {
            var dateList = new List<string>();

            for (int i = 0; i <= 15 * 1; i = i + 1)
            {
                var date = DateTime.Now.AddDays(0 - i).ToString("yyyy-MM-dd");
                dateList.Add(date);
            }

            var allStatSymbolList = new DogStatSymbolDao().ListDogStatSymbol(userName, dateList);
            var allSymbolList = allStatSymbolList.Select(it => it.SymbolName).Distinct().ToList();
            foreach (var sysbolNameItem in allSymbolList)
            {
                var result = allStatSymbolList.FindAll(it => it.SymbolName == sysbolNameItem);

                result.Sort((a, b) =>
                {
                    return string.Compare(b.StatDate, a.StatDate);
                });

                for (var i = result.Count - 1; i > 1; i--)
                {
                    Console.WriteLine($"{result[i].EarnAmount}, {result[i - 1].EarnAmount}");
                    if (result[i].EarnAmount > result[i - 1].EarnAmount)
                    {
                        result[i - 1].EarnAmount = result[i].EarnAmount;
                        new DogStatSymbolDao().UpdateDogStatSymbol(result[i - 1]);
                    }
                }
            }
        }

        /// <summary>
        /// 检查价格的合理性
        /// </summary>
        /// <param name="quote"></param>
        /// <returns></returns>
        [HttpGet]
        [ActionName("checkControl")]
        public async Task CheckControl(string quote)
        {
            // 检查btc，eth最大购买p，以及设置maxbuyprice之间的距离。
            var controls = await new DogControlDao().ListDogControl(quote);

            foreach (var control in controls)
            {
                // 获取最大的
                var maxBuyPriceOfNoSell = new DogMoreBuyDao().GetMaxPriceOfNotSellFinished(control.QuoteCurrency, control.SymbolName);
                if (maxBuyPriceOfNoSell > control.MaxInputPrice)
                {
                    logger.Error($"最大购买价格小于未出售的最大价格 maxBuyPriceOfNoSell:{maxBuyPriceOfNoSell} --  {JsonConvert.SerializeObject(control)}");
                }
                if (maxBuyPriceOfNoSell * (decimal)1.06 < control.MaxInputPrice)
                {
                    logger.Error($"最大购买价格过分大于未出售的最大价格 maxBuyPriceOfNoSell:{maxBuyPriceOfNoSell} --  {JsonConvert.SerializeObject(control)}");
                }
            }
        }
    }
}
