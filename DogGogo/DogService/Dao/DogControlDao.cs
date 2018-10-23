using DogService.DateTypes;
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
            if (dogControl.HistoryMax < dogControl.HistoryMin
                || dogControl.MaxInputPrice <= 0
                || dogControl.EmptyPrice <= 0
                || dogControl.LadderBuyPercent <= 1
                || dogControl.LadderSellPercent <= 1
                || dogControl.HistoryMin <= 0)
            {
                throw new ApplicationException("管控数据出错");
            }
            return dogControl;
        }

        public async Task CreateDogControl(DogControl dogControl)
        {
            if (dogControl.HistoryMax < dogControl.HistoryMin
                   || dogControl.MaxInputPrice <= 0
                   || dogControl.EmptyPrice <= 0
                   || dogControl.LadderBuyPercent <= 1
                   || dogControl.LadderSellPercent <= 1
                   || dogControl.HistoryMin <= 0)
            {
                throw new ApplicationException("管控数据出错");
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

        public async Task SetUnvalid(string symbolName, string quoteCurrency)
        {
            using (var tx = Database.BeginTransaction())
            {
                await Database.UpdateAsync<DogControl>(new { IsValid = false }, new { SymbolName = symbolName, QuoteCurrency = quoteCurrency });
                tx.Commit();
            }
        }
    }
}
