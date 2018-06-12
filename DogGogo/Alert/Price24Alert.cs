using DogPlatform;
using DogPlatform.Model;
using DogService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Alert
{
    /// <summary>
    /// 用来提醒，如果24小时内有价格异常波动，大起大落的，则要提醒
    /// </summary>
    public class Price24Alert
    {
        public static void RunAlert()
        {
            var symbols = CoinUtils.GetAllCommonSymbols();
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        foreach (var symbol in symbols)
                        {
                            PlatformApi api = PlatformApi.GetInstance("xx");
                            var period = "1min";
                            var klines = api.GetHistoryKline(symbol.BaseCurrency + symbol.QuoteCurrency, period);

                            var nowPrice = klines[0].Close;
                            // 5分钟内有5%， 或者 15分钟内有10%
                            foreach (var item in klines)
                            {
                                if ( Utils.GetDateById(item.Id) > DateTime.Now.AddMinutes(-5) && (
                                    item.Close / nowPrice > (decimal)1.05 || nowPrice / item.Close  > (decimal)1.05))
                                {
                                    OpenUrlUtils.Open();
                                }

                                if (Utils.GetDateById(item.Id) > DateTime.Now.AddMinutes(-15) && (
                                    item.Close / nowPrice > (decimal)1.10 || nowPrice / item.Close > (decimal)1.10))
                                {
                                    OpenUrlUtils.Open();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
            });
        }
    }
}
