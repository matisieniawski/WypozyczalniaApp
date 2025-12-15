using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.Common;

namespace WypozyczalniaApp.Pages.Modele
{
    [Authorize(Policy = "RequireManagerRole")]
    public class DeleteModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        public DeleteModel(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ModelPojazdu ModelPojazdu { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT m.model_id, m.nazwa_modelu, m.cena_za_dobe, p.nazwa 
                    FROM modelepojazdow m
                    JOIN producenci p ON m.producent_id = p.producent_id
                    WHERE m.model_id = @id";

                var param = command.CreateParameter();
                param.ParameterName = "@id";
                param.Value = id;
                command.Parameters.Add(param);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        ModelPojazdu = new ModelPojazdu
                        {
                            ModelId = reader.GetInt32(0),
                            NazwaModelu = reader.GetString(1),
                            CenaZaDobe = reader.GetDecimal(2),
                            Producent = new Producent { Nazwa = reader.GetString(3) }
                        };
                    }
                    else
                    {
                        return NotFound();
                    }
                }
            }

            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM modelepojazdow WHERE model_id = @id";

                    var param = command.CreateParameter();
                    param.ParameterName = "@id";
                    param.Value = id;
                    command.Parameters.Add(param);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected == 0) return NotFound();
                }
            }
            catch (DbException)
            {
                if (connection.State == ConnectionState.Open) await connection.CloseAsync();

                throw;
            }

            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            return RedirectToPage("./Index");
        }
    }
}