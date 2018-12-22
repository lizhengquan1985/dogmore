using DogAccount;
using DogPlatform;
using DogPlatform.Model;
using DogRunService;
using DogRunService.Helper;
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
                var pre50 = CoinsPre45.GetPreCoins();

                var notInPre50 = res.FindAll(it => pre50.IndexOf(it.SymbolName) < 0);
                var commonSymbols = CoinUtils.GetAllCommonSymbols(quoteCurrency);
                var notControlButRun = commonSymbols.FindAll(it => res.Find(item => item.SymbolName == it.BaseCurrency) == null).Select(it => it.BaseCurrency).ToList();

                var commonSymbols22 = CoinUtils.GetAllCommonSymbols22(quoteCurrency);
                pre50.RemoveAll(it => string.IsNullOrEmpty(it) || commonSymbols.Find(item => item.BaseCurrency == it) != null);
                pre50.RemoveAll(it => commonSymbols22.Find(item => item.BaseCurrency == it) == null);
                return new
                {
                    list = res,
                    closeDic,
                    noRunPre50 = pre50,
                    notInControl = commonSymbols.FindAll(it => res.Find(item => item.SymbolName == it.BaseCurrency) == null).Select(it => it.BaseCurrency).ToList(),
                    hasControlButNotInPre50 = notInPre50.Select(it => it.SymbolName).ToList(),
                    allItems = res.Select(it => it.SymbolName).ToList()
                };
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
                    await new DogControlDao().CreateDogControl(inDB);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                throw ex;
            }
        }

        private Dictionary<decimal, int> GetFlexpointCount(List<HistoryKline> historyKlines, out decimal outFlexPercent)
        {
            Dictionary<decimal, int> result = new Dictionary<decimal, int>();
            decimal flexPercent = (decimal)1.02;
            outFlexPercent = flexPercent;
            for (int i = 0; i < 30; i++)
            {
                var flexPointList = CoinAnalyze.Analyze(historyKlines, flexPercent);
                if (flexPointList.Count != 0)
                {
                    outFlexPercent = flexPercent;
                }
                result.Add(flexPercent, flexPointList.Count);

                flexPercent += (decimal)0.005;
            }
            return result;
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

        [HttpGet]
        [ActionName("listDogStatCurrency")]
        public async Task<object> ListDogStatCurrency(string userName, int intervalDay = 1)
        {
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
                if (a == "usdt" || a == "btc" || a == "eth" || a == "ht")
                {
                    return -1;
                }
                if (b == "usdt" || b == "btc" || b == "eth" || b == "ht")
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
            return new { data, dateList };
        }
    }
}
