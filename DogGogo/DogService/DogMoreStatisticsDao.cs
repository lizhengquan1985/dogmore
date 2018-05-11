using DogService.DateTypes;
using DogService.DTO;
using log4net;
using SharpDapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService
{
    public class DogMoreStatisticsDao : BaseDao
    {
        static ILog logger = LogManager.GetLogger(typeof(DogMoreStatisticsDao));

        public DogMoreStatisticsDao() : base()
        {
        }

        public async Task<List<PigMore>> ListTodayTrade(string userName)
        {
            var smallDate = Utils.GetSmallestOfTheDate(DateTime.Now);
            var bigDate = Utils.GetBiggestOfTheDate(DateTime.Now);
            var sql = $"select *, case when SState='{StateConst.Filled}' then SDate else BDate end OrderDate from t_pig_more where (BDate>=@SmallDate or SDate>=@SmallDate)";
            if (!string.IsNullOrEmpty(userName))
            {
                sql += $" and UserName='{userName}'";
            }
            sql += " order by OrderDate desc";
            return (await Database.QueryAsync<PigMore>(sql, new { SmallDate = smallDate })).ToList();
        }

        public async Task<List<DogMoreStatisticsDay>> Statistics(string userName)
        {
            var where = "";
            if (!string.IsNullOrEmpty(userName))
            {
                where = $" where UserName='{userName}'";
            }
            var sql = $"select * from (select DATE_FORMAT(BDate,'%Y-%m-%d') BDate, count(1) BCount, sum(BQuantity*BTradeP) BAmount from t_pig_more {where} group by DATE_FORMAT(BDate,'%Y-%m-%d')) b "
                + $"join(select DATE_FORMAT(SDate, '%Y-%m-%d') SDate, count(1) SCount, sum(SQuantity * STradeP) SAmount, sum(SQuantity * STradeP - BQuantity * BTradeP) Earning from t_pig_more {where} group by DATE_FORMAT(SDate, '%Y-%m-%d')) s"
                + " on b.BDate = s.SDate order by b.BDate DESC";
            return (await Database.QueryAsync<DogMoreStatisticsDay>(sql)).ToList();
        }

        public async Task<List<PigMore>> ListBuy(string userName, string name, DateTime begin, DateTime end)
        {
            var smallDate = Utils.GetSmallestOfTheDate(DateTime.Now);
            var bigDate = Utils.GetBiggestOfTheDate(DateTime.Now);
            var sql = $"select * from t_pig_more where UserName=@UserName and Name=@Name and BDate>=@BeginDate and BDate<=@EndDate";
            return (await Database.QueryAsync<PigMore>(sql, new { UserName = userName, Name = name, BeginDate = begin, EndDate = end })).ToList();
        }

        public async Task<List<PigMore>> ListSell(string userName, string name, DateTime begin, DateTime end)
        {
            var smallDate = Utils.GetSmallestOfTheDate(DateTime.Now);
            var bigDate = Utils.GetBiggestOfTheDate(DateTime.Now);
            var sql = $"select * from t_pig_more where UserName=@UserName and SState='{StateConst.Filled}' and Name=@Name and SDate>=@BeginDate and SDate<=@EndDate";
            return (await Database.QueryAsync<PigMore>(sql, new { UserName = userName, Name = name, BeginDate = begin, EndDate = end })).ToList();
        }
    }
}
