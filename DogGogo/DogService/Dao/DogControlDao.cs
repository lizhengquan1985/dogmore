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
            var dogControl = Database.Get<DogControl>(new { SymbolName = symbolName, QuoteCurrency = quoteCurrency });
            if (dogControl == null ||
                dogControl.HistoryMax < dogControl.HistoryMin ||
                dogControl.MaxInputPrice <= 0 ||
                dogControl.EmptyPrice <= 0 ||
                dogControl.HistoryMin <= 0)
            {
                return null;
            }
            return dogControl;
        }

        public DogControl GetDogControlBySet(string symbolName, string quoteCurrency)
        {
            var dogControl = Database.Get<DogControl>(new { SymbolName = symbolName, QuoteCurrency = quoteCurrency });
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

            if (dogControl.MaxInputPrice <= 0
                   || dogControl.EmptyPrice <= 0)
            {
                throw new ApplicationException("管控数据出错");
            }

            var indb = GetDogControlBySet(dogControl.SymbolName, dogControl.QuoteCurrency);
            if (indb != null)
            {
                var emptyPrice = Math.Max(dogControl.EmptyPrice, indb.HistoryMin * (decimal)1.5);
                var maxInputPrice = Math.Min(dogControl.MaxInputPrice, indb.HistoryMax);
                await Database.UpdateAsync<DogControl>(new { EmptyPrice = emptyPrice, MaxInputPrice = maxInputPrice, dogControl.WillDelist }, new { dogControl.SymbolName, dogControl.QuoteCurrency });
            }
            else
            {
                dogControl.CreateTime = DateTime.Now;
                await Database.InsertAsync(dogControl);
            }
        }

        public async Task UpdateDogControlMaxAndMin(DogControl dogControl)
        {
            if (dogControl.QuoteCurrency != "usdt"
                && dogControl.QuoteCurrency != "btc"
                && dogControl.QuoteCurrency != "eth"
                && dogControl.QuoteCurrency != "ht")
            {
                throw new ApplicationException("管控数据QuoteCurrency出错");
            }

            await Database.UpdateAsync<DogControl>(new { dogControl.HistoryMax, dogControl.HistoryMin, dogControl.AvgPrice }, new { dogControl.SymbolName, dogControl.QuoteCurrency });
        }

        public async Task<List<DogControlMemo>> ListDogControl(string quoteCurrency)
        {
            return (await Database.QueryAsync<DogControlMemo>(new { QuoteCurrency = quoteCurrency })).ToList();
        }

        public List<DogControl> ListAllDogControl()
        {
            return (Database.Query<DogControl>(new { })).ToList();
        }

        public async Task SetUnvalid(string symbolName, string quoteCurrency)
        {
            using (var tx = Database.BeginTransaction())
            {
                await Database.UpdateAsync<DogControl>(new { }, new { SymbolName = symbolName, QuoteCurrency = quoteCurrency });
                tx.Commit();
            }
        }

        public async Task<long> GetCount(string quoteCurrency)
        {
            var sql = $"select count(1) from t_dog_control where QuoteCurrency=@QuoteCurrency ";
            return (await Database.QueryAsync<long>(sql, new { QuoteCurrency = quoteCurrency })).FirstOrDefault();
        }

        public async Task DeleteData(string symbolName, string quoteCurrency)
        {
            using (var tx = Database.BeginTransaction())
            {
                var sql = $"delete from t_{quoteCurrency}_{symbolName} where id<{Utils.GetIdByDate(DateTime.Now.AddMinutes(-2880))}";
                await Database.ExecuteAsync(sql);
                tx.Commit();
            }
        }
    }
}
