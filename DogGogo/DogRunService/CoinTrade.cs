using DogAccount;
using DogPlatform;
using DogPlatform.Model;
using DogRunService.DataTypes;
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
using System.Threading;
using System.Threading.Tasks;

namespace DogRunService
{
    public class AnalyzeResult
    {
        /// <summary>
        /// 分析后的拐点数据
        /// </summary>
        public List<FlexPoint> FlexPointList { get; set; }
        public decimal NowPrice { get; set; }
        public decimal LastLowPrice { get; set; }
        /// <summary>
        /// 原始数据
        /// </summary>
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
                Thread.Sleep(1000 * 3);
                return null;
            }
            var historyKlines = historyKlineData.Data;

            // 获取最近行情
            decimal lastLowPrice;
            decimal nowPrice = historyKlines[0].Close;
            // 分析是否下跌， 下跌超过一定数据，可以考虑
            decimal flexPercent = (decimal)1.050;
            var flexPointList = CoinAnalyze.Analyze(historyKlines, out lastLowPrice, flexPercent);
            if (flexPointList == null || flexPointList.Count == 0 || (flexPointList.Count == 1 && ((isBuy && flexPointList[0].isHigh) || (!isBuy && !flexPointList[0].isHigh))))
            {
                flexPercent = (decimal)1.045;
                flexPointList = CoinAnalyze.Analyze(historyKlines, out lastLowPrice, flexPercent);
            }
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
                //Thread.Sleep(1000 * 60 * 60);
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
                //Thread.Sleep(1000 * 60 * 60);
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
                || !JudgeBuyUtils.ControlCanBuy(symbol.BaseCurrency, nowPrice)
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

                decimal minBuyPrice = new DogMoreBuyDao().GetMinPriceOfNotSellFinished(accountId, userName, symbol.BaseCurrency);
                if (minBuyPrice <= 0)
                {
                    minBuyPrice = 25000;
                }

                var ladderBuyPercent = DogControlUtils.GetLadderBuy(symbol.BaseCurrency, nowPrice);
                if (nowPrice * ladderBuyPercent > minBuyPrice || nowPrice * (decimal)1.01 >= minBuyPrice)
                {
                    continue;
                }

