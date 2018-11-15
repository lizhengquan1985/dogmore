using DogPlatform.Model;
using DogService;
using DogService.Dao;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogRunService
{
    public class JudgeBuyUtils
    {
        static ILog logger = LogManager.GetLogger(typeof(JudgeBuyUtils));

        public static bool ControlCanBuy(string symbolName, string quoteCurrency, decimal nowPrice)
        {
            var control = new DogControlDao().GetDogControl(symbolName, quoteCurrency);
            if (control == null || nowPrice > control.MaxInputPrice)
            {
                Console.WriteLine($"{symbolName}{quoteCurrency} -- 由于管控,不能购入 MaxInputPrice:{(control?.MaxInputPrice.ToString() ?? "无设置")}, nowPrice:{nowPrice}");
                return false;
            }
            return true;
        }

        public static bool ControlCanSell(string symbolName, string quoteCurrency, decimal nowPrice)
        {
            var control = new DogControlDao().GetDogControl(symbolName, quoteCurrency);
            if (control == null || control.HistoryMin <= 0)
            {
                Console.WriteLine($"    {symbolName}{quoteCurrency}由于管控，未设置管控null,不能出售");
                // 未管控的不能操作
                return false;
            }

            if (nowPrice <= control.EmptyPrice || nowPrice < control.HistoryMin * 2 || nowPrice <= (control.HistoryMax - control.HistoryMin) * (decimal)0.6 + control.HistoryMin)
            {
                Console.WriteLine($"    {symbolName}{quoteCurrency}由于管控,不能出售 要大于CanEmptyPrice:{control.EmptyPrice}才能出售 >=nowPrice:{nowPrice}");
                return false;
            }

            return true;
        }
    }
}
