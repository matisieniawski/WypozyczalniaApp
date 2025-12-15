using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace WypozyczalniaApp.Pages.Account
{
    public class LogoutModel : PageModel
    {
        
        public async Task<IActionResult> OnPostAsync()
        {
           
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

           
            return LocalRedirect("/Account/Login");
        }

      
        public async Task<IActionResult> OnGetAsync()
        {
            return await OnPostAsync();
        }
    }
}