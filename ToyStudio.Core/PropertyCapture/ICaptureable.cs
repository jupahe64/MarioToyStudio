namespace ToyStudio.Core.PropertyCapture
{
    public interface ICaptureable
    {
        IEnumerable<IPropertyCapture> CaptureProperties();
    }
}
