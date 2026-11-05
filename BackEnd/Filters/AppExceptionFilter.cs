using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Filters
{
    public class AppExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            var (status, message) = context.Exception switch
            {
                ArgumentException => (StatusCodes.Status400BadRequest, context.Exception.Message),
                KeyNotFoundException => (StatusCodes.Status404NotFound, context.Exception.Message),
                DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "اطلاعات هم‌زمان توسط کاربر دیگری تغییر کرده است، دوباره تلاش کنید"),
                _ => (0, string.Empty)
            };

            if (status == 0)
            {
                return;
            }

            context.Result = new ObjectResult(new { message }) { StatusCode = status };
            context.ExceptionHandled = true;
        }
    }
}