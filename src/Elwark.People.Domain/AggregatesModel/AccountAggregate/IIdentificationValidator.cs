using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;

namespace Elwark.People.Domain.AggregatesModel.AccountAggregate
{
    public interface IIdentificationValidator
    {
        Task CheckUniquenessAsync(Identification identification, CancellationToken ct = default);
    }
}