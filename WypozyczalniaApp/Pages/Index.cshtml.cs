using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WypozyczalniaApp.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Menu");
            }

            return RedirectToPage("/Account/Login");
        }
    }
}