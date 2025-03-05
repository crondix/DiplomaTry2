using System.Numerics;
using Diploma.Application.Interfaces;
using Diploma.Domain.Entities;


namespace Diploma.Application.Helpers
{
    class ClusterResult<T>:IClusterResult where T : INumber<T>, IMinMaxValue<T>
    {
        public Guid Id { get; private set; }
        private int _clusterNumber { get; set; }
        private IClusterItem _item { get; set; }

        public int ClusterNumber { get { return _clusterNumber; } }
        public IClusterItem Item { get { return _item; } }

      public ClusterResult(int clusterNumber, ClusterItem<T> item)
        {
            _clusterNumber= clusterNumber;
            _item= item;
            Id= Guid.NewGuid();

        }
    }
}
