using DogPlatform;
using DogPlatform.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Alert
{
    class PriceAlert
    {
        public static void RunAlert(CommonSymbol symbol, decimal big, decimal small)
        {
            Task.Run(() =>
            {
                while (true)
                {

                    try
                    {

                        PlatformApi api = PlatformApi.GetInstance("xx");
                        var period = "1min";
                        var klines = api.GetHistoryKline(symbol.BaseCurrency + symbol.QuoteCurrency, period);

                        var nowPrice = klines[0].Close;
                        Console.WriteLine(klines[0].Close);

                        if(nowPrice > big)
                        {

                            System.Diagnostics.Process.Start("explorer.exe", "http://blog.csdn.net/testcs_dn");
                        }

                        if(nowPrice < small)
                        {
                            System.Diagnostics.Process.Start("explorer.exe", "http://blog.csdn.net/testcs_dn");
                        }

                    }
                    catch (Exception ex)
                    {

                    }
                    Thread.Sleep(1000 * 5);
                }
            });

        }
    }
}
