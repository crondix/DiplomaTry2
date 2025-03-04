using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Diploma.Application.Interfaces
{
    public interface IClusterItem
    {
        Guid Id { get; }
        object Item { get; set; }
        IEnumerable<object> Properties { get; }
    }

    public interface IClusterItem<T> : IClusterItem
        where T : INumber<T>, IMinMaxValue<T>
    {
        new T[] Properties { get; set; }
    }
}
