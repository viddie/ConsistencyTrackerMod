using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.EverestInterop.Models {
    public class GetFileContentResponse : Response {
        public string fileName { get; set; }
        public string fileContent { get; set; }
    }
}
