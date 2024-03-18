using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyStudio.Core.util
{
    public class Property<TObject, TValue>(Func<TObject, TValue> getter, Action<TObject, TValue> setter)
    {
        public TValue GetValue(TObject obj) => getter.Invoke(obj);
        public void SetValue(TObject obj, TValue value) => setter.Invoke(obj, value);
    }
}
