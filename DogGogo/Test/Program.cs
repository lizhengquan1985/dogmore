using DogAccount;
using DogPlatform;
using DogService;
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
            var list = new List<long>() {
                4837632202  ,
4837650208  ,
4837668499  ,
4837685228  ,
4837702585  ,
4837719925  ,
4837737756  ,
4837756813  ,
4837775394  ,
4837793985  ,
4837813243  ,
4837831303  ,
4837849887  ,
4837867618  ,
4837885011  ,
4837903034  ,
4837920726  ,
4837938194  ,
4837957082  ,
4837975040  ,
4837993018  ,
4838011127  ,
4838029254  ,
4838046696  ,
4838063865  ,
4838570204  ,
4838696851  ,
4838716251  ,
4838735366  ,
4838754773  ,
4838773803  ,
4838791728  ,
4838810535  ,
4838829967  ,
4838849015  ,
4838867290  ,
4838885182  ,
4838902986  ,
4838920344  ,
4838938130  ,
4838956034  ,
4838974607  ,
4838992720  ,
4839011070  ,
4839029566  ,
4844937832  ,
4844970401  ,
4845622456  ,
4845642892  ,
4845781715  ,
4845821532  ,
4845841612  ,
4845882280  ,
4846254472  ,
4846441772  ,
4846533102  ,
4846551897  ,
4846889150  ,
4847096675  ,
4847482064  ,
4849460361  ,
4850443430  ,
4850571233  ,
4850761941  ,
4831037310  ,
4831056715  ,
4831076527  ,
4831096888  ,
4831117615  ,
4832836877  ,
4832856830  ,
4832878901  ,
4832900815  ,
4832920219  ,
4832938534  ,
4832957630  ,
4832975554  ,
4832993590  ,
4833011619  ,
4833030962  ,
4833049447  ,
4833068001  ,
4833086769  ,
4833105107  ,
4833122791  ,
4833140430  ,
4833158738  ,
4833176113  ,
4833195307  ,
4833214835  ,
4833233880  ,
4833252465  ,
4833269431  ,
4833286459  ,
4833305275  ,
4833323138  ,
4833342950  ,
4833364508  ,
4833386481  ,
4833405523  ,
4833425958  ,
4833445499  ,
4833465074  ,
4833482802  ,
4833500217  ,
4833519341  ,
4833539958  ,
4833559348  ,
4833577307  ,
4833594752  ,
4833612332  ,
4833632085  ,
4833650413  ,
4833668134  ,
4833686061  ,
4833703988  ,
4833720862  ,
4835426535  ,
4835444860  ,
4835941013  ,
4835959798  ,
4835979075  ,
4836066256  ,
4836088516  ,
4836110091  ,
4836130194  ,
4836149783  ,
4836169385  ,
4836189394  ,
4836208821  ,
4836227807  ,
4836247482  ,
4836266767  ,
4836286277  ,
4836304627  ,
4836322266  ,
4836339899  ,
4836357872  ,
4836376121  ,
4836393697  ,
4836412394  ,
4836423399  ,
4836430714  ,
4836449437  ,
4836460236  ,
4836468013  ,
4836478962  ,
4836486756  ,
4836518912  ,
4836537325  ,
4836556388  ,
4836574471  ,
4836592826  ,
4836610910  ,
4836627776  ,
4836646269  ,
4836664084  ,
4836681481  ,
4836698687  ,
4836715856  ,
4836733326  ,
4836751220  ,
4836768418  ,
4836785696  ,
4836803029  ,
4836820657  ,
4836837387  ,
4836854776  ,
4836872289  ,
4836889755  ,
4836911820  ,
4836932080  ,
4836952440  ,
4836972131  ,
4836990739  ,
4837009836  ,
4837028728  ,
4837046278  ,
4837065027  ,
4837083991  ,
4837101725  ,
4837120565  ,
4837138842  ,
4837157124  ,
4837258894  ,
4837280389  ,
4837299880  ,
4837318749  ,
4837337306  ,
4837356870  ,
4837376112  ,
4837400401  ,
4837418557  ,
4837436227  ,
4837452982  ,
4837470734  ,
4837488143  ,
4837510866  ,
4837528627  ,
4837545296  ,
4837561682  ,
4837578882  ,
4837596091  ,
4837614400  ,
            };
            var i = "";
            foreach (var orderId in list)
            {
                if (i != "ttt")
                {
                    i = Console.ReadLine();
                }
                SearchOrder(orderId);
            }
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

                if(orderDetail.Data.type != "sell-limit")
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
    }
}
