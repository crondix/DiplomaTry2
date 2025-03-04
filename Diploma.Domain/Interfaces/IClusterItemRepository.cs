using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using Diploma.Domain.Entities;

namespace Diploma.Domain.Interfaces
{
    public interface IClusterItemRepository<T> where T : INumber<T>, IMinMaxValue<T>
    {
        public Task<ClusterItem<T>> GetByIdAsync(Guid id);
        public Task<IEnumerable<ClusterItem<T>>> GetAllAsync();
        public Task AddAsync(ClusterItem<T> item);
        public Task UpdateAsync(ClusterItem<T> item);
        public Task DeleteAsync(Guid id);
    }
}
