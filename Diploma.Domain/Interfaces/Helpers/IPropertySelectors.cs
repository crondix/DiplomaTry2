using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Diploma.Domain.Entities;

namespace A_Diploma.Domain.Interfaces.Helpers
{
   public interface IPropertySelectors<T>
    {
        Expression<Func<T, double>>[] propertySelectors { get; set; } 
        

    }
}
