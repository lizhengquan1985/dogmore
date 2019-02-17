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
            if (control == null || control.HistoryMin <= 0 || nowPrice > control.MaxInputPrice)
            {
                return false;
            }
            return true;
        }

        public static bool ControlCanSell(string symbolName, string quoteCurrency, List<HistoryKline> historyKlines, decimal nowPrice)
        {
            var control = new DogControlDao().GetDogControl(symbolName, quoteCurrency);
            if (control == null || control.HistoryMin <= 0)
            {
                // 未管控的不能操作
                return false;
            }

            if (nowPrice <= control.EmptyPrice || nowPrice < control.HistoryMin * 2)
            {
                return false;
            }

            var maxPrice = historyKlines.Max(it => it.Close);
            var minPrice = historyKlines.Min(it => it.Close);

            if (nowPrice > minPrice * 2 && nowPrice > (control.HistoryMax - control.HistoryMin) * (decimal)0.3 + control.HistoryMin)
            {
                // 涨了1倍的，也可以空
                return true;
            }

            if(nowPrice <= (control.HistoryMax - control.HistoryMin) * (decimal)0.6 + control.HistoryMin)
            {
                return false;
            }

            return true;
        }
    }
}
