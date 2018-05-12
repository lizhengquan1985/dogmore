using DogService.DateTypes;
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

        public async Task<List<DogMoreBuy>> ListBuyByBuyOrderId(List<long> buyOrderIds)
        {
            var sql = $"select * from t_dog_more_buy where BuyOrderId in({string.Join(",",buyOrderIds)})";
            return (await Database.QueryAsync<DogMoreBuy>(sql)).ToList();
        }

        public async Task<List<DogMoreBuy>> ListTodayBuy(string userName)
        {
            var smallDate = Utils.GetSmallestOfTheDate(DateTime.Now);
            var sql = $"select * from t_dog_more_buy where UserName='{userName}' and BuyDate>=@SmallDate";
            return (await Database.QueryAsync<DogMoreBuy>(sql, new { SmallDate = smallDate })).ToList();
        }

        public async Task<List<DogMoreSell>> ListTodaySell(string userName)
        {
            var smallDate = Utils.GetSmallestOfTheDate(DateTime.Now);
            var sql = $"select * from t_dog_more_sell where UserName='{userName}' and SellDate>=@SmallDate";
            return (await Database.QueryAsync<DogMoreSell>(sql, new { SmallDate = smallDate })).ToList();
        }

        public async Task<List<DogMoreBuy>> ListBuy(string userName, string symbolName, DateTime begin, DateTime end)
        {
            var smallDate = Utils.GetSmallestOfTheDate(DateTime.Now);
            var bigDate = Utils.GetBiggestOfTheDate(DateTime.Now);
            var sql = $"select * from t_dog_more_buy where UserName=@UserName and SymbolName=@SymbolName and BuyDate>=@BeginDate and BuyDate<=@EndDate";
            return (await Database.QueryAsync<DogMoreBuy>(sql, new { UserName = userName, SymbolName = symbolName, BeginDate = begin, EndDate = end })).ToList();
        }

        public async Task<List<DogMoreSell>> ListSell(string userName, string symbolName, DateTime begin, DateTime end)
        {
            var smallDate = Utils.GetSmallestOfTheDate(DateTime.Now);
            var bigDate = Utils.GetBiggestOfTheDate(DateTime.Now);
            var sql = $"select * from t_dog_more_sell where UserName=@UserName and SellState='{StateConst.Filled}' and SymbolName=@SymbolName and SellDate>=@BeginDate and SellDate<=@EndDate";
            return (await Database.QueryAsync<DogMoreSell>(sql, new { UserName = userName, SymbolName = symbolName, BeginDate = begin, EndDate = end })).ToList();
        }
    }
}
