using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogApi.Controller
{
    class CoinsPre45
    {
        public static List<string> GetPre40Coins()
        {
            List<List<string>> historyCoins = new List<List<string>>();
            historyCoins.Add(new List<string>
                {
                    "btc","eth","xrp","ltc","eos",
                    "usdt","bch","trx","xlm","bnb",
                    "bsv","ada", "xmr","iota","dash",
                   "neo","etc","xem","ont", "ht",

                    "waves","xtz","vet","zec","okb",
                    "btg","qtum","link","dcr","rep",
                    "xuc","zrx","lsk","zil","bat",
                    "omg","nano","bts","dgb","ae",
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

        public static List<string> GetPre80Coins()
        {
            List<List<string>> historyCoins = new List<List<string>>();
            historyCoins.Add(new List<string>
                {
                    "xvg","icx","btm","strat","steem",
                    "iost","atp","snt","theta","ark",
                    "ardr","etn", "pax","r","ppt",
                   "pivx","lrc","mana","mco", "dgd",

                    "kcs","powr","wtc","wan","bnt",
                    "san","xzc","elf","pay","qash",
                    "poly","qkc","nas","loom","agi",
                    "eng","mgo","zen","gas","gbyte",
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

        public static List<string> GetPre120Coins()
        {
            List<List<string>> historyCoins = new List<List<string>>();
            historyCoins.Add(new List<string>
                {
                    "fun","storj","edr","cvc","safe",
                    "cmt","pai","dent","nxs","nuls",
                    "gvt","salt", "req","mda","rvn",
                   "cwv","bu","inb","kmd", "ft",

                    "dai","gnt","fct","hsr","gxc",
                    "rdd","wax","bhp","enj","tomo",
                    "inve","usdc","nxt","wicc","edo",
                    "dac","rlc","part","knc","edg",
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
