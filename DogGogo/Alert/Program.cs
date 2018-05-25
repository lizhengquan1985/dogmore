using DogPlatform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alert
{
    class Program
    {
        static void Main(string[] args)
        {
            var symbols = CoinUtils.GetAllCommonSymbols();
            System.Diagnostics.Process.Start("explorer.exe", "http://blog.csdn.net/testcs_dn");
            while (true)
            {
                Console.WriteLine("请输入symbol");
                var symbol = Console.ReadLine();
                Console.WriteLine("请输入big");
                var big = Console.ReadLine();
                Console.WriteLine("请输入small");
                var small = Console.ReadLine();

                PriceAlert.RunAlert(symbols.Find(it=>it.BaseCurrency == symbol), decimal.Parse(big), decimal.Parse(small));
            }


            Console.ReadLine();
        }
    }
}
