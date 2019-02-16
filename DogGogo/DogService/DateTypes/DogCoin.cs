using SharpDapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogService.DateTypes
{
    [Table("t_dog_coin")]
    public class DogCoin
    {
        [Key]
        public string SymbolName { get; set; }
        public int Level { get; set; }
    }
}
