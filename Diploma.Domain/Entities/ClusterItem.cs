using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using Diploma.Application.Interfaces;

namespace Diploma.Domain.Entities
{
    public class ClusterItem
    {

        private object _item;
        private int _clusterNumber;

        public Guid Id { get; private set; }
        public object Item { get => _item; }
        public int ClusterNumber { get => _clusterNumber; set => _clusterNumber = value; }

        public ClusterItem(int clusterNumber, object item)
        {
            _clusterNumber = clusterNumber;
            _item = item;
            Id = Guid.NewGuid();

        }
    }
}
