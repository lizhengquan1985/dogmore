using DogAccount;
using DogPlatform;
using DogPlatform.Model;
using DogRunService.Helper;
using DogService;
using DogService.DateTypes;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DogRunService
{
    public class CoinTrade
    {
        static ILog logger = LogManager.GetLogger(typeof(CoinTrade));

        public static void Run(CommonSymbols symbol)
        {
            try
            {
                // 计算是否适合购买
                RunBuy(symbol);
            }
            catch (Exception ex)
            {
                logger.Error("---> 购买异常: " + ex.Message, ex);
            }
            try
            {
                // 计算是否适合出售
                RunSell(symbol);
            }
            catch (Exception ex)
            {
                logger.Error("---> 出售异常: " + ex.Message, ex);
            }
        }

        private static void RunBuy(CommonSymbols symbol)
        {
            var key = HistoryKlinePools.GetKey(symbol, "1min");
            var historyKlineData = HistoryKlinePools.Get(key);
            if (historyKlineData == null || historyKlineData.Data == null || historyKlineData.Data.Count == 0 || historyKlineData.Date < DateTime.Now.AddMinutes(-1))// TODO
            {
                logger.Error($"RunBuy 数据还未准备好：{symbol.BaseCurrency}");
                Thread.Sleep(1000 * 5);
                return;
            }
            var historyKlines = historyKlineData.Data;

            // 获取最近行情
            decimal lastLowPrice;
            decimal nowPrice;
            // 分析是否下跌， 下跌超过一定数据，可以考虑
            decimal flexPercent = (decimal)1.04;
            var flexPointList = CoinAnalyze.Analyze(historyKlines, out lastLowPrice, out nowPrice, flexPercent);
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && flexPointList[0].isHigh))
            {
                flexPercent = (decimal)1.035;
                flexPointList = CoinAnalyze.Analyze(historyKlines, out lastLowPrice, out nowPrice, flexPercent);
            }
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && flexPointList[0].isHigh))
            {
                flexPercent = (decimal)1.03;
                flexPointList = CoinAnalyze.Analyze(historyKlines, out lastLowPrice, out nowPrice, flexPercent);
            }
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && flexPointList[0].isHigh))
            {
                flexPercent = (decimal)1.025;
                flexPointList = CoinAnalyze.Analyze(historyKlines, out lastLowPrice, out nowPrice, flexPercent);
            }
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && flexPointList[0].isHigh))
            {
                flexPercent = (decimal)1.02;
                flexPointList = CoinAnalyze.Analyze(historyKlines, out lastLowPrice, out nowPrice, flexPercent);
            }
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && flexPointList[0].isHigh))
            {
                flexPercent = (decimal)1.015;
                flexPointList = CoinAnalyze.Analyze(historyKlines, out lastLowPrice, out nowPrice, flexPercent);
            }
            if (flexPointList.Count == 0 && flexPointList.Count == 0)
            {
                logger.Error($"RunBuy--------------> 分析{symbol.BaseCurrency}的flexPoint结果数量为0 ");
                return;
            }

            if (flexPointList[0].isHigh || JudgeBuyUtils.IsQuickRise(symbol.BaseCurrency, historyKlines)
                )//|| JudgeBuyUtils.CheckCalcMaxhuoluo(historyKlines)
            {
                // 最高点 不适合购入
                // 快速升高 不适合购入
                // 大量回落 不适合购入
                return;
            }

            var userNames = UserPools.GetAllUserName();
            foreach (var userName in userNames)
            {
                AccountConfig accountConfig = AccountConfigUtils.GetAccountConfig(userName);
                var accountId = accountConfig.MainAccountId;

                //var noSellList = new PigMoreDao().ListPigMore(accountId, userName, symbol.BaseCurrency, new List<string> { StateConst.PartialFilled, StateConst.Submitted, StateConst.Submitting, StateConst.PreSubmitted });
                var canBuy = JudgeBuyUtils.CheckCanBuy(nowPrice, flexPointList[0].close);
                if (!canBuy)
                {
                    continue;
                }

                decimal minBuyPrice = new DogMoreBuyDao().GetMinPriceOfNotSell(accountId, userName, symbol.BaseCurrency);
                if (minBuyPrice <= 0)
                {
                    minBuyPrice = 999999;
                }
                //noSellList.ForEach(item =>
                //{
                //    if (item.BOrderP < minBuyPrice)
                //    {
                //        minBuyPrice = item.BOrderP;
                //    }
                //});

                if (nowPrice * (decimal)1.03 > minBuyPrice)
                {
                    // 最近一次购入,没有低于3%
                    continue;
                }

                PlatformApi api = PlatformApi.GetInstance(userName);
                var accountInfo = api.GetAccountBalance(accountId);
                var usdt = accountInfo.Data.list.Find(it => it.currency == "usdt");
                decimal recommendAmount = usdt.balance / 200; // TODO 测试阶段，暂定低一些，
                Console.WriteLine($"RunBuy--------> 开始 {symbol.BaseCurrency}  推荐额度：{decimal.Round(recommendAmount, 2)} ");

                if (recommendAmount < (decimal)0.3)
                {
                    continue;
                }

                // 获取最近的购买记录
                // 购买的要求
                // 1. 最近一次是低点， 并且有上升的迹象。
                // 2. 快速上升的，快速下降情况（如果升的太高， 最一定要回落，或者有5个小时平稳才考虑购入，）
                // 3. 如果flexpoint 小于等于1.02，则只能考虑买少一点。
                // 4. 余额要足够，推荐购买的额度要大于0.3
                // 5. 

                decimal buyQuantity = recommendAmount / nowPrice;

                buyQuantity = decimal.Round(buyQuantity, symbol.AmountPrecision);
                if (symbol.BaseCurrency == "xrp" && buyQuantity <= 1)
                {
                    buyQuantity = (decimal)1.01;
                }
                decimal orderPrice = decimal.Round(nowPrice * (decimal)1.005, symbol.PricePrecision);

                OrderPlaceRequest req = new OrderPlaceRequest();
                req.account_id = accountId;
                req.amount = buyQuantity.ToString();
                req.price = orderPrice.ToString();
                req.source = "api";
                req.symbol = symbol.BaseCurrency + symbol.QuoteCurrency;
                req.type = "buy-limit";
                if (BuyLimitUtils.Record(userName, symbol.BaseCurrency))
                {
                    logger.Error(" --------------------- 两个小时内购买次数太多，暂停一会 --------------------- ");
                    logger.Error(" --------------------- 两个小时内购买次数太多，暂停一会 --------------------- ");
                    logger.Error(" --------------------- 两个小时内购买次数太多，暂停一会 --------------------- ");
                    Thread.Sleep(1000 * 5);
                    return;
                }
                HBResponse<long> order = api.OrderPlace(req);
                if (order.Status == "ok")
                {
                    new DogMoreBuyDao().CreatePigMore(new PigMore()
                    {
                        Name = symbol.BaseCurrency,
                        AccountId = accountId,
                        UserName = accountConfig.UserName,
                        FlexPercent = flexPercent,

                        BQuantity = buyQuantity,
                        BOrderP = orderPrice,
                        BDate = DateTime.Now,
                        BOrderResult = JsonConvert.SerializeObject(order),
                        BState = StateConst.PreSubmitted,
                        BTradeP = 0,
                        BOrderId = order.Data,
                        BFlex = JsonConvert.SerializeObject(flexPointList),
                        BMemo = "",
                        BOrderDetail = "",
                        BOrderMatchResults = "",

                        SOrderId = 0,
                        SOrderResult = "",
                        SDate = DateTime.MinValue,
                        SFlex = "",
                        SMemo = "",
                        SOrderDetail = "",
                        SOrderMatchResults = "",
                        SOrderP = 0,
                        SQuantity = 0,
                        SState = "",
                        STradeP = 0,
                    });
                    // 下单成功马上去查一次
                    QueryBuyDetailAndUpdate(userName, order.Data);
                }
                logger.Error($"下单购买结果 {JsonConvert.SerializeObject(req)}, order：{JsonConvert.SerializeObject(order)}, 上一次最低购入价位：{minBuyPrice},nowPrice：{nowPrice}, accountId：{accountId}");
                logger.Error($"下单购买结果 分析 {JsonConvert.SerializeObject(flexPointList)}");
            }
        }

        private static void QueryBuyDetailAndUpdate(string userName, long orderId)
        {
            PlatformApi api = PlatformApi.GetInstance(userName);

            var orderDetail = api.QueryOrderDetail(orderId);
            if (orderDetail.Status == "ok" && orderDetail.Data.state == "filled")
            {
                var matchResult = api.QueryOrderMatchResult(orderId);
                decimal maxPrice = 0;
                foreach (var item in matchResult.Data)
                {
                    if (maxPrice < item.price)
                    {
                        maxPrice = item.price;
                    }
                }
                if (matchResult.Status == "ok")
                {
                    new DogMoreBuyDao().UpdatePigMoreBuySuccess(orderId, orderDetail, matchResult, maxPrice);
                }
            }
        }

        private static void RunSell(CommonSymbols symbol)
        {
            var key = HistoryKlinePools.GetKey(symbol, "1min");
            var historyKlineData = HistoryKlinePools.Get(key);
            if (historyKlineData == null || historyKlineData.Data == null || historyKlineData.Data.Count == 0
                || historyKlineData.Date < DateTime.Now.AddMinutes(-1))// TODO
            {
                logger.Error($"RunSell 数据还未准备好：{symbol.BaseCurrency}");
                Thread.Sleep(1000 * 5);
                return;
            }

            var historyKlines = historyKlineData.Data;

            // 获取最近行情
            decimal lastLowPrice;
            decimal nowPrice;
            // 分析是否下跌， 下跌超过一定数据，可以考虑
            decimal flexPercent = (decimal)1.04;
            var flexPointList = CoinAnalyze.Analyze(historyKlines, out lastLowPrice, out nowPrice, flexPercent);
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && !flexPointList[0].isHigh))
            {
                flexPercent = (decimal)1.035;
                flexPointList = CoinAnalyze.Analyze(historyKlines, out lastLowPrice, out nowPrice, flexPercent);
            }
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && !flexPointList[0].isHigh))
            {
                flexPercent = (decimal)1.03;
                flexPointList = CoinAnalyze.Analyze(historyKlines, out lastLowPrice, out nowPrice, flexPercent);
            }
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && !flexPointList[0].isHigh))
            {
                flexPercent = (decimal)1.025;
                flexPointList = CoinAnalyze.Analyze(historyKlines, out lastLowPrice, out nowPrice, flexPercent);
            }
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && !flexPointList[0].isHigh))
            {
                flexPercent = (decimal)1.02;
                flexPointList = CoinAnalyze.Analyze(historyKlines, out lastLowPrice, out nowPrice, flexPercent);
            }
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && !flexPointList[0].isHigh))
            {
                flexPercent = (decimal)1.015;
                flexPointList = CoinAnalyze.Analyze(historyKlines, out lastLowPrice, out nowPrice, flexPercent);
            }
            if (flexPointList.Count == 0 && flexPointList.Count == 0)
            {
                logger.Error($"--------------> 分析{symbol.BaseCurrency}的flexPoint结果数量为0 ");
                return;
            }

            if (!flexPointList[0].isHigh)
            {
                // 最低点 不适合出售
                logger.Error($"最低点 不适合出售 {symbol.BaseCurrency}-{flexPercent},lastLowPrice:{lastLowPrice}, nowPrice:{nowPrice}, flexPointList:{JsonConvert.SerializeObject(flexPointList)}");
                return;
            }

            var userNames = UserPools.GetAllUserName();
            foreach (var userName in userNames)
            {
                var accountConfig = AccountConfigUtils.GetAccountConfig(userName);
                var accountId = accountConfig.MainAccountId;
                var needSellPigMoreList = new DogMoreBuyDao().GetNeedSellPigMore(accountId, userName, symbol.BaseCurrency);

                foreach (var needSellPigMoreItem in needSellPigMoreList)
                {
                    // 分析是否 大于
                    decimal itemNowPrice = 0;
                    decimal higher = JudgeSellUtils.AnalyzeNeedSell(needSellPigMoreItem.BOrderP, needSellPigMoreItem.BDate, symbol.BaseCurrency, symbol.QuoteCurrency, out itemNowPrice, historyKlines);

                    decimal gaoyuPercentSell = (decimal)1.035;

                    bool needHuitou = true;// 如果很久没有出售过,则要考虑不需要回头
                    if (flexPercent < (decimal)1.04)
                    {
                        gaoyuPercentSell = (decimal)1.035;
                        if (flexPointList.Count <= 2 && needSellPigMoreList.Where(it => it.BDate > DateTime.Now.AddDays(-1)).ToList().Count == 0)
                        {
                            // 1天都没有交易. 并且波动比较小. 则不需要回头
                            needHuitou = false;
                        }
                    }

                    var canSell = JudgeSellUtils.CheckCanSell(needSellPigMoreItem.BOrderP, higher, itemNowPrice, gaoyuPercentSell, needHuitou);

                    //logger.Error($"是否能够出售:  {symbol.BaseCurrency},{canSell}, price:{needSellPigMoreItem.BOrderP}, nowPrice:{nowPrice},itemNowPrice:{itemNowPrice}, {userName}, {needSellPigMoreList.Count}, {accountId}");
                    if (canSell)
                    {
                        decimal sellQuantity = needSellPigMoreItem.BQuantity * (decimal)0.99;
                        sellQuantity = decimal.Round(sellQuantity, symbol.AmountPrecision);
                        if (symbol.BaseCurrency == "xrp" && sellQuantity < 1)
                        {
                            sellQuantity = 1;
                        }
                        // 出售
                        decimal sellPrice = decimal.Round(itemNowPrice * (decimal)0.985, symbol.PricePrecision);
                        OrderPlaceRequest req = new OrderPlaceRequest();
                        req.account_id = accountId;
                        req.amount = sellQuantity.ToString();
                        req.price = sellPrice.ToString();
                        req.source = "api";
                        req.symbol = symbol.BaseCurrency + symbol.QuoteCurrency; ;
                        req.type = "sell-limit";
                        PlatformApi api = PlatformApi.GetInstance(userName);
                        HBResponse<long> order = api.OrderPlace(req);
                        if (order.Status == "ok")
                        {
                            new DogMoreBuyDao().ChangeDataWhenSell(needSellPigMoreItem.Id, sellQuantity, sellPrice, JsonConvert.SerializeObject(order), JsonConvert.SerializeObject(flexPointList), order.Data);
                            // 下单成功马上去查一次
                            QuerySellDetailAndUpdate(userName, order.Data);
                        }

                        logger.Error($"下单出售结果 {JsonConvert.SerializeObject(req)}, order：{JsonConvert.SerializeObject(order)},itemNowPrice：{itemNowPrice} higher：{higher}，accountId：{accountId}");
                        logger.Error($"下单出售结果 分析 {JsonConvert.SerializeObject(flexPointList)}");
                    }
                }
            }
        }

        private static void QuerySellDetailAndUpdate(string userName, long orderId)
        {
            PlatformApi api = PlatformApi.GetInstance(userName);

            var orderDetail = api.QueryOrderDetail(orderId);
            if (orderDetail.Status == "ok" && orderDetail.Data.state == "filled")
            {
                var orderMatchResult = api.QueryOrderMatchResult(orderId);
                decimal minPrice = 99999999;
                foreach (var item in orderMatchResult.Data)
                {
                    if (minPrice > item.price)
                    {
                        minPrice = item.price;
                    }
                }
                // 完成
                new DogMoreBuyDao().UpdateTradeRecordSellSuccess(orderId, orderDetail, orderMatchResult, minPrice);
            }
        }

        public static void CheckBuyOrSellState()
        {
            try
            {
                var needChangeBuyStatePigMoreList = new DogMoreBuyDao().ListNeedChangeBuyStatePigMore();
                //Console.WriteLine($"未改变状态的交易记录2：{needChangeBuyStatePigMoreList.Count}");
                foreach (var item in needChangeBuyStatePigMoreList)
                {
                    // 如果长时间没有购买成功， 则取消订单。
                    if (item.BDate < DateTime.Now.AddMinutes(-30))
                    {
                        //api.
                    }
                    // TODO
                    QueryBuyDetailAndUpdate(item.UserName, item.BOrderId);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }

            try
            {
                var needChangeSellStatePigMoreList = new DogMoreBuyDao().ListNeedChangeSellStatePigMore();
                //Console.WriteLine($"未改变状态的交易记录1：{needChangeSellStatePigMoreList.Count}");
                foreach (var item in needChangeSellStatePigMoreList)
                {
                    // 如果长时间没有出售成功， 则取消订单。
                    // TODO
                    QuerySellDetailAndUpdate(item.UserName, item.SOrderId);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }
    }
}
