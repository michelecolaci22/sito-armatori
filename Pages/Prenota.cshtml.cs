using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebAppColax.Pages;

public class PrenotaModel : PageModel
{
    private readonly IConfiguration _config;
    private readonly ILogger<PrenotaModel> _log;

    public PrenotaModel(IConfiguration config, ILogger<PrenotaModel> log)
    {
        _config = config;
        _log = log;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public string? Website { get; set; }

    [TempData]
    public string? Esito { get; set; }

    [TempData]
    public string? EmailInviata { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Seleziona il tipo di esperienza.")]
        public string TipoEsperienza { get; set; } = "charter"; // charter, grecia, grotte

        public string? YachtSelezionato { get; set; } = "awave"; // awave, baia, cayman, dc11

        [Required(ErrorMessage = "Seleziona la fascia oraria.")]
        public string FasciaOraria { get; set; } = "mattina"; // mattina (10-14), pomeriggio (14:30-18:30), fullday (10-18)

        [Required(ErrorMessage = "Seleziona la data desiderata.")]
        [DataType(DataType.Date)]
        public DateTime? Data { get; set; }

        [Required(ErrorMessage = "Specifica il numero di persone.")]
        [Range(1, 30, ErrorMessage = "Il numero di ospiti deve essere compreso tra 1 e 30.")]
        public int Persone { get; set; } = 2;

        [Required(ErrorMessage = "Inserisci nome e cognome.")]
        [StringLength(100)]
        public string Nome { get; set; } = "";

        [Required(ErrorMessage = "Inserisci un indirizzo email.")]
        [EmailAddress(ErrorMessage = "Inserisci un'email valida.")]
        [StringLength(150)]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Inserisci un recapito telefonico.")]
        [Phone(ErrorMessage = "Numero di telefono non valido.")]
        [StringLength(30)]
        public string Telefono { get; set; } = "";

        [StringLength(1000)]
        public string? Note { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "È necessario accettare l'informativa sulla privacy.")]
        public bool Privacy { get; set; }
    }

    public void OnGet(string? tour, string? yacht, string? slot, int? giorno)
    {
        if (!string.IsNullOrWhiteSpace(tour))
        {
            if (tour.Contains("grecia", StringComparison.OrdinalIgnoreCase))
            {
                Input.TipoEsperienza = "grecia";
                Input.FasciaOraria = "fullday";
            }
            else if (tour.Contains("grotte", StringComparison.OrdinalIgnoreCase))
            {
                Input.TipoEsperienza = "grotte";
            }
            else
            {
                Input.TipoEsperienza = "charter";
            }
        }

        if (!string.IsNullOrWhiteSpace(yacht))
        {
            Input.YachtSelezionato = yacht.ToLowerInvariant();
        }

        if (!string.IsNullOrWhiteSpace(slot))
        {
            Input.FasciaOraria = slot.ToLowerInvariant();
        }

        if (giorno.HasValue && giorno.Value >= 1 && giorno.Value <= 31)
        {
            try
            {
                var year = DateTime.Today.Year;
                var month = DateTime.Today.Month;
                if (giorno.Value < DateTime.Today.Day)
                {
                    month = month == 12 ? 1 : month + 1;
                    if (month == 1) year++;
                }
                Input.Data = new DateTime(year, month, giorno.Value);
            }
            catch
            {
                Input.Data = DateTime.Today.AddDays(1);
            }
        }
        else
        {
            Input.Data = DateTime.Today.AddDays(1);
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!string.IsNullOrEmpty(Website))
        {
            Esito = "ok";
            EmailInviata = Input.Email;
            return RedirectToPage();
        }

        if (Input.Data.HasValue && Input.Data.Value.Date < DateTime.Today)
        {
            ModelState.AddModelError("Input.Data", "La data non può essere nel passato.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var host = _config["Contatti:SmtpHost"];
        var from = _config["Contatti:From"];
        var to = _config["Contatti:To"];

        var nomeEsperienza = Input.TipoEsperienza switch
        {
            "grecia" => "Tour in Grecia (Isole Diapontie)",
            "grotte" => "Tour alle Grotte di Leuca",
            _ => $"Charter Yacht ({Input.YachtSelezionato?.ToUpperInvariant() ?? "Non specificato"})"
        };

        var fasciaTesto = Input.FasciaOraria switch
        {
            "pomeriggio" => "Mezza Giornata Pomeriggio (14:30 – 18:30)",
            "fullday" => "Full Day (10:00 – 18:00)",
            _ => "Mezza Giornata Mattina (10:00 – 14:00)"
        };

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
        {
            _log.LogWarning("Modulo prenotazioni: SMTP non configurato. Simulazione invio riuscita per {Email}", Input.Email);
            Esito = "ok";
            EmailInviata = Input.Email;
            return RedirectToPage();
        }

        try
        {
            var nome = Pulisci(Input.Nome);
            var dataStr = Input.Data?.ToString("dd/MM/yyyy") ?? "Non indicata";

            using var msg = new MailMessage(new MailAddress(from, "Sito Armatori Salentini"), new MailAddress(to))
            {
                Subject = $"[Prenotazione] {nomeEsperienza} – {nome} ({dataStr})",
                Body = $"NUOVA RICHIESTA DI PRENOTAZIONE\n" +
                       $"===================================\n" +
                       $"Esperienza: {nomeEsperienza}\n" +
                       $"Fascia Oraria: {fasciaTesto}\n" +
                       $"Data Richiesta: {dataStr}\n" +
                       $"Numero Ospiti: {Input.Persone}\n\n" +
                       $"DATI CLIENTE:\n" +
                       $"Nome: {nome}\n" +
                       $"Email: {Input.Email}\n" +
                       $"Telefono: {Pulisci(Input.Telefono)}\n\n" +
                       $"NOTE E RICHIESTE:\n" +
                       $"{(string.IsNullOrWhiteSpace(Input.Note) ? "Nessuna nota fornita" : Input.Note)}\n"
            };
            msg.ReplyToList.Add(new MailAddress(Input.Email, nome));

            using var client = new SmtpClient(host, int.TryParse(_config["Contatti:SmtpPort"], out var port) ? port : 587)
            {
                EnableSsl = !string.Equals(_config["Contatti:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase)
            };

            var user = _config["Contatti:SmtpUser"];
            if (!string.IsNullOrWhiteSpace(user))
            {
                client.Credentials = new NetworkCredential(user, _config["Contatti:SmtpPassword"]);
            }

            await client.SendMailAsync(msg);
            Esito = "ok";
            EmailInviata = Input.Email;
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Errore invio email prenotazione");
            Esito = "errore";
            return Page();
        }
    }

    private static string Pulisci(string s) => s.Replace("\r", " ").Replace("\n", " ").Trim();
}
