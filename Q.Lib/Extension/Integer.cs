using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib.Extension
{
   public static class Integer
    {
        public static decimal Product(this IEnumerable<decimal> muns)
        {
            decimal result = 0.00m;
            var l_nums = muns.ToList();
            for (int i = 0; i < l_nums.Count; i++)
            {
                if (i == 0)
                {
                    result = l_nums[i];
                }
                else
                {
                    result = result * l_nums[i];
                }
            }
            return result;
        }
    }
}
