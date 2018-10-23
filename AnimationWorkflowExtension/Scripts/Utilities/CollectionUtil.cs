using System;
using System.Collections.Generic;

namespace UCCExtensions {
    public class CollectionUtil {
        public static string[] UpdateNames(IEnumerable<string> keys, bool sorted = true) {
            List<string> names = new List<string>();
            foreach (string name in keys) {
                names.Add(name);
            }
            if (sorted) names.Sort();
            return names.ToArray();
        }

        public static U GetOrAdd<T, U>(IDictionary<T, U> dictionary, T name) {
            U group;
            if (!dictionary.TryGetValue(name, out group)) {
                group = (U)typeof(U).GetConstructor(new Type[0]).Invoke(new object[0]);
                dictionary[name] = group;
            }
            return group;
        }

        public static U GetOrAdd<T, U>(Dictionary<T, U> dictionary, T name, U defaultValue) {
            U group;
            if (!dictionary.TryGetValue(name, out group)) {
                group = defaultValue;
                dictionary[name] = group;
            }
            return group;
        }
    }
}
