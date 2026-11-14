using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppColax.Services;

namespace WebAppColax.Pages;

public class ContattiModel : PageModel
{
    public static readonly string[] Argomenti =
    {
        "Tour in Grecia", "Tour alle grotte", "Charter yacht esclusivo",
        "Barche usate", "Servizi a terra", "Altro"
    };

    private readonly EmailSender _emailSender;

    public ContattiModel(EmailSender emailSender)
    {
        _emailSender = emailSender;
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

        var nome = Pulisci(Input.Nome);
        var body = $"Nome: {nome}\nEmail: {Input.Email}\nTelefono: {Pulisci(Input.Telefono ?? "-")}\nArgomento: {Input.Argomento}\n\n{Input.Messaggio.Trim()}";
        var result = await _emailSender.SendAsync(
            $"[Sito] {Input.Argomento} – {nome}",
            body,
            Input.Email,
            nome,
            HttpContext.RequestAborted);

        if (result == EmailSendResult.Sent)
        {
            Esito = "ok";
            return RedirectToPage();
        }

        Esito = result == EmailSendResult.NotConfigured ? "configurazione" : "errore";
        return Page();
    }

    // Evita a capo nei campi usati nelle intestazioni dell'email
    private static string Pulisci(string s) => s.Replace("\r", " ").Replace("\n", " ").Trim();
}
