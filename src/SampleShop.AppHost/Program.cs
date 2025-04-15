var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.SampleShop_ApiService>("apiservice");

builder.AddProject<Projects.SampleShop_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
