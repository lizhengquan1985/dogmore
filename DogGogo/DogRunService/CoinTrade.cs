﻿using DogAccount;
using DogPlatform;
using DogPlatform.Model;
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
    public class CoinTrade
    {
        static DateTime nobtcbalanceTime = DateTime.MinValue;

        /// <summary>
        /// 做多间隔
        /// </summary>
        public static decimal ladderMoreBuyPercent = (decimal)1.078;
        /// <summary>
        /// 收割间隔
        /// </summary>
        public static decimal ladderMoreSellPercent = (decimal)1.095;
        /// <summary>
        /// 做空间隔
        /// </summary>
        private static decimal ladderEmptySellPercent = (decimal)1.088;
        /// <summary>
        /// 收割空单
        /// </summary>
        private static decimal ladderEmptyBuyPercent = (decimal)1.08;

        static ILog logger = LogManager.GetLogger(typeof(CoinTrade));
        public static int RunCount = 0;

        public static bool Run(int index, CommonSymbol symbol, List<Ticker> tickers)
        {
            // 先获取最近的数据， 看看是否靠近购入，卖出
            var minDogMoreBuy = new DogMoreBuyDao().GetSmallestDogMoreBuy(symbol.QuoteCurrency, symbol.BaseCurrency);
            var maxDogEmptySell = new DogEmptySellDao().GetBiggestDogEmptySell(symbol.QuoteCurrency, symbol.BaseCurrency);
            var findTicker = tickers.Find(it => it.symbol == symbol.BaseCurrency + symbol.QuoteCurrency);
            if (findTicker == null)
            {
                //logger.Error($"{symbol.QuoteCurrency}, {symbol.BaseCurrency}");
                return false;
            }

            if (findTicker.open <= 0 || findTicker.close <= 0 || findTicker.high <= 0 || findTicker.low <= 0)
            {
                logger.Error($"数据不对 : {JsonConvert.SerializeObject(findTicker)}");
                return false;
            }

            var control = new DogControlDao().GetDogControl(symbol.BaseCurrency, symbol.QuoteCurrency);
            if (control == null)
            {
                return false;
            }

            if (control.HistoryMin >= findTicker.close || control.HistoryMax <= findTicker.close)
            {
                // 初始化一下
                RefreshHistoryMaxMinAsync(control.SymbolName, control.QuoteCurrency);
            }

            new DogNowPriceDao().CreateDogNowPrice(new DogNowPrice
            {
                NowPrice = findTicker.close,
                NowTime = Utils.GetIdByDate(DateTime.Now),
                QuoteCurrency = symbol.QuoteCurrency,
                SymbolName = symbol.BaseCurrency,
                TodayMaxPrice = findTicker.high,
                TodayMinPrice = findTicker.low,
                NearMaxPrice = findTicker.high
            });

            var maySell = false;
            var mayBuy = false;
            if ((
                    control.EmptyPrice < findTicker.close && (
                    maxDogEmptySell == null ||
                    findTicker.close / maxDogEmptySell.SellOrderPrice > (decimal)1.082)
                )

                || (maxDogEmptySell != null &&
                maxDogEmptySell.SellOrderPrice / findTicker.close > (decimal)1.085))
            {
                maySell = true;
            }
            if (
                (control.MaxInputPrice > findTicker.close && (
                minDogMoreBuy == null
                || minDogMoreBuy.BuyOrderPrice / findTicker.close > (decimal)1.062))

                || (minDogMoreBuy != null && findTicker.close / minDogMoreBuy.BuyOrderPrice > (decimal)1.09))
            {
                mayBuy = true;
                if (symbol.QuoteCurrency == "btc" && nobtcbalanceTime > DateTime.Now.AddMinutes(-5) && (
                    minDogMoreBuy == null || minDogMoreBuy.BuyOrderPrice / findTicker.close > (decimal)1.06
                    ))
                {
                    mayBuy = false;
                }
            }

            if (symbol.BaseCurrency == "xmx")
            {
                Console.WriteLine($"{maySell}, {mayBuy}");
            }
            if (!mayBuy && !maySell)
            {
                return false;
            }

            AnalyzeResult analyzeResult = AnalyzeResult.GetAnalyzeResult(symbol);
            if (analyzeResult == null)
            {
                return false;
            }

            try
            {
                // 计算是否适合购买
                RunBuy(symbol, analyzeResult);
            }
            catch (Exception ex)
            {
                logger.Error($"---> 购买异常: {JsonConvert.SerializeObject(symbol)}" + ex.Message, ex);
            }

            try
            {
                // 计算是否适合出售
                RunSell(symbol, analyzeResult, findTicker);

                RunCount++;
            }
            catch (Exception ex)
            {
                logger.Error($"---> 出售异常: {JsonConvert.SerializeObject(symbol)}" + ex.Message, ex);
            }

            return true;
        }

        private static void RunBuy(CommonSymbol symbol, AnalyzeResult analyzeResult)
        {
            var nowPrice = analyzeResult.NowPrice;

            var userNames = UserPools.GetAllUserName();

            // 空单的自动波动收割
            foreach (var userName in userNames)
            {
                var dogEmptySellList = new DogEmptySellDao().GetNeedShougeDogEmptySell(userName, symbol.BaseCurrency, symbol.QuoteCurrency);
                if (dogEmptySellList == null || dogEmptySellList.Count == 0)
                {
                    continue;
                }

                Console.WriteLine("做空收割 " + symbol.BaseCurrency + symbol.QuoteCurrency + $", nowPrice:{nowPrice} 空单数量：" + dogEmptySellList.Count);
                foreach (var dogEmptySellItem in dogEmptySellList)
                {
                    try
                    {
                        ShouGeDogEmpty(dogEmptySellItem, symbol, analyzeResult);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }

            // 1.低于管控的购入价
            if (!JudgeBuyUtils.ControlCanBuy(symbol.BaseCurrency, symbol.QuoteCurrency, nowPrice))
            {
                return;
            }

            // 自动波动做多
            foreach (var userName in userNames)
            {
                try
                {
                    BuyWhenDoMore(symbol, AccountConfigUtils.GetAccountConfig(userName), analyzeResult);
                }
                catch (Exception ex)
                {
                }
            }
        }

        /// <summary>
        /// 购买,做多的时候
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="userName"></param>
        /// <param name="accountId"></param>
        public static void BuyWhenDoMore(CommonSymbol symbol, AccountConfig account, AnalyzeResult analyzeResult)
        {
            var accountId = account.MainAccountId;
            var userName = account.UserName;
            var nowPrice = analyzeResult.NowPrice;

            var dogMoreBuy = new DogMoreBuyDao().GetMinBuyPriceDataOfNotSellFinished(accountId, userName, symbol.QuoteCurrency, symbol.BaseCurrency);
            var ladderBuyWhenDoMore = ladderMoreBuyPercent;
            if (symbol.QuoteCurrency == "usdt")
            {
                ladderBuyWhenDoMore = (decimal)1.06;
            }
            if (dogMoreBuy != null && (nowPrice * ladderBuyWhenDoMore > Math.Min(dogMoreBuy.BuyTradePrice, dogMoreBuy.BuyOrderPrice)))
            {
                throw new ApplicationException("有价格比这个更低得还没有收割。不能重新做多。");
            }

            PlatformApi api = PlatformApi.GetInstance(userName);
            var accountInfo = api.GetAccountBalance(accountId);
            var quoteCurrency = accountInfo.Data.list.Find(it => it.currency == symbol.QuoteCurrency);
            // 要减去空单未收割得额度总和
            var notShougeEmptySellAmount = new DogEmptySellDao().GetSumNotShougeDogEmptySell(userName, symbol.QuoteCurrency);
            if (!CommonHelper.CheckBalanceForDoMore(symbol.QuoteCurrency, quoteCurrency.balance, notShougeEmptySellAmount))
            {
                Console.WriteLine($"{symbol.BaseCurrency}{symbol.QuoteCurrency}余额不足notShougeEmptySellAmount:{notShougeEmptySellAmount},balance:{quoteCurrency.balance}");
                if (symbol.QuoteCurrency == "btc" && account.MainAccountId == "529880")
                {
                    nobtcbalanceTime = DateTime.Now;
                }
                throw new ApplicationException($"余额不足notShougeEmptySellAmount:{notShougeEmptySellAmount},balance:{quoteCurrency.balance}");
            }

            decimal recommendAmount = DogControlUtils.GetRecommendBuyAmount(symbol);
            var maxBuyPrice = new DogMoreBuyDao().GetMaxBuyPrice(accountId, userName, symbol.QuoteCurrency, symbol.BaseCurrency);
            recommendAmount = DogControlUtils.GetMoreSize(recommendAmount, maxBuyPrice, nowPrice);

            // 购买的要求
            decimal buyQuantity = recommendAmount / nowPrice;
            buyQuantity = CoinUtils.CalcTradeQuantity(symbol, buyQuantity);

            // 判断是否满足最小购买数量
            if (!CoinUtils.IsBiggerThenLeastBuyForDoMore(symbol.BaseCurrency, symbol.QuoteCurrency, buyQuantity))
            {
                Console.WriteLine($"    {symbol.BaseCurrency}{symbol.QuoteCurrency},做多数量太少，不符合最小交易额度");
                return;
            }

            buyQuantity = decimal.Round(buyQuantity, symbol.AmountPrecision);
            decimal orderPrice = decimal.Round(nowPrice * (decimal)1.006, symbol.PricePrecision);

            OrderPlaceRequest req = new OrderPlaceRequest();
            req.account_id = accountId;
            req.amount = buyQuantity.ToString();
            req.price = orderPrice.ToString();
            req.source = "api";
            req.symbol = symbol.BaseCurrency + symbol.QuoteCurrency;
            req.type = "buy-limit";
            if (BuyLimitUtils.Record(userName, symbol.BaseCurrency + symbol.QuoteCurrency))
            {
                logger.Error(" --------------------- 两个小时内购买次数太多，暂停一会 --------------------- ");
                Thread.Sleep(1000 * 10);
                return;
            }

            HBResponse<long> order = null;
            try
            {
                logger.Error($"");
                logger.Error($"1: 开始下单 -----------------------------{JsonConvert.SerializeObject(req)}");
                order = api.OrderPlace(req);
                logger.Error($"2: 下单结束 -----------------------------{JsonConvert.SerializeObject(order)}");

                if (order.Status == "ok")
                {
                    new DogMoreBuyDao().CreateDogMoreBuy(new DogMoreBuy()
                    {
                        SymbolName = symbol.BaseCurrency,
                        QuoteCurrency = symbol.QuoteCurrency,
                        AccountId = accountId,
                        UserName = userName,

                        BuyQuantity = buyQuantity,
                        BuyOrderPrice = orderPrice,
                        BuyDate = DateTime.Now,
                        BuyOrderResult = JsonConvert.SerializeObject(order),
                        BuyState = StateConst.PreSubmitted,
                        BuyTradePrice = 0,
                        BuyOrderId = order.Data,
                        BuyMemo = "",
                        BuyOrderDetail = "",
                        BuyOrderMatchResults = "",
                        IsFinished = false
                    });

                    // 下单成功马上去查一次
                    QueryBuyDetailAndUpdate(userName, order.Data);
                }
                logger.Error($"3: 入库结束 -----------------------------做多  下单购买结果 {JsonConvert.SerializeObject(req)}, notShougeEmptySellAmount:{notShougeEmptySellAmount}, order：{JsonConvert.SerializeObject(order)}, nowPrice：{nowPrice}, accountId：{accountId},");
                logger.Error($"");
            }
            catch (Exception ex)
            {
                logger.Error($"严重严重 ---------------  下的出错  --------------{JsonConvert.SerializeObject(req)}");
                Thread.Sleep(1000 * 60 * 5);
                throw ex;
            }

        }

        public static string ShouGeDogEmpty(DogEmptySell dogEmptySell, CommonSymbol symbol, AnalyzeResult analyzeResult, bool isShouge = false)
        {
            var nowPrice = analyzeResult.NowPrice;
            var ladderEmptyWhenShouGe = ladderEmptyBuyPercent;
            if (isShouge)
            {
                ladderEmptyWhenShouGe = (decimal)1.05;
            }
            if (nowPrice * ladderEmptyWhenShouGe > dogEmptySell.SellTradePrice)
            {
                Console.WriteLine("没有收益，不能收割");
                return "没有收益，不能收割";
            }

            if (!analyzeResult.CheckCanBuyForHuiDiao(dogEmptySell))
            {
                return "没有回调";
            }

            decimal buyQuantity = CommonHelper.CalcBuyQuantityForEmptyShouge(dogEmptySell.SellQuantity, dogEmptySell.SellTradePrice, nowPrice, symbol);
            if (buyQuantity <= dogEmptySell.SellQuantity || nowPrice * buyQuantity >= dogEmptySell.SellQuantity * dogEmptySell.SellTradePrice)
            {
                Console.WriteLine($"     {symbol.BaseCurrency}{symbol.QuoteCurrency}没有实现双向收益， 不能收割空单");
                return "没有实现双向收益， 不能收割空单";
            }
            decimal orderPrice = decimal.Round(nowPrice * (decimal)1.01, symbol.PricePrecision);

            OrderPlaceRequest req = new OrderPlaceRequest();
            req.account_id = dogEmptySell.AccountId;
            req.amount = buyQuantity.ToString();
            req.price = orderPrice.ToString();
            req.source = "api";
            req.symbol = symbol.BaseCurrency + symbol.QuoteCurrency;
            req.type = "buy-limit";

            if (BuyLimitUtils.Record(dogEmptySell.UserName, symbol.BaseCurrency + symbol.QuoteCurrency))
            {
                logger.Error(" --------------------- 两个小时内购买次数太多，暂停一会 --------------------- ");
                Thread.Sleep(1000 * 5);
                return " 两个小时内购买次数太多，暂停一会";
            }

            PlatformApi api = PlatformApi.GetInstance(dogEmptySell.UserName);
            HBResponse<long> order = null;
            try
            {
                logger.Error($"");
                logger.Error($"1: 开始下单 -----------------------------{JsonConvert.SerializeObject(req)}");
                order = api.OrderPlace(req);
                logger.Error($"2: 下单结束 -----------------------------{JsonConvert.SerializeObject(order)}");

                if (order.Status == "ok")
                {
                    new DogEmptyBuyDao().CreateDogEmptyBuy(new DogEmptyBuy()
                    {
                        SymbolName = symbol.BaseCurrency,
                        QuoteCurrency = symbol.QuoteCurrency,
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
                        BuyMemo = "",
                        BuyOrderDetail = "",
                        BuyOrderMatchResults = "",
                    });
                    // 下单成功马上去查一次
                    QueryEmptyBuyDetailAndUpdate(dogEmptySell.UserName, order.Data);
                }
                logger.Error($"3: 入库结束 ----------------------------- 空单收割");
                logger.Error($"");
                return "运行结束";
            }
            catch (Exception ex)
            {
                logger.Error($"严重严重 --------------- 空单收割出错");
                Thread.Sleep(1000 * 60 * 5);
                throw ex;
            }
        }

        private static void RunSell(CommonSymbol symbol, AnalyzeResult analyzeResult, Ticker ticker)
        {
            if (ticker.symbol != symbol.BaseCurrency + symbol.QuoteCurrency)
            {
                Console.WriteLine("--------------------- 数据错误");
                return;
            }

            var historyKlines = analyzeResult.HistoryKlines;
            var nowPrice = analyzeResult.NowPrice;

            var userNames = UserPools.GetAllUserName();

            // 多单的自动波动收割
            foreach (var userName in userNames)
            {
                var needSellDogMoreBuyList = new DogMoreBuyDao().GetNeedSellDogMoreBuy(userName, symbol.BaseCurrency, symbol.QuoteCurrency);

                foreach (var dogMoreBuyItem in needSellDogMoreBuyList)
                {
                    try
                    {
                        if (ticker.close < dogMoreBuyItem.BuyOrderPrice * (decimal)1.07)
                        {
                            continue;
                        }

                        ShouGeDogMore(dogMoreBuyItem, symbol, analyzeResult);
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"收割出错 {ex.Message} {JsonConvert.SerializeObject(dogMoreBuyItem)}", ex);
                        continue;
                    }
                }
            }

            // 不符合管控的，则不考虑做空
            if (!JudgeBuyUtils.ControlCanSell(symbol.BaseCurrency, symbol.QuoteCurrency, historyKlines, nowPrice))
            {
                if (symbol.BaseCurrency == "xmx")
                {
                    Console.WriteLine("----------------------------------------------------------------------------------------");
                }
                return;
            }

            foreach (var userName in userNames)
            {
                try
                {
                    Console.WriteLine($"---> before doempty {userName}   {symbol.BaseCurrency},{symbol.QuoteCurrency}");

                    // 和上次做空价格要相差8%
                    var maxSellTradePrice = new DogEmptySellDao().GetMaxSellTradePrice(userName, symbol.BaseCurrency, symbol.QuoteCurrency);
                    if (maxSellTradePrice != null && nowPrice < maxSellTradePrice * ladderEmptySellPercent)
                    {
                        continue;
                    }

                    var accountConfig = AccountConfigUtils.GetAccountConfig(userName);
                    var accountId = accountConfig.MainAccountId;

                    // 要减去未收割得。
                    var notShougeQuantity = new DogMoreBuyDao().GetBuyQuantityNotShouge(userName, symbol.BaseCurrency);

                    // 出售
                    decimal sellPrice = decimal.Round(nowPrice * (decimal)0.988, symbol.PricePrecision);

                    // 阶梯有数量差别
                    var minSellEmptyPrice = new DogEmptySellDao().GetMaxSellEmptyPrice(userName, symbol.BaseCurrency, symbol.QuoteCurrency);
                    var sellQuantity = DogControlUtils.GetEmptySize(userName, symbol.BaseCurrency, minSellEmptyPrice, nowPrice);
                    sellQuantity = decimal.Round(sellQuantity, symbol.AmountPrecision);

                    if (
                        (symbol.QuoteCurrency == "usdt" && sellQuantity * nowPrice < (decimal)0.8)
                        || (symbol.QuoteCurrency == "btc" && sellQuantity * nowPrice < (decimal)0.00009)
                        || (symbol.QuoteCurrency == "eth" && sellQuantity * nowPrice < (decimal)0.003)
                        || (symbol.QuoteCurrency == "ht" && sellQuantity * nowPrice < (decimal)0.5)
                        )
                    {
                        Console.WriteLine($"    {symbol.BaseCurrency}{symbol.QuoteCurrency},做空不超过{sellQuantity * nowPrice},, sellQuantity: {sellQuantity},  nowPrice:{nowPrice}");
                        continue;
                    }

                    Console.WriteLine($"准备做空 sellQuantity:{sellQuantity}, nowPrice:{nowPrice}");
                    SellWhenDoEmpty(accountId, userName, symbol, sellQuantity, sellPrice, $"device:");
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message, ex);
                }
            }

        }

        private static void SellWhenDoEmpty(string accountId, string userName, CommonSymbol symbol, decimal sellQuantity, decimal sellPrice, string sellMemo = "")
        {
            try
            {
                if ((symbol.AmountPrecision == 0 && sellQuantity < (decimal)20)
                    || (symbol.AmountPrecision == 1 && sellQuantity < (decimal)2)
                    || (symbol.AmountPrecision == 2 && sellQuantity < (decimal)0.2)
                    || (symbol.AmountPrecision == 3 && sellQuantity < (decimal)0.02)
                    || (symbol.AmountPrecision == 4 && sellQuantity < (decimal)0.002))
                {
                    LogNotBuy(symbol.BaseCurrency + "-empty-sell", $"sell userName:{userName} 出错,{symbol.BaseCurrency} 的精度为 {symbol.AmountPrecision}, 但是却要出售{sellQuantity}  ");
                    return;
                }

                OrderPlaceRequest req = new OrderPlaceRequest();
                req.account_id = accountId;
                req.amount = sellQuantity.ToString();
                req.price = sellPrice.ToString();
                req.source = "api";
                req.symbol = symbol.BaseCurrency + symbol.QuoteCurrency; ;
                req.type = "sell-limit";

                PlatformApi api = PlatformApi.GetInstance(userName);
                HBResponse<long> order = null;
                try
                {
                    logger.Error($"");
                    logger.Error($"1: 开始下单 -----------------------------{JsonConvert.SerializeObject(req)}");
                    order = api.OrderPlace(req);
                    logger.Error($"2: 下单结束 -----------------------------{JsonConvert.SerializeObject(req)}");

                    if (order.Status == "ok")
                    {
                        DogEmptySell dogEmptySell = new DogEmptySell()
                        {
                            AccountId = accountId,
                            UserName = userName,
                            SellOrderId = order.Data,
                            SellOrderResult = JsonConvert.SerializeObject(order),
                            SellDate = DateTime.Now,
                            SellQuantity = sellQuantity,
                            SellOrderPrice = sellPrice,
                            SellState = StateConst.Submitted,
                            SellTradePrice = 0,
                            SymbolName = symbol.BaseCurrency,
                            QuoteCurrency = symbol.QuoteCurrency,
                            SellMemo = sellMemo,
                            SellOrderDetail = "",
                            SellOrderMatchResults = "",
                            IsFinished = false
                        };
                        new DogEmptySellDao().CreateDogEmptySell(dogEmptySell);

                        // 下单成功马上去查一次
                        QueryEmptySellDetailAndUpdate(userName, order.Data);
                    }
                    logger.Error($"3: 入库结束 -----------------------------做空 {JsonConvert.SerializeObject(req)}下单出售结果：" + JsonConvert.SerializeObject(order));
                    logger.Error($"");
                }
                catch (Exception ex)
                {
                    logger.Error($"严重 ---------------  做空出错  --------------{JsonConvert.SerializeObject(req)}");
                    Thread.Sleep(1000 * 60 * 5);
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"SellWhenDoEmpty  {ex.Message}", ex);
            }
        }

        public static void ShouGeDogMore(DogMoreBuy dogMoreBuy, CommonSymbol symbol, AnalyzeResult analyzeResult = null)
        {
            if (analyzeResult == null)
            {
                analyzeResult = AnalyzeResult.GetAnalyzeResult(symbol);
            }
            if (analyzeResult == null)
            {
                return;
            }

            var nowPrice = analyzeResult.NowPrice;

            var thisLadderMoreSellPercent = ladderMoreSellPercent;
            if (analyzeResult.NowPrice / analyzeResult.MinPrice > (decimal)1.20)
            {
                thisLadderMoreSellPercent = (decimal)1.085;
            }
            else if (analyzeResult.NowPrice / analyzeResult.MinPrice > (decimal)1.30)
            {
                thisLadderMoreSellPercent = (decimal)1.08;
            }
            else if (analyzeResult.NowPrice / analyzeResult.MinPrice > (decimal)1.40)
            {
                thisLadderMoreSellPercent = (decimal)1.075;
            }
            else if (analyzeResult.NowPrice / analyzeResult.MinPrice > (decimal)1.50)
            {
                thisLadderMoreSellPercent = (decimal)1.07;
            }
            else if (analyzeResult.NowPrice / analyzeResult.MinPrice > (decimal)1.60)
            {
                thisLadderMoreSellPercent = (decimal)1.065;
            }
            thisLadderMoreSellPercent = Math.Max(thisLadderMoreSellPercent, (decimal)1.065);

            // 没有大于预期, 也不能收割
            if (nowPrice < dogMoreBuy.BuyTradePrice * thisLadderMoreSellPercent)
            {
                return;
            }

            if (!analyzeResult.CheckCanSellForHuiDiao(dogMoreBuy))
            {
                Console.WriteLine("不满足回调");
                // 判断是否有回掉
                return;
            }

            // 计算要出的数量
            decimal sellQuantity = JudgeSellUtils.CalcSellQuantityForMoreShouge(dogMoreBuy.BuyQuantity, dogMoreBuy.BuyTradePrice, nowPrice, symbol);
            // 计算要出的价格
            decimal sellPrice = decimal.Round(nowPrice * (decimal)0.988, symbol.PricePrecision);
            if (sellQuantity >= dogMoreBuy.BuyQuantity)
            {
                Console.WriteLine("出售的量过多");
                return;
            }
            if (sellQuantity * sellPrice <= dogMoreBuy.BuyQuantity * dogMoreBuy.BuyTradePrice)
            {
                //logger.Error($"{dogMoreBuy.SymbolName}{dogMoreBuy.QuoteCurrency} 未实现双向收益 sellQuantity:{sellQuantity}, BuyQuantity:{dogMoreBuy.BuyQuantity}，sellQuantity * nowPrice：{sellQuantity * nowPrice}，dogMoreBuy.BuyQuantity * dogMoreBuy.BuyTradePrice：{dogMoreBuy.BuyQuantity * dogMoreBuy.BuyTradePrice}");
                return;
            }

            OrderPlaceRequest req = new OrderPlaceRequest();
            req.account_id = dogMoreBuy.AccountId;
            req.amount = sellQuantity.ToString();
            req.price = sellPrice.ToString();
            req.source = "api";
            req.symbol = symbol.BaseCurrency + symbol.QuoteCurrency; ;
            req.type = "sell-limit";
            PlatformApi api = PlatformApi.GetInstance(dogMoreBuy.UserName);
            HBResponse<long> order = null;
            try
            {
                logger.Error($"");
                logger.Error($"1:开始下单 -----------------------------{JsonConvert.SerializeObject(req)}");
                order = api.OrderPlace(req);
                logger.Error($"2:下单结束 -----------------------------{JsonConvert.SerializeObject(order)}");

                // 下单出错, 报了异常, 也需要查询是否下单成功. 查询最近的订单.
                if (order.Status == "ok")
                {
                    DogMoreSell dogMoreSell = new DogMoreSell()
                    {
                        AccountId = dogMoreBuy.AccountId,
                        UserName = dogMoreBuy.UserName,
                        BuyOrderId = dogMoreBuy.BuyOrderId,
                        SellOrderId = order.Data,
                        SellOrderResult = JsonConvert.SerializeObject(order),
                        SellDate = DateTime.Now,
                        SellQuantity = sellQuantity,
                        SellOrderPrice = sellPrice,
                        SellState = StateConst.Submitted,
                        SellTradePrice = 0,
                        SymbolName = symbol.BaseCurrency,
                        QuoteCurrency = symbol.QuoteCurrency,
                        SellMemo = "",
                        SellOrderDetail = "",
                        SellOrderMatchResults = ""
                    };
                    new DogMoreSellDao().CreateDogMoreSell(dogMoreSell);

                    // 下单成功马上去查一次
                    QuerySellDetailAndUpdate(dogMoreBuy.UserName, order.Data);
                }
                logger.Error($"3:入库结束 ----------------------------- 多单收割 --> {JsonConvert.SerializeObject(dogMoreBuy)}");
                logger.Error($"");
            }
            catch (Exception ex)
            {
                logger.Error($"严重严重111111 -----------------------------  多单收割出错");
                Thread.Sleep(1000 * 60 * 5);
                throw ex;
            }
        }

        #region 手动

        public static void DoEmpty(CommonSymbol symbol, string userName, string accountId)
        {
            AnalyzeResult analyzeResult = AnalyzeResult.GetAnalyzeResult(symbol);
            if (analyzeResult == null)
            {
                throw new ApplicationException("做空失败，分析出错");
            }
            var nowPrice = analyzeResult.NowPrice;

            if (nowPrice * (decimal)1.06 < analyzeResult.MaxPrice)
            {
                throw new ApplicationException("已经降低了6%， 不要做空，谨慎起见");
            }

            var maxSellTradePrice = new DogEmptySellDao().GetMaxSellTradePrice(userName, symbol.BaseCurrency, symbol.QuoteCurrency);
            if (maxSellTradePrice != null && nowPrice < maxSellTradePrice * (decimal)1.06)
            {
                throw new ApplicationException("有价格比这个更高得还没有收割。不能重新做空。");
            }

            PlatformApi api = PlatformApi.GetInstance(userName);

            var accountInfo = api.GetAccountBalance(AccountConfigUtils.GetAccountConfig(userName).MainAccountId);
            var balanceItem = accountInfo.Data.list.Find(it => it.currency == symbol.BaseCurrency);
            // 要减去未收割得。
            var notShougeQuantity = new DogMoreBuyDao().GetBuyQuantityNotShouge(userName, symbol.BaseCurrency);
            if (notShougeQuantity >= balanceItem.balance || notShougeQuantity <= 0)
            {
                logger.Error($"未收割得数量大于余额，有些不合理，  {symbol.BaseCurrency},, {userName},, {notShougeQuantity}, {balanceItem.balance}");
                return;
            }

            var devide = DogControlUtils.GetRecommendDivideForEmpty(symbol.BaseCurrency, symbol.QuoteCurrency, nowPrice, (balanceItem.balance - notShougeQuantity));
            decimal sellQuantity = (balanceItem.balance - notShougeQuantity) / devide; // 暂定每次做空1/12

            if (sellQuantity * nowPrice > 10) // 一次做空不超过10usdt
            {
                sellQuantity = 10 / nowPrice;
            }
            sellQuantity = decimal.Round(sellQuantity, symbol.AmountPrecision);

            if (sellQuantity * nowPrice < 1)
            {
                sellQuantity = (balanceItem.balance - notShougeQuantity) / 10;
                if (sellQuantity * nowPrice < 1)
                {
                    sellQuantity = (balanceItem.balance - notShougeQuantity) / 5;
                    if (sellQuantity * nowPrice < 1)
                    {
                        sellQuantity = (balanceItem.balance - notShougeQuantity) / 3;
                        if (sellQuantity * nowPrice < (decimal)0.2)
                        {
                            LogNotBuy(symbol.BaseCurrency, $"收益不超过0.2usdt,, balance: {balanceItem.balance},  notShougeQuantity:{notShougeQuantity}, {nowPrice}, yu: {(balanceItem.balance - notShougeQuantity) * nowPrice}");
                            return;
                        }
                    }
                }
            }

            // 出售
            decimal sellPrice = decimal.Round(nowPrice * (decimal)0.985, symbol.PricePrecision);

            SellWhenDoEmpty(accountId, userName, symbol, sellQuantity, sellPrice);
        }

        public static string BuyWhenDoMoreAnalyze(CommonSymbol symbol, AccountConfig account, decimal ladderBuyPercent)
        {
            AnalyzeResult analyzeResult = AnalyzeResult.GetAnalyzeResult(symbol);
            if (analyzeResult == null)
            {
                // 初始化数据, 再次拿去
                analyzeResult = AnalyzeResult.GetAnalyzeResult(symbol);
                if (analyzeResult == null)
                {
                    throw new ApplicationException("做多失败，分析出错");
                }
            }

            var historyKlines = analyzeResult.HistoryKlines;
            var nowPrice = analyzeResult.NowPrice;

            // 1.低于管控的购入价
            var controlCanBuy = JudgeBuyUtils.ControlCanBuy(symbol.BaseCurrency, symbol.QuoteCurrency, nowPrice);
            if (!controlCanBuy)
            {
                return $"判断 发现不适合 controlCanBuy:{controlCanBuy}";
            }

            try
            {
                BuyWhenDoMore(symbol, account, analyzeResult);
            }
            catch (Exception ex)
            {
                //logger.Error($"{userName} {JsonConvert.SerializeObject(symbol)} {ex.Message}", ex);
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex);
                return ex.Message;
            }
            return "----";
        }

        #endregion

        #region 查询结果

        private static void QueryBuyDetailAndUpdate(string userName, long orderId)
        {
            try
            {
                PlatformApi api = PlatformApi.GetInstance(userName);

                var orderDetail = api.QueryOrderDetail(orderId);
                if (orderDetail.Status == "ok" && orderDetail.Data.state == "filled")
                {
                    var orderMatchResult = api.QueryOrderMatchResult(orderId);
                    if (orderMatchResult == null || orderMatchResult.Data == null)
                    {
                        return;
                    }
                    decimal maxPrice = 0;
                    foreach (var item in orderMatchResult.Data)
                    {
                        if (maxPrice < item.price)
                        {
                            maxPrice = item.price;
                        }
                    }
                    if (orderMatchResult.Status == "ok" && maxPrice > 0)
                    {
                        new DogMoreBuyDao().UpdateDogMoreBuySuccess(orderId, orderDetail, orderMatchResult, maxPrice);
                    }
                }

                if (orderDetail.Status == "ok" && orderDetail.Data.state == StateConst.PartialCanceled)
                {
                    var orderMatchResult = api.QueryOrderMatchResult(orderId);
                    decimal maxPrice = 0;
                    decimal buyQuantity = 0;
                    foreach (var item in orderMatchResult.Data)
                    {
                        if (maxPrice < item.price)
                        {
                            maxPrice = item.price;
                        }
                        buyQuantity += item.FilledAmount;
                    }
                    if (orderMatchResult.Status == "ok")
                    {
                        new DogMoreBuyDao().UpdateDogMoreBuySuccess(orderId, buyQuantity, orderDetail, orderMatchResult, maxPrice);
                    }
                }

                if (orderDetail.Status == "ok" && orderDetail.Data.state == StateConst.Canceled)
                {
                    // 完成
                    new DogMoreBuyDao().UpdateDogMoreBuyWhenCancel(orderId);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                logger.Error($"QueryBuyDetailAndUpdate, orderId:{orderId}  查询数据出错");
            }
        }

        private static void QuerySellDetailAndUpdate(string userName, long orderId)
        {
            try
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
                    if (orderMatchResult.Status == "ok" && minPrice > 0)
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
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
                logger.Error("QuerySellDetailAndUpdate 查询结果时候出错");
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
                //Console.WriteLine($"needChangeBuyStateDogEmptyBuyList: {needChangeBuyStateDogEmptyBuyList.Count}");
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
            try
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
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }

        private static void QueryEmptyBuyDetailAndUpdate(string userName, long orderId)
        {
            try
            {
                PlatformApi api = PlatformApi.GetInstance(userName);

                var orderDetail = api.QueryOrderDetail(orderId);

                Console.WriteLine($"orderDetail: {JsonConvert.SerializeObject(orderDetail)}");
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
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }

        #endregion

        #region 记录日志

        private static Dictionary<string, DateTime> dic = new Dictionary<string, DateTime>();
        public static void LogNotBuy(string symbolName, string reason)
        {
            try
            {
                if (dic.ContainsKey(symbolName) && dic[symbolName] > DateTime.Now.AddMinutes(-60))
                {
                    return;
                }
                logger.Error(symbolName + " ------" + reason);
                if (dic.ContainsKey(symbolName))
                {
                    dic[symbolName] = DateTime.Now;
                }
                else
                {
                    dic.Add(symbolName, DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }

        #endregion

        public static void RefreshHistoryMaxMinAsync(string symbolName, string quoteCurrency)
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
    }
}
