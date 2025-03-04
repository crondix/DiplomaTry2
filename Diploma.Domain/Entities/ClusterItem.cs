using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using Diploma.Application.Interfaces;

namespace Diploma.Domain.Entities
{
    public class ClusterItem<T>:IClusterItem<T> where T : INumber<T>, IMinMaxValue<T>
    {
        public Guid Id { get; private set; }
        public object Item { get; set; }
        public T[] Properties { get; set; }
        IEnumerable<object> IClusterItem.Properties => Properties.Cast<object>(); // Приведение типов
        public ClusterItem(object item, T[] properties)
        {
            Id = Guid.NewGuid();
            Item = item;
            Properties = properties;
        }
    }
}
