using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppColax.Services;

namespace WebAppColax.Pages.BarcheUsate;

public class DettaglioModel : PageModel
{
    private readonly BarcheService _svc;

    public DettaglioModel(BarcheService svc) => _svc = svc;

    public Barca Barca { get; private set; } = new();
    public List<string> Foto { get; private set; } = new();
    public string Descrizione { get; private set; } = "";
    public List<(string Etichetta, string Valore)> Scheda { get; private set; } = new();
    public List<string> Dotazioni { get; private set; } = new();
    public List<string> InfoUtili { get; private set; } = new();
    public List<Barca> AltreBarche { get; private set; } = new();

    public string WhatsAppUrl { get; private set; } = "";
    public string MailtoUrl { get; private set; } = "";
    public string TelefonoTesto { get; private set; } = "";
    public string TelefonoLink { get; private set; } = "";

    public IActionResult OnGet(string slug)
    {
        var b = _svc.Trova(slug);
        if (b is null || !b.InVendita) return NotFound();

        var d = _svc.Dati.Predefiniti;
        Barca = b;
        Foto = b.Foto.Where(f => !string.IsNullOrWhiteSpace(f)).ToList();
        Descrizione = string.IsNullOrWhiteSpace(b.Descrizione) ? d.Descrizione : b.Descrizione!;
        Dotazioni = b.Dotazioni.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        InfoUtili = b.InformazioniUtili is { Count: > 0 } ? b.InformazioniUtili : d.InformazioniUtili;

        // Scheda tecnica: prima Anno e Lunghezza, poi i campi standard, poi eventuali campi extra della barca
        void Aggiungi(string etichetta, string? valore)
        {
            if (!string.IsNullOrWhiteSpace(valore)) Scheda.Add((etichetta, valore!));
        }
        Aggiungi("Anno", b.Anno);
        Aggiungi("Lunghezza", b.Lunghezza);
        foreach (var campo in d.CampiScheda)
            if (b.Scheda.TryGetValue(campo, out var v)) Aggiungi(campo, v);
        foreach (var kv in b.Scheda)
            if (!d.CampiScheda.Contains(kv.Key)) Aggiungi(kv.Key, kv.Value);

        // Messaggi precompilati
        var link = Url.PageLink("/BarcheUsate/Dettaglio", values: new { slug = b.Slug }) ?? "";
        string Riempi(string t) => t
            .Replace("{nome}", b.Nome).Replace("{anno}", b.Anno).Replace("{lunghezza}", b.Lunghezza)
            .Replace("{prezzo}", b.Prezzo).Replace("{link}", link);

        var testoWa = Riempi(d.MessaggioWhatsapp);
        WhatsAppUrl = $"https://wa.me/{d.Contatti.Whatsapp}?text={Uri.EscapeDataString(testoWa)}";

        var oggetto = Riempi(d.EmailOggetto);
        var corpo = Riempi(d.EmailCorpo).Replace("\r", "").Replace("\n", "\r\n");
        MailtoUrl = $"mailto:{d.Contatti.Email}?subject={Uri.EscapeDataString(oggetto)}&body={Uri.EscapeDataString(corpo)}";

        TelefonoTesto = d.Contatti.Telefono;
        TelefonoLink = new string(d.Contatti.Telefono.Where(c => char.IsDigit(c) || c == '+').ToArray());
        AltreBarche = _svc.InVendita.Where(x => x.Slug != b.Slug).Take(3).ToList();
        return Page();
    }
}
