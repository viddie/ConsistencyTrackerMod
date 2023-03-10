using Celeste.Mod.ConsistencyTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.EverestInterop.Models.Responses
{
    public class ParseFormatResponse : Response
    {
        public double calculationTime { get; set; }
        public List<string> formats { get; set; }
    }
}
