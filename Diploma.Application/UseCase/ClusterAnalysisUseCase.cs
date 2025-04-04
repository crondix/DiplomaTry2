using System.Linq.Expressions;
using System.Numerics;
using System.Threading.Tasks;

using A_Diploma.Domain.Interfaces;
using A_Diploma.Domain.Interfaces.Helpers;
using A_Diploma.Domain.Interfaces.Repository;

using Diploma.Application.Interfaces.ClasterMethods;
using Diploma.Domain.Entities;
using Diploma.Domain.Interfaces;
using Diploma.Domain.Interfaces.ClasterMethods;
using Diploma.Infrastructure.Interfaces.Helpers;

namespace Diploma.Application.UseCase
{
    public class ClusterAnalysisUseCase<T,B> 
    {
        private readonly IClusterAnalyzer<B> _analyzer;
        private readonly IRepository<T> _repository;
        private readonly IToMatrixConverter<T> _converter;
        private readonly IMatrixNormalizer _normolaizer;
        private readonly IPropertySelectors<T> _propetys;
        // Зависимость внедряется через конструктор
        public ClusterAnalysisUseCase(IClusterAnalyzer<B> analyzer, IClusterOptions options, IToMatrixConverter<T> converter, IMatrixNormalizer normolaizer, IRepository<T> repository,  IPropertySelectors<T> propetys)
        {
            _analyzer = analyzer;
            _converter = converter;
            _normolaizer = normolaizer;
            _repository = repository;
            _propetys = propetys;
        }

        public async Task<IEnumerable<B>> Execute()
        {
            IEnumerable<T> data = await _repository.GetAllAsync();
            
            var matrix = _converter.Convert(data.ToArray(), _propetys.propertySelectors);
            var normalaizMatrix = _normolaizer.Normalize(matrix);
            var сlusterizationResult = _analyzer.Execute();

            return сlusterizationResult;

        }

    }
}
