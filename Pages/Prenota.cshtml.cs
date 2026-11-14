using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppColax.Services;

namespace WebAppColax.Pages;

public class PrenotaModel : PageModel
{
    private readonly EmailSender _emailSender;

    public PrenotaModel(EmailSender emailSender)
    {
        _emailSender = emailSender;
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
        [RegularExpression("^(charter|grecia|grotte)$", ErrorMessage = "Seleziona un'esperienza valida.")]
        public string TipoEsperienza { get; set; } = "charter"; // charter, grecia, grotte

        [Required(ErrorMessage = "Seleziona uno yacht.")]
        [RegularExpression("^(awave|baia|cayman|dc11)$", ErrorMessage = "Seleziona uno yacht valido.")]
        public string? YachtSelezionato { get; set; } = "awave"; // awave, baia, cayman, dc11

        [Required(ErrorMessage = "Seleziona la fascia oraria.")]
        [RegularExpression("^(mattina|pomeriggio|fullday)$", ErrorMessage = "Seleziona una fascia oraria valida.")]
        public string FasciaOraria { get; set; } = "mattina"; // mattina (10-14), pomeriggio (14:30-18:30), fullday (10-18)

        [Required(ErrorMessage = "Seleziona la data desiderata.")]
        [DataType(DataType.Date)]
        public DateTime? Data { get; set; }

        [Required(ErrorMessage = "Specifica il numero di persone.")]
        [Range(1, 20, ErrorMessage = "Il numero di ospiti deve essere compreso tra 1 e 20.")]
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

    public void OnGet(string? tour, string? yacht, string? slot, DateTime? data, int? persone)
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

        Input.Data = data?.Date;

        if (persone is >= 1 and <= 20)
        {
            Input.Persone = persone.Value;
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

        if (Input.Data.HasValue && Input.Data.Value.Date > DateTime.Today.AddMonths(12))
        {
            ModelState.AddModelError("Input.Data", "Scegli una data entro i prossimi 12 mesi.");
        }

        if (Input.TipoEsperienza == "grecia" && Input.FasciaOraria != "fullday")
        {
            ModelState.AddModelError("Input.FasciaOraria", "Il tour in Grecia prevede la giornata intera.");
        }

        if (Input.TipoEsperienza == "grotte" && Input.FasciaOraria == "fullday")
        {
            ModelState.AddModelError("Input.FasciaOraria", "Per il tour delle grotte scegli la mattina o il pomeriggio.");
        }

        var capienzaMassima = Input.TipoEsperienza switch
        {
            "charter" => Input.YachtSelezionato switch
            {
                "cayman" => 8,
                "awave" or "baia" or "dc11" => 10,
                _ => 0
            },
            "grecia" => 12,
            _ => 20
        };

        if (capienzaMassima > 0 && Input.Persone > capienzaMassima)
        {
            ModelState.AddModelError("Input.Persone", $"Per questa esperienza il numero massimo è {capienzaMassima} ospiti.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var nomeEsperienza = Input.TipoEsperienza switch
        {
            "grecia" => "Tour in Grecia (Isole Diapontie)",
            "grotte" => "Tour alle Grotte di Leuca",
            _ => $"Charter Yacht ({Input.YachtSelezionato?.ToUpperInvariant() ?? "Non specificato"})"
        };

        var fasciaTesto = Input.TipoEsperienza switch
        {
            "grecia" => "Giornata intera (08:30 – 19:30)",
            "grotte" when Input.FasciaOraria == "pomeriggio" => "Pomeriggio (14:30 – 18:00)",
            "grotte" => "Mattina (10:00 – 13:30)",
            _ => Input.FasciaOraria switch
            {
                "pomeriggio" => "Mezza Giornata Pomeriggio (14:30 – 18:30)",
                "fullday" => "Full Day (10:00 – 18:00)",
                _ => "Mezza Giornata Mattina (10:00 – 14:00)"
            }
        };

        var nome = Pulisci(Input.Nome);
        var dataStr = Input.Data?.ToString("dd/MM/yyyy") ?? "Non indicata";
        var body = "NUOVA RICHIESTA DI PRENOTAZIONE\n" +
                   "===================================\n" +
                   $"Esperienza: {nomeEsperienza}\n" +
                   $"Fascia oraria richiesta: {fasciaTesto}\n" +
                   $"Data preferita: {dataStr}\n" +
                   $"Numero ospiti: {Input.Persone}\n\n" +
                   "DATI CLIENTE:\n" +
                   $"Nome: {nome}\n" +
                   $"Email: {Input.Email}\n" +
                   $"Telefono: {Pulisci(Input.Telefono)}\n\n" +
                   "NOTE E RICHIESTE:\n" +
                   $"{(string.IsNullOrWhiteSpace(Input.Note) ? "Nessuna nota fornita" : Input.Note.Trim())}\n";

        var result = await _emailSender.SendAsync(
            $"[Richiesta prenotazione] {nomeEsperienza} – {nome} ({dataStr})",
            body,
            Input.Email,
            nome,
            HttpContext.RequestAborted);

        if (result == EmailSendResult.Sent)
        {
            Esito = "ok";
            EmailInviata = Input.Email;
            return RedirectToPage();
        }

        Esito = result == EmailSendResult.NotConfigured ? "configurazione" : "errore";
        return Page();
    }

    private static string Pulisci(string s) => s.Replace("\r", " ").Replace("\n", " ").Trim();
}
