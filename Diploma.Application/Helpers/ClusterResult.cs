using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Diploma.Application.Interfaces;
using Diploma.Domain.Entities;


namespace Diploma.Application.Helpers
{
    class ClusterResult<T> : IClusterResult
    {
        public ClusterItem<T> Item { get; set; }
        public int ClusterNumber { get; set; }
    }
}
