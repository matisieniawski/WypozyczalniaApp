using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;
using System.Data;
using System.Data.Common;

namespace WypozyczalniaApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WynajemController : ControllerBase
    {
        private readonly WypozyczalniaDbContext _context;

        public WynajemController(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        [HttpPost("rozpocznij")]
        public async Task<IActionResult> RozpocznijWynajem([FromBody] Wynajem wynajem)
        {
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var transaction = await connection.BeginTransactionAsync())
            {
                try
                {
                   
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = (DbTransaction)transaction;
                        command.CommandText = @"
                            INSERT INTO wynajmy (
                                klient_id, pojazd_id, pracownik_id, lokalizacja_odbioru_id, 
                                data_wypozyczenia, data_zwrotu_planowana, koszt_calkowity
                            ) VALUES (
                                @klient, @pojazd, @pracownik, @lokalizacja, 
                                @dataOd, @dataDo, @koszt
                            )
                            RETURNING wynajem_id";

                        var p1 = command.CreateParameter(); p1.ParameterName = "@klient"; p1.Value = wynajem.KlientId; command.Parameters.Add(p1);
                        var p2 = command.CreateParameter(); p2.ParameterName = "@pojazd"; p2.Value = wynajem.PojazdId; command.Parameters.Add(p2);
                        var p3 = command.CreateParameter(); p3.ParameterName = "@pracownik"; p3.Value = wynajem.PracownikId; command.Parameters.Add(p3);
                        var p4 = command.CreateParameter(); p4.ParameterName = "@lokalizacja"; p4.Value = wynajem.LokalizacjaOdbioruId; command.Parameters.Add(p4);

                        var p5 = command.CreateParameter(); p5.ParameterName = "@dataOd";
                        p5.Value = DateTime.SpecifyKind(wynajem.DataWypozyczenia, DateTimeKind.Utc);
                        command.Parameters.Add(p5);

                        var p6 = command.CreateParameter(); p6.ParameterName = "@dataDo";
                        p6.Value = DateTime.SpecifyKind(wynajem.DataZwrotuPlanowana, DateTimeKind.Utc);
                        command.Parameters.Add(p6);

                        var p7 = command.CreateParameter(); p7.ParameterName = "@koszt"; p7.Value = wynajem.KosztCalkowity ?? 0; command.Parameters.Add(p7);

                        var result = await command.ExecuteScalarAsync();
                        if (result != null)
                        {
                            wynajem.WynajemId = (int)result;
                        }
                    }

                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        Message = "Wynajem rozpoczęty pomyślnie.",
                        WynajemId = wynajem.WynajemId
                    });
                }
                catch (PostgresException ex)
                {
                    await transaction.RollbackAsync();


                    if (ex.SqlState == "P0001")
                    {
                        return BadRequest($"Blad walidacji w bazie (TRIGGER): {ex.MessageText}");
                    }

                    if (ex.SqlState == "P0002")
                    {
                        return BadRequest($"Konflikt rezerwacji: {ex.MessageText}");
                    }

                    if (ex.SqlState == "23514")
                    {
                        if (ex.ConstraintName == "check_daty_wynajmu")
                            return BadRequest("Nieprawidłowe daty: Data zwrotu musi być późniejsza niż data wypożyczenia.");

                        return BadRequest($"Naruszenie reguł poprawności danych: {ex.ConstraintName}");
                    }

                    return StatusCode(500, $"Blad bazy danych (SQL State: {ex.SqlState}): {ex.MessageText}");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Wystapil blad krytyczny: {ex.Message}");
                }
            }
        }
    }
}