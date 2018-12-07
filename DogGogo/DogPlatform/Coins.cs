﻿using DogPlatform.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogPlatform
{
    public class CoinUtils
    {
        private static Dictionary<string, CommonSymbol> usdtCoins = new Dictionary<string, CommonSymbol>();
        private static Dictionary<string, CommonSymbol> btcCoins = new Dictionary<string, CommonSymbol>();
        private static Dictionary<string, CommonSymbol> ethCoins = new Dictionary<string, CommonSymbol>();
        private static Dictionary<string, CommonSymbol> htCoins = new Dictionary<string, CommonSymbol>();
        // 购入时候考虑大于最小交易额度的10%
        private static Dictionary<string, decimal> usdtLeastBuy = new Dictionary<string, decimal> {
            {"btc",(decimal)0.0001 },
            {"xrp",(decimal)1 },
            {"eth",(decimal)0.001 },
            {"bch",(decimal)0.001 },
            {"ltc",(decimal)0.001 },
            {"etc",(decimal)0.01 }
        };
        private static Dictionary<string, decimal> btcLeastBuy = new Dictionary<string, decimal> {
            { "bat",1},
            { "ardr",1},
            {"eth",(decimal)0.001 },
        };
        private static Dictionary<string, decimal> ethLeastBuy = new Dictionary<string, decimal>
        {
            {"xmr",(decimal)0.0001 },
            {"eos",(decimal)0.1 },
            {"omg",(decimal)0.01 },
            {"iota",(decimal)0.01 },
            {"ada",(decimal)0.1 },
            {"steem",(decimal)0.01 },
            {"zrx",(decimal)0.1 },
            {"ont",(decimal)0.01 },
            {"ht",(decimal)0.1 },
            {"btm",(decimal)0.1 },
            {"trx",(decimal)1 },
            {"bts",(decimal)0.1 },
            {"hc",(decimal)0.01 },
            {"icx",(decimal)0.01 },
            {"qtum",(decimal)0.01 },
            {"lsk",(decimal)0.001 },
            {"bat",(decimal)1 },
            {"gnt",(decimal)0.1 },
            {"dcr",(decimal)0.001 },
            {"pai",(decimal)0.1 },
            {"dgb",(decimal)1 },
            {"xlm",(decimal)0.1 },
            {"hit",(decimal)1 },
            {"vet",(decimal)1 },
            {"ae",(decimal)0.01 },
        };
        private static Dictionary<string, decimal> htLeastBuy = new Dictionary<string, decimal>
        {
            {"xrp",(decimal)0.1 },
            {"iost",(decimal)1 },
            {"dash",(decimal)0.0001 },
            {"eos",(decimal)0.01 },
            {"bch",(decimal)0.0001 },
            {"ltc",(decimal)0.001 },
            {"etc",(decimal)0.01 },
            {"mt",(decimal)1 },
            {"kcash",(decimal)1 },
            {"hpt",(decimal)1 },
        };

        private static decimal GetPrecisionValue(int precision)
        {
            if (precision == 0)
            {
                return 1;
            }
            if (precision == 1)
            {
                return (decimal)0.1;
            }
            if (precision == 2)
            {
                return (decimal)0.01;
            }
            if (precision == 3)
            {
                return (decimal)0.001;
            }
            if (precision == 4)
            {
                return (decimal)0.0001;
            }
            Console.WriteLine($"不合理的精度值" + precision);
            return 0;
        }

        public static decimal CalcPreQ(CommonSymbol symbol, decimal quantity)
        {
            string symbolName = symbol.BaseCurrency;
            string quoteCurrency = symbol.QuoteCurrency;
            var leastBy = (decimal)0;
            if (quoteCurrency == "usdt" && usdtLeastBuy.ContainsKey(symbolName))
            {
                leastBy = usdtLeastBuy[symbolName];
            }
            if (quoteCurrency == "btc" && btcLeastBuy.ContainsKey(symbolName))
            {
                leastBy = btcLeastBuy[symbolName];
            }
            if (quoteCurrency == "eth" && ethLeastBuy.ContainsKey(symbolName))
            {
                leastBy = ethLeastBuy[symbolName];
            }
            if (quoteCurrency == "ht" && htLeastBuy.ContainsKey(symbolName))
            {
                leastBy = htLeastBuy[symbolName];
            }
            if(leastBy == 0)
            {
                // 如果没有设置， 则原值返回
                return quantity;
            }
            var amountPrecisionValue = GetPrecisionValue(symbol.AmountPrecision);
            if (leastBy == amountPrecisionValue)
            {
                if(leastBy * 10 > quantity * 2)
                {
                    throw new ApplicationException("----");
                }
                else
                {
                    return leastBy * 10;
                }
            }
            else if (leastBy > amountPrecisionValue)
            {
                if(quantity > leastBy)
                {
                    return quantity;
                }
                else if(quantity * 2 > leastBy)
                {
                    return leastBy;
                }
                else
                {
                    throw new ApplicationException("----");
                }
            }
            else
            {
                throw new ApplicationException("--222--");
            }
        }

        public static bool IsBiggerThenLeastBuyForDoMore(string symbolName, string quoteCurrency, decimal quantity)
        {
            // 最小购买的+精度的
            if (quoteCurrency == "usdt" && usdtLeastBuy.ContainsKey(symbolName))
            {
                var symbol = usdtCoins[symbolName];
                return quantity >= usdtLeastBuy[symbolName] * (decimal)1.1 && quantity >= (usdtLeastBuy[symbolName] + GetPrecisionValue(symbol.AmountPrecision)) * (decimal)1.06 && quantity >= GetPrecisionValue(symbol.AmountPrecision) * 10;
            }
            if (quoteCurrency == "btc" && btcLeastBuy.ContainsKey(symbolName))
            {
                var symbol = btcCoins[symbolName];
                return quantity >= btcLeastBuy[symbolName] * (decimal)1.1 && quantity >= (btcLeastBuy[symbolName] + GetPrecisionValue(symbol.AmountPrecision)) * (decimal)1.06 && quantity >= GetPrecisionValue(symbol.AmountPrecision) * 10;
            }
            if (quoteCurrency == "eth" && ethLeastBuy.ContainsKey(symbolName))
            {
                var symbol = ethCoins[symbolName];
                return quantity >= ethLeastBuy[symbolName] * (decimal)1.1 && quantity >= (ethLeastBuy[symbolName] + GetPrecisionValue(symbol.AmountPrecision)) * (decimal)1.06 && quantity >= GetPrecisionValue(symbol.AmountPrecision) * 10;
            }
            if (quoteCurrency == "ht" && htLeastBuy.ContainsKey(symbolName))
            {
                var symbol = htCoins[symbolName];
                return quantity >= htLeastBuy[symbolName] * (decimal)1.1 && quantity >= (htLeastBuy[symbolName] + GetPrecisionValue(symbol.AmountPrecision)) * (decimal)1.06 && quantity >= GetPrecisionValue(symbol.AmountPrecision) * 10;
            }
            return true;
        }

        public static bool IsBiggerThenLeast(string symbolName, string quoteCurrency, decimal quantity)
        {
            if (quoteCurrency == "usdt" && usdtLeastBuy.ContainsKey(symbolName))
            {
                return quantity >= usdtLeastBuy[symbolName] * (decimal)1.01;
            }
            return true;
        }

        public static decimal GetLeast(string symbolName, string quoteCurrency)
        {
            if (quoteCurrency == "usdt" && usdtLeastBuy.ContainsKey(symbolName))
            {
                return usdtLeastBuy[symbolName] * (decimal)1.005;
            }
            throw new ApplicationException("不知道最小购买额度");
        }

        public static void Init()
        {
            PlatformApi api = PlatformApi.GetInstance("xx"); // 不需要角色,可以随意xx
            var commonSymbols = api.GetCommonSymbols();

            // usdt
            var usdtCommonSymbols = commonSymbols.FindAll(it => it.QuoteCurrency == "usdt");
            Console.WriteLine($"us-dt对数量: {usdtCommonSymbols.Count}");
            foreach (var item in usdtCommonSymbols)
            {
                item.LeastBuyQuantity = 1; // TODO 改进
                if (!usdtCoins.ContainsKey(item.BaseCurrency))
                {
                    usdtCoins.Add(item.BaseCurrency, item);
                }
            }

            // b
            var btcCommonSymbols = commonSymbols.FindAll(it => it.QuoteCurrency == "btc");
            Console.WriteLine($"b-tc对数量: {btcCommonSymbols.Count}");
            foreach (var item in btcCommonSymbols)
            {
                item.LeastBuyQuantity = 1; // TODO 改进
                if (!btcCoins.ContainsKey(item.BaseCurrency))
                {
                    btcCoins.Add(item.BaseCurrency, item);
                }
            }

            // e
            var ethCommonSymbols = commonSymbols.FindAll(it => it.QuoteCurrency == "eth");
            Console.WriteLine($"e-th对数量: {ethCommonSymbols.Count}");
            foreach (var item in ethCommonSymbols)
            {
                item.LeastBuyQuantity = 1; // TODO 改进
                if (!ethCoins.ContainsKey(item.BaseCurrency))
                {
                    ethCoins.Add(item.BaseCurrency, item);
                }
            }

            // ht
            var htCommonSymbols = commonSymbols.FindAll(it => it.QuoteCurrency == "ht");
            Console.WriteLine($"h-t对数量: {htCommonSymbols.Count}");
            foreach (var item in htCommonSymbols)
            {
                item.LeastBuyQuantity = 1; // TODO 改进
                if (!htCoins.ContainsKey(item.BaseCurrency))
                {
                    htCoins.Add(item.BaseCurrency, item);
                }
            }
        }

        public static List<CommonSymbol> GetAllCommonSymbols(string quoteCurrency)
        {
            if (quoteCurrency == "usdt")
            {
                var res = usdtCoins.Values.ToList();
                return res.Where(it => it.BaseCurrency != "ven" && it.BaseCurrency != "btc" && it.BaseCurrency != "hsr").ToList();
            }
            else if (quoteCurrency == "btc")
            {
                var res = btcCoins.Values.ToList();
                var addCoins = new List<string> { "ada", "ae", "ardr", "bat", "bcd", "bch", "bcx", "bsv", "btg", "bts", "dash", "dcr", "eos", "etc", "eth", "ht", "iota", "lsk", "ltc", "neo", "omg", "ont", "qtum", "steem", "trx", "vet", "xem", "xlm", "xmr", "xrp", "zec", "zrx" };
                var addSymbols = res.Where(it => addCoins.Contains(it.BaseCurrency)).ToList();
                return addSymbols;
            }
            else if (quoteCurrency == "eth")
            {
                var res = ethCoins.Values.ToList();
                var addCoins = new List<string> { "ada", "ae", "bat", "btm", "bts", "dcr", "dgb", "eos", "gnt", "hc", "hit", "ht", "icx", "iota", "lsk", "omg", "ont", "pai", "qtum", "steem", "trx", "vet", "xlm", "xmr", "zrx" };
                var addSymbols = res.Where(it => addCoins.Contains(it.BaseCurrency)).ToList();
                return addSymbols;
            }
            else if (quoteCurrency == "ht")
            {
                return htCoins.Values.ToList();
            }
            throw new ApplicationException("error");
        }

        public static CommonSymbol GetCommonSymbol(string baseCurrency, string quoteCurrency)
        {
            var symbols = GetAllCommonSymbols(quoteCurrency);
            var symbol = symbols.Find(it => it.BaseCurrency == baseCurrency);
            return symbol;
        }
    }
}
