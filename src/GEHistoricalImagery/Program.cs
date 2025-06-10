using GEHistoricalImagery.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var disableCache = builder.Configuration.GetValue<bool>("NoCache");
builder.Services.AddSingleton(new ImageryService(disableCache));


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
builder.WebHost.UseUrls("http://localhost:35001");

app.Run();
