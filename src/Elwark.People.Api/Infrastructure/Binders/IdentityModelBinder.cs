using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Elwark.People.Api.Infrastructure.Binders
{
    public class IdentityModelBinder<T> : IModelBinder
    {
        private readonly Func<string?, T> _converter;

        public IdentityModelBinder(Func<string?, T> converter) =>
            _converter = converter;

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext is null)
                throw new ArgumentNullException(nameof(bindingContext), nameof(IdentityModelBinder<T>));

            var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

            try
            {
                var result = _converter(value.FirstValue);
                bindingContext.Result = ModelBindingResult.Success(result);
            }
            catch
            {
                bindingContext.Result = ModelBindingResult.Failed();
            }

            return Task.CompletedTask;
        }
    }
}