                PlatformApi api = PlatformApi.GetInstance(userName);
                var accountInfo = api.GetAccountBalance(accountId);
                var usdt = accountInfo.Data.list.Find(it => it.currency == "usdt");
                // 要减去空单未收割得额度总和
                var notShougeEmptySellAmount = new DogEmptySellDao().GetSumNotShougeDogEmptySell(userName);
                if (notShougeEmptySellAmount >= usdt.balance)
                {
                    continue;
                }
                decimal recommendAmount = (usdt.balance - notShougeEmptySellAmount) / DogControlUtils.GetRecommendDivide(symbol.BaseCurrency, nowPrice); // TODO 测试阶段，暂定低一些，

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
                    try
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
                    catch (Exception ex)
                    {
                        Console.WriteLine("----RunBuy------危险-----------");
                        logger.Error(ex.Message, ex);
                        Thread.Sleep(1000 * 60 * 60);
                    }
                }
                logger.Error($"下单购买结果 {JsonConvert.SerializeObject(req)}, notShougeEmptySellAmount:{notShougeEmptySellAmount}, order：{JsonConvert.SerializeObject(order)}, 上一次最低购入价位：{minBuyPrice},nowPrice：{nowPrice}, accountId：{accountId}");
                logger.Error($"下单购买结果 分析 {JsonConvert.SerializeObject(flexPointList)}");
            }

            // 空单的自动波动收割
            foreach (var userName in userNames)
            {
                AccountConfig accountConfig = AccountConfigUtils.GetAccountConfig(userName);
                var accountId = accountConfig.MainAccountId;

                var needBuyDogEmptySellList = new DogEmptySellDao().GetNeedBuyDogEmptySell(accountId, userName, symbol.BaseCurrency);
                Console.WriteLine(symbol.BaseCurrency + $", nowPrice:{nowPrice} 空单数量：" + needBuyDogEmptySellList.Count + "--");
                foreach (var item in needBuyDogEmptySellList)
                {
                    Console.WriteLine(item.SellTradePrice);
                }

                var canBuy = JudgeBuyUtils.CheckCanBuy(nowPrice, flexPointList[0].close);
                if (!canBuy)
                {
                    continue;
                }

                var ladderBuyPercent = DogControlUtils.GetLadderBuy(symbol.BaseCurrency, nowPrice);
                PlatformApi api = PlatformApi.GetInstance(userName);

                foreach (var needBuyDogEmptySellItem in needBuyDogEmptySellList)
                {
                    ShouGeEmpty(needBuyDogEmptySellItem, symbol, analyzeResult, ladderBuyPercent);
                }
            }

            try
            {
                // 空单的指令收割
                var list = new OrderReapDao().List(ReapType.ShougeEmpty);
                foreach (var item in list)
                {
                    var percent = item.Percent;
                    var dogEmptySell = new DogEmptySellDao().GetDogEmptySellBySellOrderId(item.OrderId);
                    if (dogEmptySell.IsFinished || dogEmptySell.SymbolName != symbol.BaseCurrency)
                    {
                        continue;
                    }

                    if (dogEmptySell.SellTradePrice < nowPrice * percent)
                    {
                        continue;
                    }

                    var dogEmptyBuyList = new DogEmptyBuyDao().ListDogEmptyBuyBySellOrderId(dogEmptySell.SellOrderId);
                    if (dogEmptyBuyList.Count > 0 &&
                        dogEmptyBuyList.Find(it =>
                            it.BuyState != StateConst.Canceled.ToString()
                            && it.BuyState != StateConst.PartialFilled.ToString()
                            && it.BuyState != StateConst.Filled.ToString()) != null)
                    {
                        // 存在操作中的,则不操作
                        continue;
                    }

                    ShouGeEmpty(dogEmptySell, symbol, analyzeResult, percent);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }

        public static void ShouGeEmpty(DogEmptySell dogEmptySell, CommonSymbols symbol, AnalyzeResult analyzeResult, decimal percent = (decimal)1.02)
        {
            var nowPrice = analyzeResult.NowPrice;
            if (nowPrice * percent > dogEmptySell.SellTradePrice)
            {
                logger.Error($"{dogEmptySell.SymbolName}, --> nowPrice:{nowPrice}, tradePrice:{dogEmptySell.SellTradePrice} 收割不了");
                return;
            }

            decimal buyQuantity = CommonHelper.CalcBuyQuantityForEmptyShouge(dogEmptySell.SellQuantity, dogEmptySell.SellTradePrice, nowPrice, symbol.AmountPrecision);
            decimal orderPrice = decimal.Round(nowPrice * (decimal)1.005, symbol.PricePrecision);

            OrderPlaceRequest req = new OrderPlaceRequest();
            req.account_id = dogEmptySell.AccountId;
            req.amount = buyQuantity.ToString();
            req.price = orderPrice.ToString();
            req.source = "api";
            req.symbol = symbol.BaseCurrency + symbol.QuoteCurrency;
            req.type = "buy-limit";

            if (BuyLimitUtils.Record(dogEmptySell.UserName, symbol.BaseCurrency))
            {
                logger.Error(" --------------------- 两个小时内购买次数太多，暂停一会 --------------------- ");
                logger.Error(" --------------------- 两个小时内购买次数太多，暂停一会 --------------------- ");
                logger.Error(" --------------------- 两个小时内购买次数太多，暂停一会 --------------------- ");
                Thread.Sleep(1000 * 5);
                return;
            }

            PlatformApi api = PlatformApi.GetInstance(dogEmptySell.UserName);
            HBResponse<long> order = api.OrderPlace(req);
            if (order.Status == "ok")
            {
                new DogEmptyBuyDao().CreateDogEmptyBuy(new DogEmptyBuy()
                {
                    SymbolName = symbol.BaseCurrency,
                    AccountId = dogEmptySell.AccountId,
                    UserName = dogEmptySell.UserName,
                    SellOrderId = dogEmptySell.SellOrderId,

                    BuyQuantity = buyQuantity,
                    BuyOrderPrice = orderPrice,
                    BuyDate = DateTime.Now,
                    BuyOrderResult = JsonConvert.SerializeObject(order),
                    BuyState = StateConst.PreSubmitted,
                    BuyTradePrice = 0,
                    BuyOrderId = order.Data,
                    BuyFlex = JsonConvert.SerializeObject(analyzeResult.FlexPointList),
                    BuyMemo = "",
                    BuyOrderDetail = "",
                    BuyOrderMatchResults = "",
                });
                // 下单成功马上去查一次
                QueryEmptyBuyDetailAndUpdate(dogEmptySell.UserName, order.Data);
            }
            logger.Error($"空单收割-下单购买结果 {JsonConvert.SerializeObject(req)}, order：{JsonConvert.SerializeObject(order)}, ,nowPrice：{nowPrice}, accountId：{dogEmptySell.AccountId}");
            logger.Error($"空单收割-下单购买结果 分析 {JsonConvert.SerializeObject(analyzeResult.FlexPointList)}");
        }

        public static void ShouGeMore(DogMoreBuy dogMoreBuy, decimal percent = (decimal)1.02)
        {
            var symbols = CoinUtils.GetAllCommonSymbols();
            CommonSymbols symbol = symbols.Find(it => it.BaseCurrency == dogMoreBuy.SymbolName);

            AnalyzeResult analyzeResult = AnalyzeResult.GetAnalyzeResult(symbol, true);
            var nowPrice = analyzeResult.NowPrice;
            if (nowPrice < dogMoreBuy.BuyTradePrice * percent)
            {
                logger.Error($"{dogMoreBuy.SymbolName}, --> nowPrice:{nowPrice}, tradePrice:{dogMoreBuy.BuyTradePrice} 收割多不了");
                return;
            }

            decimal sellQuantity = JudgeSellUtils.CalcSellQuantityForMoreShouge(dogMoreBuy.BuyQuantity, dogMoreBuy.BuyTradePrice, nowPrice, symbol);

            // 出售
            decimal sellPrice = decimal.Round(nowPrice * (decimal)0.985, symbol.PricePrecision);
            OrderPlaceRequest req = new OrderPlaceRequest();
            req.account_id = dogMoreBuy.AccountId;
            req.amount = sellQuantity.ToString();
            req.price = sellPrice.ToString();
            req.source = "api";
            req.symbol = symbol.BaseCurrency + symbol.QuoteCurrency; ;
            req.type = "sell-limit";
            PlatformApi api = PlatformApi.GetInstance(dogMoreBuy.UserName);
            HBResponse<long> order = api.OrderPlace(req);
            logger.Error("收割-下单出售结果：" + JsonConvert.SerializeObject(order));
            if (order.Status == "ok")
            {
                try
                {
                    DogMoreSell dogMoreSell = new DogMoreSell()
                    {
                        AccountId = dogMoreBuy.AccountId,
                        UserName = dogMoreBuy.UserName,
                        BuyOrderId = dogMoreBuy.BuyOrderId,
                        SellOrderId = order.Data,
                        SellOrderResult = JsonConvert.SerializeObject(order),
                        SellDate = DateTime.Now,
                        SellFlex = "",
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
                }
                catch (Exception ex)
                {
                    logger.Error("------RunSell----危险-----------");
                    logger.Error(ex.Message, ex);
                    Thread.Sleep(1000 * 60 * 60);
                }

                // 下单成功马上去查一次
                QuerySellDetailAndUpdate(dogMoreBuy.UserName, order.Data);
            }

            logger.Error($"下单出售结果 {JsonConvert.SerializeObject(req)}, order：{JsonConvert.SerializeObject(order)},nowPrice：{nowPrice}，accountId：{dogMoreBuy.AccountId}");
        }

        private static void QueryBuyDetailAndUpdate(string userName, long orderId)
        {
            PlatformApi api = PlatformApi.GetInstance(userName);

            var orderDetail = api.QueryOrderDetail(orderId);
            if (orderDetail.Status == "ok" && orderDetail.Data.state == "filled")
            {
                logger.Error(JsonConvert.SerializeObject(orderDetail));
                var orderMatchResult = api.QueryOrderMatchResult(orderId);
                logger.Error("------------> " + JsonConvert.SerializeObject(orderMatchResult));
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
                    new DogMoreBuyDao().UpdateDogMoreBuySuccess(orderId, orderDetail, orderMatchResult, maxPrice);
                }
            }

            if (orderDetail.Status == "ok" && orderDetail.Data.state == StateConst.Canceled)
            {
                // 完成
                new DogMoreBuyDao().UpdateDogMoreBuyWhenCancel(orderId);
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
                return;
            }

            // 多单的自动波动收割
            var userNames = UserPools.GetAllUserName();
            foreach (var userName in userNames)
            {
                var accountConfig = AccountConfigUtils.GetAccountConfig(userName);
                var accountId = accountConfig.MainAccountId;
                var needSellDogMoreBuyList = new DogMoreBuyDao().GetNeedSellDogMoreBuy(accountId, userName, symbol.BaseCurrency);

                foreach (var needSellDogMoreBuyItem in needSellDogMoreBuyList)
                {
                    // 分析是否 大于
                    decimal afterBuyHighClosePrice = JudgeSellUtils.AnalyzeNeedSell(needSellDogMoreBuyItem.BuyTradePrice, needSellDogMoreBuyItem.BuyDate, historyKlines);

                    decimal gaoyuPercentSell = DogControlUtils.GetLadderSell(needSellDogMoreBuyItem.SymbolName); //(decimal)1.035;

                    bool needHuitou = true;// 如果很久没有出售过,则要考虑不需要回头
                    if (flexPercent < (decimal)1.04)
                    {
                        gaoyuPercentSell -= (decimal)0.005;
                        if (flexPointList.Count <= 2 && needSellDogMoreBuyList.Where(it => it.BuyDate > DateTime.Now.AddDays(-1)).ToList().Count == 0)
                        {
                            // 1天都没有交易. 并且波动比较小. 则不需要回头
                            needHuitou = false;
                        }
                    }

                    var canSell = JudgeSellUtils.CheckCanSell(needSellDogMoreBuyItem.BuyTradePrice, afterBuyHighClosePrice, nowPrice, gaoyuPercentSell, needHuitou);
                    if (!canSell)
                    {
                        continue;
                    }
                    decimal sellQuantity = JudgeSellUtils.CalcSellQuantityForMoreShouge(needSellDogMoreBuyItem.BuyQuantity, needSellDogMoreBuyItem.BuyTradePrice, nowPrice, symbol);

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
                    HBResponse<long> orderResult = api.OrderPlace(req);
                    logger.Error($"auto 下单出售结果 req:{JsonConvert.SerializeObject(req)}, orderResult::{JsonConvert.SerializeObject(orderResult)},nowPrice：{nowPrice} higher：{afterBuyHighClosePrice}，accountId：{accountId}");
                    if (orderResult.Status == "ok")
                    {
                        try
                        {
                            DogMoreSell dogMoreSell = new DogMoreSell()
                            {
                                AccountId = accountId,
                                UserName = userName,
                                BuyOrderId = needSellDogMoreBuyItem.BuyOrderId,
                                SellOrderId = orderResult.Data,
                                SellOrderResult = JsonConvert.SerializeObject(orderResult),
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
                        }
                        catch (Exception ex)
                        {
                            logger.Error("------RunSell----危险-----------");
                            logger.Error(ex.Message, ex);
                            Thread.Sleep(1000 * 60 * 60);
                        }

                        // 下单成功马上去查一次
                        QuerySellDetailAndUpdate(userName, orderResult.Data);
                    }
                }
            }

            try
            {
                // 多单的指令收割
                var list = new OrderReapDao().List(ReapType.ShougeMore);
                foreach (var item in list)
                {
                    var percent = item.Percent;
                    var dogMoreBuy = new DogMoreBuyDao().GetByBuyOrderId(item.OrderId);
                    if (dogMoreBuy.IsFinished || dogMoreBuy.SymbolName != symbol.BaseCurrency)
                    {
                        continue;
                    }

                    if (dogMoreBuy.BuyTradePrice * percent > nowPrice)
                    {
                        continue;
                    }

                    var dogMoreSellList = new DogMoreSellDao().ListDogMoreSellByBuyOrderId(dogMoreBuy.BuyOrderId);
                    if (dogMoreSellList.Count > 0 &&
                        dogMoreSellList.Find(it =>
                            it.SellState != StateConst.Canceled.ToString()
                            && it.SellState != StateConst.PartialFilled.ToString()
                            && it.SellState != StateConst.Filled.ToString()) != null)
                    {
                        // 存在操作中的,则不操作
                        continue;
                    }

                    ShouGeMore(dogMoreBuy, percent);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }

            // 自动做空
            // 要求  1. 进入拐点区域, 2. 受管控未过期
            var control = new DogControlDao().GetDogControl(symbol.BaseCurrency);
            if (nowPrice * (decimal)1.02 > flexPointList[0].close && nowPrice * (decimal)1.005 < flexPointList[0].close
                && control != null && nowPrice >= control.EmptyPrice && control.EmptyExpiredTime > DateTime.Now
                && nowPrice >= control.HistoryMin * (decimal)1.2 && control.HistoryMin > 0 && nowPrice >= (control.HistoryMax - control.HistoryMin) * (decimal)0.2 + control.HistoryMin)
            {
                foreach (var userName in userNames)
                {
                    try
                    {
                        // 和上次做空价格要相差8%
                        var maxSellTradePrice = new DogEmptySellDao().GetMaxSellTradePrice(userName, symbol.BaseCurrency);
                        if (maxSellTradePrice != null && nowPrice < maxSellTradePrice * (decimal)1.08)
                        {
                            // 上一次还没收割得，相差8%， 要等等
                            continue;
                        }

                        var accountConfig = AccountConfigUtils.GetAccountConfig(userName);
                        var accountId = accountConfig.MainAccountId;
                        PlatformApi api = PlatformApi.GetInstance(userName);

                        var accountInfo = api.GetAccountBalance(AccountConfigUtils.GetAccountConfig(userName).MainAccountId);
                        var balanceItem = accountInfo.Data.list.Find(it => it.currency == symbol.BaseCurrency);
                        // 要减去未收割得。
                        var notShougeQuantity = new DogMoreBuyDao().GetBuyQuantityNotShouge(userName, symbol.BaseCurrency);
                        if (notShougeQuantity >= balanceItem.balance || notShougeQuantity <= 0)
                        {
                            logger.Error($"未收割得数量大于余额，有些不合理，  {symbol.BaseCurrency},, {userName},, {notShougeQuantity}, {balanceItem.balance}");
                            continue;
                        }

                        decimal sellQuantity = (balanceItem.balance - notShougeQuantity) / DogControlUtils.GetEmptySellDivide(symbol.BaseCurrency, nowPrice); ; // 暂定每次做空1/12
                        if (sellQuantity * nowPrice < 1)
                        {
                            if(balanceItem.balance * nowPrice > 6)
                            {
                                sellQuantity = 1 / nowPrice;
                            }
                            else
                            {
                                sellQuantity = (decimal)0.5 / nowPrice;
                            }
                        }
                        if (sellQuantity * nowPrice > 10)
                        {
                            sellQuantity = 10 / nowPrice;
                        }
                        sellQuantity = decimal.Round(sellQuantity, symbol.AmountPrecision);

                        // 出售
                        decimal sellPrice = decimal.Round(nowPrice * (decimal)0.985, symbol.PricePrecision);
                        EmtpyTrade(accountId, userName, symbol, sellQuantity, sellPrice, flexPointList);

                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex.Message, ex);
                    }
                }
            }
        }

        private static void EmtpyTrade(string accountId, string userName, CommonSymbols symbol, decimal sellQuantity, decimal sellPrice, List<FlexPoint> flexPointList)
        {
            try
            {
                PlatformApi api = PlatformApi.GetInstance(userName);
                OrderPlaceRequest req = new OrderPlaceRequest();
                req.account_id = accountId;
                req.amount = sellQuantity.ToString();
                req.price = sellPrice.ToString();
                req.source = "api";
                req.symbol = symbol.BaseCurrency + symbol.QuoteCurrency; ;
                req.type = "sell-limit";
                HBResponse<long> order = api.OrderPlace(req);
                logger.Error("下单出售结果：" + JsonConvert.SerializeObject(order));
                if (order.Status == "ok")
                {
                    try
                    {
                        DogEmptySell dogEmptySell = new DogEmptySell()
                        {
                            AccountId = accountId,
                            UserName = userName,
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
                            SellOrderMatchResults = "",
                            FlexPercent = (decimal)1.04,
                            IsFinished = false
                        };
                        new DogEmptySellDao().CreateDogEmptySell(dogEmptySell);
                    }
                    catch (Exception ex)
                    {
                        logger.Error("------RunSell----危险-----------");
                        logger.Error(ex.Message, ex);
                    }

                    // 下单成功马上去查一次
                    QueryEmptySellDetailAndUpdate(userName, order.Data);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }

        public static void DoEmpty(CommonSymbols symbol, string userName, string accountId)
        {
            AnalyzeResult analyzeResult = AnalyzeResult.GetAnalyzeResult(symbol, false);
            if (analyzeResult == null)
            {
                throw new ApplicationException("做空失败，分析出错");
            }
            var flexPointList = analyzeResult.FlexPointList;
            var nowPrice = analyzeResult.NowPrice;
            if (!flexPointList[0].isHigh)
            {
                // 最低点 不适合出售
                throw new ApplicationException("做空失败， 最近不是最高");
            }

            if (nowPrice * (decimal)1.03 < flexPointList[0].close)
            {
                throw new ApplicationException("已经降低了3%， 不要做空，谨慎起见");
            }

            var maxSellTradePrice = new DogEmptySellDao().GetMaxSellTradePrice(userName, symbol.BaseCurrency);
            if (maxSellTradePrice != null && nowPrice < maxSellTradePrice * (decimal)1.02)
            {
                throw new ApplicationException("有价格比这个更高得还没有收割。不能重新做空。");
            }

            PlatformApi api = PlatformApi.GetInstance(userName);

            var accountInfo = api.GetAccountBalance(AccountConfigUtils.GetAccountConfig(userName).MainAccountId);
            var balanceItem = accountInfo.Data.list.Find(it => it.currency == symbol.BaseCurrency);
            var amount = balanceItem.balance * nowPrice;
            var sellAmout = Math.Max(amount / 20, 5);
            sellAmout = Math.Min(sellAmout, 10);

            decimal sellQuantity = sellAmout / nowPrice; // 暂定每次做空5美元
            sellQuantity = decimal.Round(sellQuantity, symbol.AmountPrecision);
            if (symbol.BaseCurrency == "xrp" && sellQuantity < 1)
            {
                sellQuantity = 1;
            }

            // 出售
            decimal sellPrice = decimal.Round(nowPrice * (decimal)0.985, symbol.PricePrecision);
            EmtpyTrade(accountId, userName, symbol, sellQuantity, sellPrice, flexPointList);
            //OrderPlaceRequest req = new OrderPlaceRequest();
            //req.account_id = accountId;
            //req.amount = sellQuantity.ToString();
            //req.price = sellPrice.ToString();
            //req.source = "api";
            //req.symbol = symbol.BaseCurrency + symbol.QuoteCurrency; ;
            //req.type = "sell-limit";
            //HBResponse<long> order = api.OrderPlace(req);
            //logger.Error("下单出售结果：" + JsonConvert.SerializeObject(order));
            //if (order.Status == "ok")
            //{
            //    try
            //    {
            //        DogEmptySell dogEmptySell = new DogEmptySell()
            //        {
            //            AccountId = accountId,
            //            UserName = userName,
            //            SellOrderId = order.Data,
            //            SellOrderResult = JsonConvert.SerializeObject(order),
            //            SellDate = DateTime.Now,
            //            SellFlex = JsonConvert.SerializeObject(flexPointList),
            //            SellQuantity = sellQuantity,
            //            SellOrderPrice = sellPrice,
            //            SellState = StateConst.Submitted,
            //            SellTradePrice = 0,
            //            SymbolName = symbol.BaseCurrency,
            //            SellMemo = "",
            //            SellOrderDetail = "",
            //            SellOrderMatchResults = "",
            //            FlexPercent = (decimal)1.04,
            //            IsFinished = false
            //        };
            //        new DogEmptySellDao().CreateDogEmptySell(dogEmptySell);
            //    }
            //    catch (Exception ex)
            //    {
            //        logger.Error("------RunSell----危险-----------");
            //        logger.Error(ex.Message, ex);
            //    }

            //    // 下单成功马上去查一次
            //    QueryEmptySellDetailAndUpdate(userName, order.Data);
            //}
        }

        private static void QuerySellDetailAndUpdate(string userName, long orderId)
        {
            PlatformApi api = PlatformApi.GetInstance(userName);

            var orderDetail = api.QueryOrderDetail(orderId);
            if (orderDetail.Status == "ok" && orderDetail.Data.state == "filled")
            {
                var orderMatchResult = api.QueryOrderMatchResult(orderId);
                decimal minPrice = 25000;
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
                    new DogMoreSellDao().UpdateDogMoreSellWhenSuccess(orderId, orderDetail, orderMatchResult, minPrice);
                }
            }

            if (orderDetail.Status == "ok" && orderDetail.Data.state == StateConst.Canceled)
            {
                // 完成
                new DogMoreSellDao().UpdateDogMoreSellWhenCancel(orderId);
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


            // 空
            try
            {
                var needChangeSellStateDogEmptySellList = new DogEmptySellDao().ListNeedChangeSellStateDogEmptySell();
                foreach (var item in needChangeSellStateDogEmptySellList)
                {
                    // 如果长时间没有出售成功， 则取消订单。
                    // TODO
                    QueryEmptySellDetailAndUpdate(item.UserName, item.SellOrderId);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }

            try
            {
                var needChangeBuyStateDogEmptyBuyList = new DogEmptyBuyDao().ListNeedChangeBuyStateDogEmptyBuy();
                foreach (var item in needChangeBuyStateDogEmptyBuyList)
                {
                    // 如果长时间没有出售成功， 则取消订单。
                    // TODO
                    QueryEmptyBuyDetailAndUpdate(item.UserName, item.BuyOrderId);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }

        private static void QueryEmptySellDetailAndUpdate(string userName, long orderId)
        {
            PlatformApi api = PlatformApi.GetInstance(userName);

            var orderDetail = api.QueryOrderDetail(orderId);
            if (orderDetail.Status == "ok" && orderDetail.Data.state == "filled")
            {
                var orderMatchResult = api.QueryOrderMatchResult(orderId);
                decimal minPrice = 25000;
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
                    new DogEmptySellDao().UpdateDogEmptySellWhenSuccess(orderId, orderDetail, orderMatchResult, minPrice);
                }
            }
        }

        private static void QueryEmptyBuyDetailAndUpdate(string userName, long orderId)
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
                    // 完成
                    new DogEmptyBuyDao().UpdateDogEmptyBuyWhenSuccess(orderId, orderDetail, orderMatchResult, maxPrice);
                }
            }
        }
    }
}
