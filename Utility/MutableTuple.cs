using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Utility {
    public class MutableTuple<T1, T2> {
        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }

        public MutableTuple(T1 item1, T2 item2) {
            Item1 = item1;
            Item2 = item2;
        }
    }

    public class MutableKeyValuePair<TKey, TValue> {
        public TKey Key { get; set; }
        public TValue Value { get; set; }
        public MutableKeyValuePair(TKey key, TValue value) {
            Key = key;
            Value = value;
        }

        public override string ToString() {
            return $"{Key} -> {Value}";
        }
    }
}
