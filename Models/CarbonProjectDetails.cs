using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CarbonOffset.Models
{
    public class CarbonProjectDetails
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double CarbonPrice { get; set; }
        public string Currency { get; set; }
        public int Available { get; set; }
    }
}
