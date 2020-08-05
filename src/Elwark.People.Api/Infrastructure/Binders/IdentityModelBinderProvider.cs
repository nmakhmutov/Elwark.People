using System;
using System.Collections.Generic;
using Elwark.People.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Elwark.People.Api.Infrastructure.Binders
{
    public class IdentityModelBinderProvider : IModelBinderProvider
    {
        private static readonly IDictionary<Type, IModelBinder> ModelBinders = new Dictionary<Type, IModelBinder>
        {
            {typeof(AccountId), new IdentityModelBinder<AccountId>(AccountId.Parse)},
            {typeof(IdentityId), new IdentityModelBinder<IdentityId>(IdentityId.Parse)}
        };

        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context), nameof(IdentityModelBinderProvider));

            ModelBinders.TryGetValue(context.Metadata.UnderlyingOrModelType, out var result);

            return result;
        }
    }
}