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

        public static bool CheckCanBuyForHuiDiao(decimal nowOpen, decimal nearLowOpen)
        {
            return nowOpen > nearLowOpen * (decimal)1.005 && nowOpen < nearLowOpen * (decimal)1.06;
        }

        /// <summary>
        /// 上涨很快的,要控制购入.
        /// </summary>
        /// <param name="coin"></param>
        /// <param name="historyKlines">24小时的数据</param>
        /// <returns></returns>
        public static bool IsQuickRise(CommonSymbols symbol, List<HistoryKline> historyKlines)
        {
            // 暂时判断 1个小时内是否上涨超过12%， 如果超过，则控制下
            var nowClose = historyKlines[0].Close;
            var dayMin = historyKlines.Min(it => it.Open);
            var dayMax = historyKlines.Max(it => it.Open);
            var hourMin = historyKlines.Where(it => Utils.GetDateById(it.Id) >= DateTime.Now.AddHours(-1)).Min(it => it.Open);
            var hourMax = historyKlines.Where(it => Utils.GetDateById(it.Id) >= DateTime.Now.AddHours(-1)).Max(it => it.Open);

            bool isQuickRise = false;

            if (dayMax > dayMin * (decimal)2.00
                && nowClose > dayMin * (decimal)1.60)
            {
                logger.Error($"一天内有大量的上涨，防止追涨，所以不能交易。coin:{symbol.BaseCurrency}{symbol.QuoteCurrency}, nowClose:{nowClose}, dayMin:{dayMin}, dayMax:{dayMax}");
                isQuickRise = true;
            }

            // 1. 一天上涨超过50%,  2. 现在价位还在超过40%  3. 现在价位还在上半截
            if (dayMax > dayMin * (decimal)1.60
                && nowClose > dayMin * (decimal)1.40
                && nowClose > dayMin * (1 + (decimal)0.6 * (dayMax - dayMin) / dayMin))
            {
                logger.Error($"一天内有大量的上涨，防止追涨，所以不能交易。coin:{symbol.BaseCurrency}{symbol.QuoteCurrency}, nowClose:{nowClose}, dayMin:{dayMin}, dayMax:{dayMax}");
                isQuickRise = true;
            }

            // 1. 一个小时上涨超过30%,  2. 现在价位还在超过12%  3. 现在价位还在上半截
            if (hourMax > hourMin * (decimal)1.3
                && nowClose > hourMin * (decimal)1.20
                && nowClose > hourMin * (1 + (decimal)0.6 * (hourMax - hourMin) / hourMin))
            {
                logger.Error($"一个小时内有大量的上涨，防止追涨，所以不能交易。coin:{symbol.BaseCurrency}{symbol.QuoteCurrency}, nowClose:{nowClose}, hourMin:{hourMin}, hourMax:{hourMax}");
                isQuickRise = true;
            }

            return isQuickRise;
        }

        public static bool ControlCanBuy(string symbolName, string quoteCurrency, decimal nowPrice)
        {
            var maxInputPrice = DogControlUtils.GetMaxInputPrice(symbolName, quoteCurrency);
            if (maxInputPrice == null)
            {
                // 没有控制的,默认可以购买
                return true;
            }

            if (nowPrice > maxInputPrice)
            {
                logger.Error($"由于管控,不能购入 MaxInputPrice:{maxInputPrice}, nowPrice:{nowPrice}");
                return false;
            }

            return true;
        }
    }
}
