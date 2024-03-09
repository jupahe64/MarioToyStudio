using ToyStudio.GUI.widgets;

namespace ToyStudio.GUI.util
{
    internal record struct SharedProperty<TValue>(IEnumerable<TValue> Values,
                    Action<ValueUpdateFunc<TValue>> UpdateAll);

    public delegate void ValueUpdateFunc<TValue>(ref TValue value);
}