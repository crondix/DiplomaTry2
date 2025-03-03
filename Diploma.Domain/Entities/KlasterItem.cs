using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Diploma.Domain.Entities
{
    public class KlasterItem<T> where T : INumber<T>, IMinMaxValue<T>
    {
        public Guid Id { get; private set; }
        public object Item { get; set; }
        public T[] Properties { get; set; }

        public KlasterItem(object item, T[] properties)
        {
            Id = Guid.NewGuid();
            Item = item;
            Properties = properties;
        }
    }
}
