using Microsoft.AspNetCore.Mvc;

namespace MES.Web.Controllers;

public sealed class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        ViewData["Title"] = "主選單";
        return View();
    }

    [HttpGet]
    public IActionResult Error()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        ViewData["Title"] = "系統暫時無法處理要求";
        return View();
    }
}
