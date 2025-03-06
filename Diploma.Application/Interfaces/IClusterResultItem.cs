using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using Diploma.Domain.Entities;

namespace Diploma.Application.Interfaces
{
    interface IClusterResultItem
    {
        public Guid Id { get; }
        public IClusterItem Item { get; }
        public int ClusterNumber { get;  }
    }
}
