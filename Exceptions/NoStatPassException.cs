using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Exceptions {
    public class NoStatPassException : Exception {
        public NoStatPassException() { }
        public NoStatPassException(string message) : base(message) { }
        public NoStatPassException(string message, Exception innerException) : base(message, innerException) { }
    }
}
