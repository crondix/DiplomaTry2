using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Diploma.Application.Interfaces;

namespace Diploma.Application.Helpers
{
    class ObjectToMatrixConverter : IObjectToMatrixConverter
    {
        double[,] IObjectToMatrixConverter.ObjectsToMatrix<T>(T[] objects, Expression<Func<T, double>>[] propertySelectors)
        {
            throw new NotImplementedException();
        }
    }
}
