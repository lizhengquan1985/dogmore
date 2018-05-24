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
    public class DogEmptySellDao : BaseDao
    {
        static ILog logger = LogManager.GetLogger(typeof(DogEmptySellDao));

        public DogEmptySellDao() : base()
        {
        }

        public List<DogEmptySell> GetNeedBuyDogEmptySell(string accountId, string userName, string symbolName)
        {
            var states = GetStateStringIn(new List<string>() { StateConst.PartialCanceled, StateConst.Filled });
            var states2 = GetStateStringIn(new List<string>() { StateConst.PartialCanceled, StateConst.Filled, StateConst.Canceled });
            var sql = $"select * from t_dog_empty_sell where AccountId='{accountId}' and SymbolName = '{symbolName}' and SellState in({states}) and IsFinished=0 " +
                $" and UserName='{userName}' and SellOrderId not in(select SellOrderId from t_dog_empty_buy where AccountId='{accountId}' and UserName='{userName}' and SellState not in({states})) " +
                $" order by SellOrderPrice desc limit 0,5";
            return Database.Query<DogEmptySell>(sql).ToList();
        }
    }
}
