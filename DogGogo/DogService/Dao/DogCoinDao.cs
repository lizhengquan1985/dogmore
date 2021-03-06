﻿using DogService.DateTypes;
using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.Dao
{
    public class DogCoinDao : BaseDao
    {
        public void CreateNewDogCoin(string symbolName, int level)
        {
            var dogCoin = Database.Get<DogCoin>(new { SymbolName = symbolName });
            if (dogCoin == null)
            {
                Database.Insert(new DogCoin { SymbolName = symbolName, Level = level });
            }
        }

        public async Task<List<DogCoin>> ListDogCoin()
        {
            var sql = $"select * from t_dog_coin";
            return (await Database.QueryAsync<DogCoin>(new { })).ToList();
        }
    }
}
