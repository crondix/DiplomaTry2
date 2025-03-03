using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Diploma.Application.Interfaces
{
    interface IObjectToMatrixConverter
    {
        public double[,] ObjectsToMatrix<T>(T[] objects, Expression<Func<T, double>>[] propertySelectors);
    }
}
