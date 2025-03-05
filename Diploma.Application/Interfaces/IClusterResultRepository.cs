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
    interface IClusterResultRepository
    {
        public Task<IClusterResult> GetByIdAsync(Guid id);
        public Task<IEnumerable<IClusterResult>> GetAllAsync();
        public Task AddAsync(IClusterResult item);
        public Task UpdateAsync(IClusterResult item);
        public Task DeleteAsync(Guid id);
    }
}
