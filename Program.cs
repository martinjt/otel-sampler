using System.Collections.Generic;
using System.Collections.Immutable;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<AlwaysOnSampler>();
builder.Services.AddSingleton(sp => new HealthCheckSampler<AlwaysOnSampler>(100, 
    sp.GetRequiredService<IHttpContextAccessor>(), sp.GetRequiredService<AlwaysOnSampler>()));
builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("otel-sampler-test"))
    .WithTracing(tpb => tpb
        .AddConsoleExporter()
        .AddAspNetCoreInstrumentation());

builder.Services.ConfigureOpenTelemetryTracerProvider((sp, tpb) => 
    tpb.SetSampler(sp.GetRequiredService<HealthCheckSampler<AlwaysOnSampler>>())
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public class HealthCheckSampler<T> : Sampler where T : Sampler
{
    private static Random _random = new();
    private readonly int _keepPercentage;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly T _innerSampler;

    public HealthCheckSampler(int keepPercentage, IHttpContextAccessor contextAccessor, T innerSampler)
    {
        _keepPercentage = keepPercentage;
        _contextAccessor = contextAccessor;
        _innerSampler = innerSampler;
    }

    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {

        if (_contextAccessor.HttpContext?.Request.Path == "/health")
        {
            var shouldSample = _random.Next(1, 100) < _keepPercentage;
            if (shouldSample)
            {
                var samplingAttributes = ImmutableList.CreateBuilder<KeyValuePair<string, object>>();
                samplingAttributes.Add(new("SampleRate", _keepPercentage));

                return new SamplingResult(SamplingDecision.RecordAndSample, samplingAttributes.ToImmutableList());          
            }
                
            return new SamplingResult(SamplingDecision.Drop);
        }

        return _innerSampler.ShouldSample(samplingParameters);
    }
}