using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace A_Diploma.Domain.Interfaces.Repository
{
   public interface IRepository<T> 
    {
        public Task<T> GetByIdAsync(Guid id);
        public Task<IEnumerable<T>> GetAllAsync();
        public Task AddAsync(T item);
        public Task UpdateAsync(T item);
        public Task DeleteAsync(Guid id);
    }
}
