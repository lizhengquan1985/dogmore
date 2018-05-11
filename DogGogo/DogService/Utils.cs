using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService
{
    public class Utils
    {
        /// <summary>
        /// 通过id获得时间
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static DateTime GetDateById(long id)
        {
            return new DateTime(id * 10000000 + new DateTime(1970, 1, 1, 8, 0, 0).Ticks);
        }

        /// <summary>
        /// 获取某个日期当天最小值
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime GetSmallestOfTheDate(DateTime date)
        {
            var year = date.Year;
            var month = date.Month;
            var day = date.Day;
            return new DateTime(year, month, day, 0, 0, 0);
        }

        /// <summary>
        /// 获取某个日期当天最大值
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime GetBiggestOfTheDate(DateTime date)
        {
            var year = date.Year;
            var month = date.Month;
            var day = date.Day;
            return new DateTime(year, month, day, 23, 59, 59);
        }
    }
}
