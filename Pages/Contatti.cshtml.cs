using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebAppColax.Pages;

public class ContattiModel : PageModel
{
    public static readonly string[] Argomenti =
    {
        "Tour in Grecia", "Tour alle grotte", "Charter yacht esclusivo",
        "Barche usate", "Servizi a terra", "Altro"
    };

    private readonly IConfiguration _config;
    private readonly ILogger<ContattiModel> _log;

    public ContattiModel(IConfiguration config, ILogger<ContattiModel> log)
    {
        _config = config;
        _log = log;
    }

    [BindProperty] public InputModel Input { get; set; } = new();

    // Campo trappola per i bot: gli utenti reali non lo vedono né lo compilano
    [BindProperty] public string? Website { get; set; }

    [TempData] public string? Esito { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Inserisci il tuo nome.")]
        [StringLength(100)]
        public string Nome { get; set; } = "";

        [Required(ErrorMessage = "Inserisci la tua email.")]
        [EmailAddress(ErrorMessage = "L'indirizzo email non sembra valido.")]
        [StringLength(150)]
        public string Email { get; set; } = "";

        [Phone(ErrorMessage = "Il numero di telefono non sembra valido.")]
        [StringLength(30)]
        public string? Telefono { get; set; }

        [Required(ErrorMessage = "Scegli un argomento.")]
        public string Argomento { get; set; } = "";

        [Required(ErrorMessage = "Scrivi il tuo messaggio.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Il messaggio deve avere almeno 10 caratteri.")]
        public string Messaggio { get; set; } = "";

        [Range(typeof(bool), "true", "true", ErrorMessage = "Per inviare il messaggio devi accettare la privacy policy.")]
        public bool Privacy { get; set; }
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        // Bot: rispondiamo come se fosse andato tutto bene, senza inviare nulla
        if (!string.IsNullOrEmpty(Website))
        {
            Esito = "ok";
            return RedirectToPage();
        }

        if (!Argomenti.Contains(Input.Argomento))
            ModelState.AddModelError("Input.Argomento", "Scegli un argomento dall'elenco.");

        if (!ModelState.IsValid) return Page();

        var host = _config["Contatti:SmtpHost"];
        var from = _config["Contatti:From"];
        var to = _config["Contatti:To"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
        {
            _log.LogWarning("Modulo contatti: SMTP non configurato (sezione 'Contatti' in appsettings).");
            Esito = "errore";
            return Page();
        }

        try
        {
            var nome = Pulisci(Input.Nome);
            using var msg = new MailMessage(new MailAddress(from, "Sito Armatori Salentini"), new MailAddress(to))
            {
                Subject = $"[Sito] {Input.Argomento} – {nome}",
                Body = $"Nome: {nome}\nEmail: {Input.Email}\nTelefono: {Pulisci(Input.Telefono ?? "-")}\nArgomento: {Input.Argomento}\n\n{Input.Messaggio}"
            };
            msg.ReplyToList.Add(new MailAddress(Input.Email, nome));

            using var client = new SmtpClient(host, int.TryParse(_config["Contatti:SmtpPort"], out var port) ? port : 587)
            {
                EnableSsl = !string.Equals(_config["Contatti:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase)
            };
            var user = _config["Contatti:SmtpUser"];
            if (!string.IsNullOrWhiteSpace(user))
                client.Credentials = new NetworkCredential(user, _config["Contatti:SmtpPassword"]);

            await client.SendMailAsync(msg);
            Esito = "ok";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Modulo contatti: invio email non riuscito.");
            Esito = "errore";
            return Page();
        }
    }

    // Evita a capo nei campi usati nelle intestazioni dell'email
    private static string Pulisci(string s) => s.Replace("\r", " ").Replace("\n", " ").Trim();
}
