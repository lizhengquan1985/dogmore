using DogPlatform;
using DogPlatform.Model;
using System;
using System.Collections.Generic;
using System.IO;
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

            // 空
            EmptyOrderTask.Run(symbols);

            ReadAutoBuySellText(symbols);

            while (true)
            {
                Console.WriteLine("请输入symbol");
                var symbol = Console.ReadLine();
                Console.WriteLine("请输入big");
                var big = Console.ReadLine();
                Console.WriteLine("请输入small");
                var small = Console.ReadLine();

                PriceAlert.RunAlert(symbols.Find(it => it.BaseCurrency == symbol), decimal.Parse(big), decimal.Parse(small));
            }

            Console.WriteLine("over - over - over");
            Console.ReadLine();
        }


        static void ReadAutoBuySellText(List<CommonSymbols> symbols)
        {
            var fileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            var root = Path.GetDirectoryName(fileName);
            if (!File.Exists(root + @"\symbol.txt"))
            {
                File.Create(root + @"\symbol.txt");
            }

            var strArr = File.ReadAllLines(root + @"\symbol.txt");
            Console.WriteLine(root);

            foreach (var str in strArr)
            {
                var arr = str.Split(' ');
                var symbol = arr[0];
                var big = arr[1];
                var small = arr[2];
                PriceAlert.RunAlert(symbols.Find(it => it.BaseCurrency == symbol), decimal.Parse(big), decimal.Parse(small));
            }
        }
    }
}
