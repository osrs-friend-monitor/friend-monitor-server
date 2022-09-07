using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace OSRSFriendMonitor.Pages;

//[Authorize]
public class IndexModel : PageModel
{
    public async void OnGet()
    {
        Console.WriteLine("wow");
    }
}
