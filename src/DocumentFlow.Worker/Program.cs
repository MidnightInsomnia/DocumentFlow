using DocumentFlow.Application;
using DocumentFlow.Application.Abstractions.Users;
using DocumentFlow.Application.Documents.Processing.Routing;
using DocumentFlow.Infrastructure;
using DocumentFlow.Worker;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddSingleton<ICurrentUserContext, SystemCurrentUserContext>();
builder.Services.AddInfrastructure(builder.Configuration, includeIdentity: false);
builder.Services.AddOptions<DocumentProcessingWorkerOptions>()
    .Bind(builder.Configuration.GetSection(DocumentProcessingWorkerOptions.SectionName))
    .Validate(
        options => options.PollIntervalSeconds > 0,
        "PollIntervalSeconds must be greater than zero.")
    .Validate(
        options => options.FailureRetryDelaySeconds > 0,
        "FailureRetryDelaySeconds must be greater than zero.")
    .Validate(
        options => options.StaleProcessingTimeoutMinutes > 0,
        "StaleProcessingTimeoutMinutes must be greater than zero.")
    .Validate(
        options => options.RecoveryCheckIntervalSeconds > 0,
        "RecoveryCheckIntervalSeconds must be greater than zero.")
    .Validate(
        options => options.RecoveryBatchSize > 0,
        "RecoveryBatchSize must be greater than zero.")
    .ValidateOnStart();
builder.Services.AddHostedService<DocumentProcessingWorker>();
builder.Services.AddHostedService<WorkerHeartbeatService>();

var host = builder.Build();
_ = host.Services.GetRequiredService<IOptions<RoutingOptions>>().Value;
host.Run();
