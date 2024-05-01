using Microsoft.Extensions.Logging.AzureAppServices;

var builder = WebApplication.CreateBuilder(args);

// Load appsettings.json into the configuration.
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
    builder.Logging.AddConsole();
}
else
{
    builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    builder.Logging.AddAzureWebAppDiagnostics();
    builder.Services.Configure<AzureFileLoggerOptions>(options =>
    {
        options.FileName = "azure-diagnostics-stillwaiting-";
        options.FileSizeLimit = 50 * 1024;
        options.RetainedFileCountLimit = 7;
    });
    builder.Services.Configure<AzureBlobLoggerOptions>(options =>
    {
        options.BlobName = "log.txt";
    });
}


// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.Run();
