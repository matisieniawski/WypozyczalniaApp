using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace WypozyczalniaApp.Pages.Raporty
{

    [Authorize]
    public class PopularnoscModel : PageModel
    {
        public void OnGet()
        {

        }
    }
}