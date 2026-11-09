using System.Diagnostics;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FindYourFood.Hosting;

public static class HostingConventions
{
    public static void AddFindYourFoodHosting(this WebApplicationBuilder builder, string serviceName)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);
        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
                context.ProblemDetails.Extensions["traceId"] = GetTraceId(context.HttpContext);
        });

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = 16 * 1024;
        });

        var exportTelemetry = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(traces =>
            {
                traces.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
                if (exportTelemetry)
                {
                    traces.AddOtlpExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
                if (exportTelemetry)
                {
                    metrics.AddOtlpExporter();
                }
            });

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("identity-write", http =>
                RateLimitPartition.GetFixedWindowLimiter(http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1)
                    }));
        });
    }

    public static void UseFindYourFoodHosting(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.Use(async (http, next) =>
        {
            var traceId = GetTraceId(http);
            http.Response.OnStarting(() =>
            {
                http.Response.Headers["X-Correlation-ID"] = traceId;
                return Task.CompletedTask;
            });
            using var scope = app.Logger.BeginScope(new Dictionary<string, object> { ["TraceId"] = traceId });
            await next(http);
        });
    }

    private static string GetTraceId(HttpContext context) =>
        Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
}
