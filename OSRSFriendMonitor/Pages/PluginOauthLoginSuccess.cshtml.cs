using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OSRSFriendMonitor.Pages;

[AllowAnonymous]
public class LoginModel : PageModel
{
    public void OnGet()
    {
    }
}
