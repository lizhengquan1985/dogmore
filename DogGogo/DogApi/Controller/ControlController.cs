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

                var notIn = res.FindAll(it => pre50.IndexOf(it.SymbolName) < 0);
                var commonSymbols = CoinUtils.GetAllCommonSymbols(quoteCurrency);
                pre50.RemoveAll(it => string.IsNullOrEmpty(it) || res.Find(item => item.SymbolName == it) != null);
                pre50.RemoveAll(it => commonSymbols.Find(item => item.BaseCurrency == it) == null);
                return new { list = res, closeDic, pre50, notIn = notIn.Select(it => it.SymbolName).ToList(), allItems = res.Select(it => it.SymbolName).ToList() };
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
                var klines = api.GetHistoryKline(symbolName + quoteCurrency, "1day", 1000);
                if(klines.Count < 180)
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
        public async Task<object> InitAccountInfo(string userName, string quoteCurrency, string sort)
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
                catch(Exception ex)
                {

                }
            }
        }
    }
}
