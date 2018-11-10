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

        public static bool IsQuickDrop(CommonSymbol symbol, List<HistoryKline> historyKlines)
        {
            // 暂时判断 1个小时内是否上涨超过12%， 如果超过，则控制下
            var nowClose = historyKlines[0].Close;
            var dayMin = historyKlines.Min(it => it.Open);
            var dayMax = historyKlines.Max(it => it.Open);
            var hourMin = historyKlines.Where(it => Utils.GetDateById(it.Id) >= DateTime.Now.AddHours(-1)).Min(it => it.Open);
            var hourMax = historyKlines.Where(it => Utils.GetDateById(it.Id) >= DateTime.Now.AddHours(-1)).Max(it => it.Open);

            bool isQuickDrop = false;

            if (dayMax > dayMin * (decimal)2.00
                && nowClose < dayMin * (decimal)1.40)
            {
                logger.Error($"一天内有大量的下降，防止追跌，所以不能交易。coin:{symbol.BaseCurrency}{symbol.QuoteCurrency}, nowClose:{nowClose}, dayMin:{dayMin}, dayMax:{dayMax}");
                isQuickDrop = true;
            }

            // 1. 一天上涨超过60%,  2. 现在价位还在超过40%  3. 现在价位还在上半截
            if (dayMax > dayMin * (decimal)1.60
                && nowClose < dayMin * (decimal)1.20
                && nowClose < dayMin * (1 + (decimal)0.4 * (dayMax - dayMin) / dayMin))
            {
                logger.Error($"一天内有大量的下降，防止追跌，所以不能交易。coin:{symbol.BaseCurrency}{symbol.QuoteCurrency}, nowClose:{nowClose}, dayMin:{dayMin}, dayMax:{dayMax}");
                isQuickDrop = true;
            }

            // 1. 一个小时上涨超过30%,  2. 现在价位还在超过12%  3. 现在价位还在上半截
            if (hourMax > hourMin * (decimal)1.3
                && nowClose < hourMin * (decimal)1.13
                && nowClose < hourMin * (1 + (decimal)0.4 * (hourMax - hourMin) / hourMin))
            {
                logger.Error($"一个小时内有大量的下降，防止追跌，所以不能交易。coin:{symbol.BaseCurrency}{symbol.QuoteCurrency}, nowClose:{nowClose}, hourMin:{hourMin}, hourMax:{hourMax}");
                isQuickDrop = true;
            }

            return isQuickDrop;
        }

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
                Console.WriteLine($"{symbolName}{quoteCurrency}由于管控null,不能出售");
                // 未管控的不能操作
                return false;
            }

            if (nowPrice <= control.EmptyPrice || nowPrice < control.HistoryMin * 2 || nowPrice <= (control.HistoryMax - control.HistoryMin) * (decimal)0.6 + control.HistoryMin)
            {
                Console.WriteLine($"{symbolName}{quoteCurrency}由于管控,不能出售 EmptyPrice:{control.EmptyPrice}>=nowPrice:{nowPrice}");
                return false;
            }

            return true;
        }
    }
}
