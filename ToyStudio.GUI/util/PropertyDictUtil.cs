using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.util;

namespace ToyStudio.GUI.util
{
    internal class PropertyDictUtil
    {
        public static bool TryGetSharedPropertyFor<TValue>(SharedProperty<PropertyDict> sharedProp, 
            string key,
            [NotNullWhen(true)] out SharedProperty<TValue>? sharedValueProp)
        {
            foreach (var dict in sharedProp.Values)
            {
                if (!dict.TryGetValue(key, out var value) || value is not TValue)
                {
                    sharedValueProp = null;
                    return false;
                }
            }

            sharedValueProp = new SharedProperty<TValue>(
                Values: sharedProp.Values.Select(x=> x.GetValueOrDefault(key)).OfType<TValue>(),
                UpdateAll: updateFunc =>
                {
                    foreach(var dict in sharedProp.Values)
                    {
                        if (dict.GetValueOrDefault(key) is not TValue value)
                        {
                            Debug.Fail("We already made sure that all values exist and " +
                                "have the correct type, something went wrong");
                            continue;
                        }

                        updateFunc(ref value);
                        dict[key] = value!;
                    }
                }
            );

            return true;
        }
    }
}
