using ToyStudio.GUI.widgets;

namespace ToyStudio.GUI.util
{
    internal record struct SharedProperty<TValue>(IEnumerable<TValue> Values,
                    Action<ISectionDrawContext.ValueUpdateFunc<TValue>> UpdateAll);
}