using DogService.DateTypes;
using SharpDapper;
using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.Dao
{
    public class DogControlDao : BaseDao
    {
        public DogControl GetDogControl(string symbolName, string quoteCurrency)
        {
            var dogControl = Database.Get<DogControl>(new { SymbolName = symbolName, QuoteCurrency = quoteCurrency, IsValid = true });
            if (dogControl == null ||
                dogControl.HistoryMax < dogControl.HistoryMin ||
                dogControl.MaxInputPrice <= 0 ||
                dogControl.EmptyPrice <= 0 ||
                dogControl.LadderBuyPercent <= 1 ||
                dogControl.LadderSellPercent <= 1 ||
                dogControl.HistoryMin <= 0)
            {
                throw new ApplicationException($"管控数据出错{symbolName}{quoteCurrency}");
            }
            return dogControl;
        }

        public async Task CreateDogControl(DogControl dogControl)
        {
            if (dogControl.QuoteCurrency != "usdt"
                && dogControl.QuoteCurrency != "btc"
                && dogControl.QuoteCurrency != "eth"
                && dogControl.QuoteCurrency != "ht")
            {
                throw new ApplicationException("管控数据QuoteCurrency出错");
            }

            if (dogControl.HistoryMax < dogControl.HistoryMin
                   || dogControl.MaxInputPrice <= 0
                   || dogControl.EmptyPrice <= 0
                   || dogControl.LadderBuyPercent <= 1
                   || dogControl.LadderSellPercent <= 1
                   || dogControl.HistoryMin <= 0)
            {
                throw new ApplicationException("管控数据出错");
            }

            if (dogControl.LadderBuyPercent <= (decimal)1.065)
            {
                dogControl.LadderBuyPercent = (decimal)1.065;
            }
            if (dogControl.LadderSellPercent >= (decimal)1.12)
            {
                dogControl.LadderSellPercent = (decimal)1.12;
            }

            using (var tx = Database.BeginTransaction())
            {
                await Database.UpdateAsync<DogControl>(new { IsValid = false }, new { dogControl.SymbolName, dogControl.QuoteCurrency });
                dogControl.IsValid = true;
                dogControl.CreateTime = DateTime.Now;
                await Database.InsertAsync(dogControl);
                tx.Commit();
            }
        }

        public async Task<List<DogControl>> ListDogControl(string quoteCurrency)
        {
            return (await Database.QueryAsync<DogControl>(new { IsValid = true, QuoteCurrency = quoteCurrency })).ToList();
        }

        public List<DogControl> ListAllDogControl()
        {
            return (Database.Query<DogControl>(new { IsValid = true })).ToList();
        }

        public async Task SetUnvalid(string symbolName, string quoteCurrency)
        {
            using (var tx = Database.BeginTransaction())
            {
                await Database.UpdateAsync<DogControl>(new { IsValid = false }, new { SymbolName = symbolName, QuoteCurrency = quoteCurrency });
                tx.Commit();
            }
        }

        public async Task<long> GetCount(string quoteCurrency)
        {
            var sql = $"select count(1) from t_dog_control where QuoteCurrency=@QuoteCurrency and IsValid={true}";
            return (await Database.QueryAsync<long>(sql, new { QuoteCurrency = quoteCurrency })).FirstOrDefault();
        }

        public async Task DeleteData(string symbolName, string quoteCurrency)
        {
            using (var tx = Database.BeginTransaction())
            {
                var sql = $"delete from t_{quoteCurrency}_{symbolName} where id<{Utils.GetIdByDate(DateTime.Now.AddMinutes(-1000))}";
                await Database.ExecuteAsync(sql);
                tx.Commit();
            }
        }
    }
}
