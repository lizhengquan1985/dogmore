using DogAccount;
using DogPlatform;
using DogPlatform.Model;
using DogRunService.DataTypes;
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
    public class AnalyzeResult
    {
        public List<FlexPoint> FlexPointList { get; set; }
        public decimal NowPrice { get; set; }
        public decimal LastLowPrice { get; set; }
        public List<HistoryKline> HistoryKlines { get; set; }
        public decimal FlexPercent { get; set; }


        static ILog logger = LogManager.GetLogger(typeof(AnalyzeResult));

        public static AnalyzeResult GetAnalyzeResult(CommonSymbols symbol, bool isBuy)
        {
            var key = HistoryKlinePools.GetKey(symbol, "1min");
            var historyKlineData = HistoryKlinePools.Get(key);
            if (historyKlineData == null || historyKlineData.Data == null
                || historyKlineData.Data.Count == 0 || historyKlineData.Date < DateTime.Now.AddMinutes(-1)) // TODO
            {
                logger.Error($"GetAnalyzeResult 数据还未准备好：{symbol.BaseCurrency}");
                Thread.Sleep(1000 * 5);
                return null;
            }
            var historyKlines = historyKlineData.Data;

            // 获取最近行情
            decimal lastLowPrice;
            decimal nowPrice = historyKlines[0].Close;
            // 分析是否下跌， 下跌超过一定数据，可以考虑
            decimal flexPercent = (decimal)1.045;
            var flexPointList = CoinAnalyze.Analyze(historyKlines, out lastLowPrice, flexPercent);
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && ((isBuy && flexPointList[0].isHigh) || (!isBuy && !flexPointList[0].isHigh))))
            {
                flexPercent = (decimal)1.040;
                flexPointList = CoinAnalyze.Analyze(historyKlines, out lastLowPrice, flexPercent);
            }
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && ((isBuy && flexPointList[0].isHigh) || (!isBuy && !flexPointList[0].isHigh))))
            {
                flexPercent = (decimal)1.035;
                flexPointList = CoinAnalyze.Analyze(historyKlines, out lastLowPrice, flexPercent);
            }
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && ((isBuy && flexPointList[0].isHigh) || (!isBuy && !flexPointList[0].isHigh))))
            {
                flexPercent = (decimal)1.03;
                flexPointList = CoinAnalyze.Analyze(historyKlines, out lastLowPrice, flexPercent);
            }
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && ((isBuy && flexPointList[0].isHigh) || (!isBuy && !flexPointList[0].isHigh))))
            {
                flexPercent = (decimal)1.025;
                flexPointList = CoinAnalyze.Analyze(historyKlines, out lastLowPrice, flexPercent);
            }
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && ((isBuy && flexPointList[0].isHigh) || (!isBuy && !flexPointList[0].isHigh))))
            {
                flexPercent = (decimal)1.02;
                flexPointList = CoinAnalyze.Analyze(historyKlines, out lastLowPrice, flexPercent);
            }
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && ((isBuy && flexPointList[0].isHigh) || (!isBuy && !flexPointList[0].isHigh))))
            {
                flexPercent = (decimal)1.015;
                flexPointList = CoinAnalyze.Analyze(historyKlines, out lastLowPrice, flexPercent);
            }
            if (flexPointList.Count == 0 && flexPointList.Count == 0)
            {
                logger.Error($"--------------> 分析{symbol.BaseCurrency}的flexPoint结果数量为0 ");
                return null;
            }

            AnalyzeResult analyzeResult = new AnalyzeResult()
            {
                FlexPointList = flexPointList,
                LastLowPrice = lastLowPrice,
                NowPrice = nowPrice,
                HistoryKlines = historyKlines,
                FlexPercent = flexPercent
            };
            return analyzeResult;
        }
    }

    public class CoinTrade
    {
        static ILog logger = LogManager.GetLogger(typeof(CoinTrade));

        public static void Run(CommonSymbols symbol)
        {

            try
            {
                AnalyzeResult analyzeResult = AnalyzeResult.GetAnalyzeResult(symbol, true);
                if (analyzeResult != null)
                {
                    // 计算是否适合购买
                    RunBuy(symbol, analyzeResult);
                }
            }
            catch (Exception ex)
            {
                logger.Error("---> 购买异常: " + ex.Message, ex);
            }
            try
            {
                AnalyzeResult analyzeResult = AnalyzeResult.GetAnalyzeResult(symbol, false);
                if (analyzeResult != null)
                {
                    // 计算是否适合出售
                    RunSell(symbol, analyzeResult);
                }
            }
            catch (Exception ex)
            {
                logger.Error("---> 出售异常: " + ex.Message, ex);
            }
        }

        private static void RunBuy(CommonSymbols symbol, AnalyzeResult analyzeResult)
        {
            var flexPointList = analyzeResult.FlexPointList;
            var historyKlines = analyzeResult.HistoryKlines;
            var nowPrice = analyzeResult.NowPrice;
            var flexPercent = analyzeResult.FlexPercent;

            // 不适合购入的情况 最高点, 快速升高, 大量回落
            if (flexPointList[0].isHigh || JudgeBuyUtils.IsQuickRise(symbol.BaseCurrency, historyKlines)
                )//|| JudgeBuyUtils.CheckCalcMaxhuoluo(historyKlines)
            {
                return;
            }

            var userNames = UserPools.GetAllUserName();
            foreach (var userName in userNames)
            {
                AccountConfig accountConfig = AccountConfigUtils.GetAccountConfig(userName);
                var accountId = accountConfig.MainAccountId;

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

                if (nowPrice * (decimal)1.03 > minBuyPrice)
                {
                    // 最近一次购入,没有低于3%
                    continue;
                }

                PlatformApi api = PlatformApi.GetInstance(userName);
                var accountInfo = api.GetAccountBalance(accountId);
                var usdt = accountInfo.Data.list.Find(it => it.currency == "usdt");
                decimal recommendAmount = usdt.balance / 500; // TODO 测试阶段，暂定低一些，
                Console.WriteLine($"RunBuy--------> 开始 {symbol.BaseCurrency}  推荐额度：{decimal.Round(recommendAmount, 2)} ");

                if (recommendAmount < (decimal)1.1)
                {
                    // 余额要足够，推荐购买的额度要大于1.1
                    continue;
                }

                // 购买的要求
                // 2. 快速上升的，快速下降情况（如果升的太高， 最一定要回落，或者有5个小时平稳才考虑购入，）
                // 3. 如果flexpoint 小于等于1.02，则只能考虑买少一点。

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
                    new DogMoreBuyDao().CreateDogMoreBuy(new DogMoreBuy()
                    {
                        SymbolName = symbol.BaseCurrency,
                        AccountId = accountId,
                        UserName = accountConfig.UserName,
                        FlexPercent = flexPercent,

                        BuyQuantity = buyQuantity,
                        BuyOrderPrice = orderPrice,
                        BuyDate = DateTime.Now,
                        BuyOrderResult = JsonConvert.SerializeObject(order),
                        BuyState = StateConst.PreSubmitted,
                        BuyTradePrice = 0,
                        BuyOrderId = order.Data,
                        BuyFlex = JsonConvert.SerializeObject(flexPointList),
                        BuyMemo = "",
                        BuyOrderDetail = "",
                        BuyOrderMatchResults = "",
                        IsFinished = false
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
                var orderMatchResult = api.QueryOrderMatchResult(orderId);
                decimal maxPrice = 0;
                foreach (var item in orderMatchResult.Data)
                {
                    if (maxPrice < item.price)
                    {
                        maxPrice = item.price;
                    }
                }
                if (orderMatchResult.Status == "ok")
                {
                    new DogMoreBuyDao().UpdatePigMoreBuySuccess(orderId, orderDetail, orderMatchResult, maxPrice);
                }
            }
        }

        private static void RunSell(CommonSymbols symbol, AnalyzeResult analyzeResult)
        {
            var flexPointList = analyzeResult.FlexPointList;
            var historyKlines = analyzeResult.HistoryKlines;
            var nowPrice = analyzeResult.NowPrice;
            var lastLowPrice = analyzeResult.LastLowPrice;
            var flexPercent = analyzeResult.FlexPercent;

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
                var needSellDogMoreBuyList = new DogMoreBuyDao().GetNeedSellDogMoreBuy(accountId, userName, symbol.BaseCurrency);

                foreach (var needSellDogMoreBuyItem in needSellDogMoreBuyList)
                {
                    // 分析是否 大于
                    decimal afterBuyHighClosePrice = JudgeSellUtils.AnalyzeNeedSell(needSellDogMoreBuyItem.BuyOrderPrice, needSellDogMoreBuyItem.BuyDate, historyKlines);

                    decimal gaoyuPercentSell = (decimal)1.035;

                    bool needHuitou = true;// 如果很久没有出售过,则要考虑不需要回头
                    if (flexPercent < (decimal)1.04)
                    {
                        gaoyuPercentSell = (decimal)1.035;
                        if (flexPointList.Count <= 2 && needSellDogMoreBuyList.Where(it => it.BuyDate > DateTime.Now.AddDays(-1)).ToList().Count == 0)
                        {
                            // 1天都没有交易. 并且波动比较小. 则不需要回头
                            needHuitou = false;
                        }
                    }

                    var canSell = JudgeSellUtils.CheckCanSell(needSellDogMoreBuyItem.BuyOrderPrice, afterBuyHighClosePrice, nowPrice, gaoyuPercentSell, needHuitou);

                    if (canSell)
                    {
                        decimal sellQuantity = needSellDogMoreBuyItem.BuyQuantity * (decimal)0.99;
                        sellQuantity = decimal.Round(sellQuantity, symbol.AmountPrecision);
                        if (symbol.BaseCurrency == "xrp" && sellQuantity < 1)
                        {
                            sellQuantity = 1;
                        }
                        // 出售
                        decimal sellPrice = decimal.Round(nowPrice * (decimal)0.985, symbol.PricePrecision);
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
                            DogMoreSell dogMoreSell = new DogMoreSell()
                            {
                                AccountId = accountId,
                                UserName = userName,
                                BuyOrderId = needSellDogMoreBuyItem.BuyOrderId,
                                SellOrderId = order.Data,
                                SellOrderResult = JsonConvert.SerializeObject(order),
                                SellDate = DateTime.Now,
                                SellFlex = JsonConvert.SerializeObject(flexPointList),
                                SellQuantity = sellQuantity,
                                SellOrderPrice = sellPrice,
                                SellState = StateConst.Submitted,
                                SellTradePrice = 0,
                                SymbolName = symbol.BaseCurrency,
                                SellMemo = "",
                                SellOrderDetail = "",
                                SellOrderMatchResults = ""
                            };

                            new DogMoreSellDao().CreateDogMoreBuy(dogMoreSell);

                            // 下单成功马上去查一次
                            QuerySellDetailAndUpdate(userName, order.Data);
                        }

                        logger.Error($"下单出售结果 {JsonConvert.SerializeObject(req)}, order：{JsonConvert.SerializeObject(order)},nowPrice：{nowPrice} higher：{higher}，accountId：{accountId}");
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
                if (orderMatchResult.Status == "ok")
                {
                    // 完成
                    new DogMoreSellDao().UpdateTradeRecordSellSuccess(orderId, orderDetail, orderMatchResult, minPrice);
                }
            }
        }

        public static void CheckBuyOrSellState()
        {
            try
            {
                var needChangeBuyStateDogMoreBuyList = new DogMoreBuyDao().ListNeedChangeBuyStateDogMoreBuy();
                foreach (var item in needChangeBuyStateDogMoreBuyList)
                {
                    // 如果长时间没有购买成功， 则取消订单。
                    if (item.BuyDate < DateTime.Now.AddMinutes(-30))
                    {
                        //api.
                    }
                    // TODO
                    QueryBuyDetailAndUpdate(item.UserName, item.BuyOrderId);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }

            try
            {
                var needChangeSellStateDogMoreSellList = new DogMoreSellDao().ListNeedChangeSellStateDogMoreSell();
                foreach (var item in needChangeSellStateDogMoreSellList)
                {
                    // 如果长时间没有出售成功， 则取消订单。
                    // TODO
                    QuerySellDetailAndUpdate(item.UserName, item.SellOrderId);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }
    }
}
