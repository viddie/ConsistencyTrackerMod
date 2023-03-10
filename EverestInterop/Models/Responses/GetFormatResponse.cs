using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.EverestInterop.Models.Responses
{
    public class GetFormatResponse : Response
    {
        public string format { get; set; }
        public string text { get; set; }
    }
}
