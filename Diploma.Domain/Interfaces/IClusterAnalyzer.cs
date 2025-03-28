using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Diploma.Domain.Entities;

namespace Diploma.Domain.Interfaces
{
    public interface IClusterAnalyzer<T>
    {
        public IEnumerable<T> Execute(params List<object> args);
    }
}
