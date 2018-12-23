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
                    "bts",//fct  dgb   zb start
                    "ae",//xvg    icx atp  link
                    "bcx","bcd",
                });

            historyCoins.Add(new List<string>
                {
                    "btc","xrp","eth","usdt","eos",
                    "xlm","bch","ltc","bsv","trx",
                    "ada","xmr","bnb","xem","dash","iota",
                     "etc","neo","waves","ont","doge",
                    "xtz","ht","zec","vet","btg",

                    "okb","qtum","zrx","dcr","lsk",
                    "xuc","omg","bat","zil",// start
                    "bts","ae","bcd", // xvg
                    "nano", "dgb","bcx",
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
