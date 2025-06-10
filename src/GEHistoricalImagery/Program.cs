using GEHistoricalImagery.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. 添加服务到依赖注入容器
builder.Services.AddControllers();

// 将 ImageryService 注册为单例服务。
// 根据你的缓存策略，你可能希望使用 AddScoped 或 AddTransient。
var disableCache = builder.Configuration.GetValue<bool>("NoCache");
builder.Services.AddSingleton(new ImageryService(disableCache));


// 添加 Swagger 用于 API 文档和测试 (可选，但强烈推荐)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 2. 配置 HTTP 请求处理管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// (可选) 如果你准备使用 Nginx 处理 HTTPS，可以注释掉下面这行
// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// 启动应用
app.Run();
