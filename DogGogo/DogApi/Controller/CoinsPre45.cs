using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogApi.Controller
{
    class CoinsPre45
    {
        public static List<string> GetPreCoins()
        {
            List<List<string>> historyCoins = new List<List<string>>();
            historyCoins.Add(new List<string>
                {
                    "btc","xrp","eth","bchabc","bch",
                    "eos","xlm","usdt","ltc","bsv",
                    "xmr","ada","trx","iota","dash",
                    "xem","bnb","ont","neo","etc",
                    "xtz","btg","zec","bcx",

                    "doge","ht","bcn","vet","mkr",
                    "okb","qtum","zrx","dcr",
                    "xuc","bcd","omg","lsk","bat",
                    "aoa","ae","xrb","maid","bts",
                });

            historyCoins.Add(new List<string>
                {
                    "btc","xrp","eth","bchabc","bch",
                    "eos","xlm","usdt","ltc","bsv",
                    "xmr","ada","trx","bcx","iota",
                    "dash","xem","bnb","ont","neo",
                    "etc", "xtz","btg","zec",

                    "doge","ht","vet","mkr","okb",
                    "zrx","qtum","dcr","xuc",
                    "bcd","omg","lsk","bcn","bat",
                    "xrb","aoa","ae","bts", "dgb",
                });

            var set = new HashSet<string>();
            foreach (var item in historyCoins)
            {
                foreach (var str in item)
                {
                    set.Add(str);
                }
            }
            return set.ToList();
        }
    }
}
