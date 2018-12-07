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
                    "btc","xrp","eth","usdt","xlm",
                    "bch","bsv","eos","ltc","trx",
                    "ada","xmr", "xem","iota","bnb",
                   "dash","neo","etc","ont", "doge",
                    "ht","zec","vet","btg","xtz",

                   "okb","zrx","xuc","dcr",//waves
                    "qtum","bat","lsk","omg","xrb",
                    "bts",//fct  dgb zil  zb
                    "ae",//xvg    icx atp  link
                    "aoa","maid","bcx","bcn","mkr","bcd",
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
