using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using Diploma.Application.Helpers;
using Diploma.Domain.Entities;

namespace Diploma.Application.Interfaces
{
    public interface IClusterResultRepository
    {
        public Task<IClusterResultItem> GetByIdAsync(Guid id);
        public Task<IEnumerable<IClusterResultItem>> GetAllAsync();
        public Task AddAsync(IClusterResultItem item);
        public Task UpdateAsync(IClusterResultItem item);
        public Task DeleteAsync(Guid id);
    }
}
