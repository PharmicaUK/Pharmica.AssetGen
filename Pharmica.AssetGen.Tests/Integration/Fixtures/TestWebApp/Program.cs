using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestWebApp;

namespace Pharmica.AssetGen.Tests.TestWebApp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseStaticFiles();

        // Endpoint that returns asset paths using the generated class
        app.MapGet(
            "/api/assets/logo",
            () => Results.Ok(new { path = StaticAssets.Images.LogoPng })
        );
        app.MapGet("/api/assets/style", () => Results.Ok(new { path = StaticAssets.Css.StyleCss }));
        app.MapGet(
            "/api/assets/all",
            () =>
                Results.Ok(
                    new
                    {
                        logo = StaticAssets.Images.LogoPng,
                        icon = StaticAssets.Images.IconSvg,
                        style = StaticAssets.Css.StyleCss,
                        script = StaticAssets.Js.AppJs,
                    }
                )
        );

        app.MapControllers();

        app.Run();
    }
}
