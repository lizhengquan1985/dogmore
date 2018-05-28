using DogPlatform;
using DogPlatform.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Alert
{
    /// <summary>
    /// fileName: autoempty
    /// symbol, order, price, orderid
    /// </summary>
    public class EmptyOrderTask
    {
        public static void Run(List<CommonSymbols> symbols)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    var fileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                    var root = Path.GetDirectoryName(fileName);
                    fileName = root + "/autoempty.txt";
                    if (!File.Exists(fileName))
                    {
                        Thread.Sleep(1000 * 5);
                        continue;
                    }

                    var strArr = File.ReadAllLines(fileName);
                    Console.WriteLine(root);

                    foreach (var str in strArr)
                    {
                        if (str.StartsWith("#"))
                        {
                            continue;
                        }

                        var arr = str.Split(' ');
                        var symbol = arr[0];
                        var order = arr[1];
                        var price = arr[2];
                        var orderId = arr[3];
                        if(order == "shouge")
                        {
                            Shouge(symbols.Find(it => it.BaseCurrency == symbol), decimal.Parse(price), orderId);
                        }
                        if (order == "forceShouge")
                        {
                            ForceShouge(symbols.Find(it => it.BaseCurrency == symbol), decimal.Parse(price), orderId);
                        }
                    }
                }
            });
        }

        public static void Shouge(CommonSymbols symbol, decimal price, string orderId)
        {
            try
            {

                PlatformApi api = PlatformApi.GetInstance("xx");
                var period = "1min";
                var klines = api.GetHistoryKline(symbol.BaseCurrency + symbol.QuoteCurrency, period);

                var nowPrice = klines[0].Close;
                Console.WriteLine(klines[0].Close);

                if (nowPrice < price)
                {
                    System.Diagnostics.Process.Start("explorer.exe", "http://118.31.44.235/api/empty/shouge?orderid="+orderId);
                }

            }
            catch (Exception ex)
            {

            }
            Thread.Sleep(1000 * 5);
        }

        public static void ForceShouge(CommonSymbols symbol, decimal price, string orderId)
        {
            try
            {
                PlatformApi api = PlatformApi.GetInstance("xx");
                var period = "1min";
                var klines = api.GetHistoryKline(symbol.BaseCurrency + symbol.QuoteCurrency, period);

                var nowPrice = klines[0].Close;
                Console.WriteLine(klines[0].Close);

                if (nowPrice < price)
                {
                    System.Diagnostics.Process.Start("explorer.exe", "http://118.31.44.235/api/empty/forceShouge?orderid=" + orderId);
                }
            }
            catch (Exception ex)
            {

            }
            Thread.Sleep(1000 * 5);
        }
    }
}
