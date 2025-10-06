using Microsoft.AspNetCore.Mvc;
using TestWebApp;

namespace Pharmica.AssetGen.Tests.TestWebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetsController : ControllerBase
{
    [HttpGet("logo-path")]
    public IActionResult GetLogoPath()
    {
        // Demonstrates using generated constants in a controller
        return Ok(new { path = StaticAssets.Images.LogoPng });
    }

    [HttpGet("all-images")]
    public IActionResult GetAllImages()
    {
        return Ok(new { logo = StaticAssets.Images.LogoPng, icon = StaticAssets.Images.IconSvg });
    }

    [HttpGet("nested-asset")]
    public IActionResult GetNestedAsset()
    {
        // Demonstrates nested directory access
        return Ok(new { path = StaticAssets.Lib.Bootstrap.BootstrapMinCss });
    }
}
