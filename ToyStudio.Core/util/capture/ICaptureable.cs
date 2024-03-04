using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyStudio.Core.util.capture
{
    public interface ICaptureable
    {
        IEnumerable<IPropertyCapture> CaptureProperties();
    }
}
