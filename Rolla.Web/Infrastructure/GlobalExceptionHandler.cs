using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc; // برای ProblemDetails
using Rolla.Domain.Exceptions;

namespace Rolla.Web.Infrastructure;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Error occurred: {Message}", exception.Message);

        // ۱. تعیین کد وضعیت و عنوان بر اساس نوع خطا
        (int statusCode, string title, string detail) = exception switch
        {
            // اگر چیزی پیدا نشد -> 404
            NotFoundException notFound =>
                (StatusCodes.Status404NotFound, "Not Found", notFound.Message),

            // اگر قانون بیزنس نقض شد (مثلاً سفر قبلاً رزرو شده) -> 400 یا 409
            BusinessRuleException business =>
                (StatusCodes.Status400BadRequest, "Business Rule Violation", business.Message),

            // خطاهای دیتابیس (مثلاً Concurrency) -> 409 Conflict
            Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException =>
                (StatusCodes.Status409Conflict, "Concurrency Conflict", "The record was modified by another user."),

            // خطاهای پیش‌بینی نشده -> 500
            _ => (StatusCodes.Status500InternalServerError, "Server Error", "An internal error occurred.")
        };

        // ۲. ساخت پاسخ استاندارد (RFC 7807)
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        // ۳. ارسال پاسخ به کلاینت
        httpContext.Response.StatusCode = statusCode;

        // نکته: اگر درخواست AJAX/API باشد، جیسون برمی‌گردانیم
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true; // یعنی "من خطا را مدیریت کردم، بقیه پایپ‌لاین نگران نباشند"
    }
}