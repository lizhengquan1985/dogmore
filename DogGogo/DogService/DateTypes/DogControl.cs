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
        public decimal PredictPrice { get; set; }
        public DateTime PredictExpiredTime { get; set; }
        public decimal EmptyPrice { get; set; }
        public DateTime EmptyExpiredTime { get; set; }
        public decimal AvgInputAmount { get; set; }
        public DateTime AvgInputExpiredTime { get; set; }
        public decimal MaxInputPrice { get; set; }
        public DateTime MaxInputPriceExpiredTime { get; set; }
        public bool IsValid { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
