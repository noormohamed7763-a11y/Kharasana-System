using FluentValidation;
using Kharasana.API.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kharasana.API.Common
{
    public class GlobalValidationActionFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // تخطّي الفلتر لو كان هناك ValidationFilter محدد على الـ Action
            if (context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor &&
                controllerActionDescriptor.MethodInfo.GetCustomAttributes(typeof(ServiceFilterAttribute), true)
                                                 .OfType<ServiceFilterAttribute>()
                                                 .Any(attr => attr.ServiceType.IsGenericType && attr.ServiceType.GetGenericTypeDefinition() == typeof(ValidationFilter<>)))
            {
                await next();
                return;
            }

            if (!context.ModelState.IsValid)
            {
                context.Result = ApiErrorResponseFactory.FromModelState(context.ModelState);
                return;
            }

            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument == null) continue;

                var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
                var validator = context.HttpContext.RequestServices.GetService(validatorType);

                if (validator is IValidator genericValidator)
                {
                    var validationContextType = typeof(ValidationContext<>).MakeGenericType(argument.GetType());
                    var validationContext = Activator.CreateInstance(validationContextType, argument) as IValidationContext;

                    if (validationContext != null)
                    {
                        var result = await (Task<FluentValidation.Results.ValidationResult>)genericValidator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

                        if (!result.IsValid)
                        {
                            foreach (var error in result.Errors)
                            {
                                context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                            }
                            context.Result = ApiErrorResponseFactory.FromModelState(context.ModelState);
                            return;
                        }
                    }
                }
            }
            await next();
        }
    }
}
