using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Diploma.Infrastructure.Interfaces.Helpers
{
    public interface IToMatrixConverter
    {

        public double[,] Convert();
    }
}
