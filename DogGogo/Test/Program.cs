using DogAccount;
using DogPlatform;
using DogService;
using DogService.Dao;
using DogService.DateTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            //SearchOrder(6542774998);
            SearchOrder(12367172910);
            //GetAccount();

            //var i = "";
            //foreach (var orderId in list)
            //{
            //    Console.WriteLine(orderId);
            //    if (i != "ttt")
            //    {
            //        i = Console.ReadLine();
            //    }
            //    SearchBuyOrder(orderId);
            //}
            Console.WriteLine("over 1");

            Console.ReadLine();
        }

        public static void GetAccount()
        {
            var userName = "qq";
            PlatformApi api = PlatformApi.GetInstance(userName);

            var accounts = api.GetAllAccount();
            Console.WriteLine(JsonConvert.SerializeObject(accounts));

            var account = api.GetAccountBalance("529880");
            Console.WriteLine(JsonConvert.SerializeObject(account));
        }

        public static void SearchOrder(long orderId)
        {
            var userName = "qq";
            AccountConfig account = AccountConfigUtils.GetAccountConfig(userName);

            PlatformApi api = PlatformApi.GetInstance(userName);

            var orderDetail = api.QueryOrderDetail(orderId);
            Console.WriteLine(orderDetail.Status);
            Console.WriteLine(orderDetail.Data.state);
            if (orderDetail.Status == "ok" && orderDetail.Data.state == "filled")
            {
                Console.WriteLine(JsonConvert.SerializeObject(orderDetail));
                Console.WriteLine(orderDetail.Data.id);
                Console.WriteLine(orderDetail.Data.symbol.Replace("usdt", ""));
                Console.WriteLine(orderDetail.Data.amount);

                if (orderDetail.Data.type != "sell-limit")
                {
                    Console.WriteLine(orderDetail.Data.type);
                    Thread.Sleep(1000 * 60 * 5);
                }

                if (new DogEmptySellDao().GetDogEmptySellBySellOrderId(orderId) != null)
                {
                    Console.WriteLine("订单存在");
                    return;
                }

                new DogEmptySellDao().CreateDogEmptySell(new DogEmptySell()
                {
                    SymbolName = orderDetail.Data.symbol.Replace("usdt", ""),
                    AccountId = account.MainAccountId,
                    UserName = account.UserName,
                    FlexPercent = (decimal)1.04,

                    SellQuantity = orderDetail.Data.amount,
                    SellOrderPrice = orderDetail.Data.price,
                    SellDate = DateTime.Now,
                    SellOrderResult = "",
                    SellState = StateConst.Submitting,
                    SellTradePrice = 0,
                    SellOrderId = orderId,
                    SellFlex = "",
                    SellMemo = "",
                    SellOrderDetail = "",
                    SellOrderMatchResults = "",
                    IsFinished = false
                });
            }


        }

        public static void SearchBuyOrder(long orderId)
        {
            var userName = "xx";
            AccountConfig account = AccountConfigUtils.GetAccountConfig(userName);

            PlatformApi api = PlatformApi.GetInstance(userName);

            var orderDetail = api.QueryOrderDetail(orderId);
            Console.WriteLine(orderDetail.Status);
            Console.WriteLine(orderDetail.Data.state);
            if (orderDetail.Status == "ok" && orderDetail.Data.state == "filled")
            {
                Console.WriteLine(JsonConvert.SerializeObject(orderDetail));
                Console.WriteLine(orderDetail.Data.id);
                Console.WriteLine(orderDetail.Data.symbol.Replace("usdt", ""));
                Console.WriteLine(orderDetail.Data.amount);

                if (orderDetail.Data.type != "buy-limit")
                {
                    Console.WriteLine(orderDetail.Data.type);
                    Thread.Sleep(1000 * 60 * 5);
                }

                if (new DogMoreBuyDao().GetByBuyOrderId(orderId) != null)
                {
                    Console.WriteLine("订单存在");
                    return;
                }

                new DogMoreBuyDao().CreateDogMoreBuy(new DogMoreBuy()
                {
                    SymbolName = orderDetail.Data.symbol.Replace("usdt", ""),
                    AccountId = account.MainAccountId,
                    UserName = account.UserName,
                    FlexPercent = (decimal)1.04,

                    BuyQuantity = orderDetail.Data.amount,
                    BuyOrderPrice = orderDetail.Data.price,
                    BuyDate = DateTime.Now,
                    BuyOrderResult = "",
                    BuyState = StateConst.Submitting,
                    BuyTradePrice = 0,
                    BuyOrderId = orderId,
                    BuyFlex = "",
                    BuyMemo = "",
                    BuyOrderDetail = "",
                    BuyOrderMatchResults = "",
                    IsFinished = false
                });
            }


        }
    }
}
