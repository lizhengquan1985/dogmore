﻿using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.DateTypes
{
    [Table("t_dog_control")]
    public class DogControl
    {
        [Key]
        public long Id { get; set; }
        public string SymbolName { get; set; }
        /// <summary>
        /// 基础分区，btc，eth，usdt，ht
        /// </summary>
        public string QuoteCurrency { get; set; }
        /// <summary>
        /// 到了这个价可以考虑做空，做空也要阶梯式的做。
        /// </summary>
        public decimal EmptyPrice { get; set; }
        /// <summary>
        /// 最大投入价格
        /// </summary>
        public decimal MaxInputPrice { get; set; }
        /// <summary>
        /// 购买阶梯数， 2%~8%， 一般情况是3%
        /// </summary>
        public decimal LadderBuyPercent { get; set; }
        /// <summary>
        /// 出售阶梯数， 2.5%~9%， 一般情况是3.5%
        /// </summary>
        public decimal LadderSellPercent { get; set; }
        /// <summary>
        /// 每点击一次保持，都会把过去的设置改成false
        /// </summary>
        public bool IsValid { get; set; }
        public DateTime CreateTime { get; set; }

        public decimal HistoryMax { get; set; }
        public decimal HistoryMin { get; set; }
        public int UpIndex { get; set; }
        public int DownIndex { get; set; }
        /// <summary>
        /// 层级,认可度, 0最大, 20 最小, 每降低一级别,是历史的5%
        /// </summary>
        public int SymbolLevel { get; set; }

        public string Memo { get; set; }
    }
}
