using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;
using FluentValidation.Results;

namespace Elwark.People.Api.Error
{
    public class ElwarkModelStateException : ElwarkException
    {
        public ElwarkModelStateException(IEnumerable<ValidationFailure> failures)
            : base(nameof(CommonError), CommonError.InvalidModelState.ToString("G")) =>
            Failures = new ReadOnlyCollection<ValidationFailure>(failures.ToList());

        public IReadOnlyCollection<ValidationFailure> Failures { get; }
    }
}