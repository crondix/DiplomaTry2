using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using Diploma.Domain.Entities;

namespace Diploma.Domain.Interfaces
{
    public interface IKlasterItemRepository<T> where T : INumber<T>, IMinMaxValue<T>
    {
        public Task<KlasterItem<T>> GetByIdAsync(Guid id);
        public Task<IEnumerable<KlasterItem<T>>> GetAllAsync();
        public Task AddAsync(KlasterItem<T> item);
        public Task UpdateAsync(KlasterItem<T> item);
        public Task DeleteAsync(Guid id);
    }
}
