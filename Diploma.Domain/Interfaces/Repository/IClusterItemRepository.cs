using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using Diploma.Application.Interfaces;
using Diploma.Domain.Entities;

namespace Diploma.Domain.Interfaces
{
    public interface IClusterItemRepository
    {
        public Task<ClusterItem> GetByIdAsync(Guid id);
        public Task<IEnumerable<ClusterItem>> GetAllAsync();
        public Task AddAsync(ClusterItem item);
        public Task UpdateAsync(ClusterItem item);
        public Task DeleteAsync(Guid id);
    }
}
