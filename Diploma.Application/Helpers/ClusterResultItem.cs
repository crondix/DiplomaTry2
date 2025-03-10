using System.Numerics;
using Diploma.Application.Interfaces;
using Diploma.Domain.Entities;


namespace Diploma.Application.Helpers
{
    class ClusterResultItem<T>:IClusterResultItem where T : INumber<T>, IMinMaxValue<T>
    {
        public Guid Id { get; private set; }
        private IClusterItem _item { get; set; }
        private int _clusterNumber { get; set; }


        public IClusterItem Item { get { return _item; } }
        public int ClusterNumber { get { return _clusterNumber; } }
        

      public ClusterResultItem(int clusterNumber, ClusterItem<T> item)
        {
            _clusterNumber= clusterNumber;
            _item= item;
            Id= Guid.NewGuid();

        }
    }
}
