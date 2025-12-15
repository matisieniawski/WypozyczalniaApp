using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace WypozyczalniaApp.Pages.Raporty
{

    [Authorize]
    public class PrzychodyModel : PageModel
    {
        public void OnGet()
        {

        }
    }
}