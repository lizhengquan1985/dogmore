using DogService.DateTypes;
using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.Dao
{
    public class ControlBuyCriticalDao : BaseDao
    {
        public async Task CreateControlBuyCritical(string symbolName, decimal maxCriticalValue, DateTime expiredTime)
        {
            ControlBuyCritical critical = new ControlBuyCritical()
            {
                ExpiredTime = expiredTime,
                IsValid = true,
                SymbolName = symbolName,
                MaxCriticalValue = maxCriticalValue
            };
            using (var tx = Database.BeginTransaction())
            {
                await Database.UpdateAsync<ControlBuyCritical>(new { IsValid = false }, new { SymbolName = symbolName });
                await Database.InsertAsync(critical);
                tx.Commit();
            }
        }

        public async Task<ControlBuyCritical> GetControlBuyCritical(string symbolName)
        {
            return await Database.GetAsync<ControlBuyCritical>(new { SymbolName = symbolName, IsValid = true });
        }
    }
}
