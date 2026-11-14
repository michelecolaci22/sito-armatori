using System.Text.Json;

namespace WebAppColax.Services;

public class ContattiBarche
{
    public string Whatsapp { get; set; } = "";   // solo cifre, con prefisso: 393287451542
    public string Telefono { get; set; } = "";
    public string Email { get; set; } = "";
}

/// <summary>Testi e impostazioni valide per tutte le barche (ogni barca può sovrascriverli).</summary>
public class Predefiniti
{
    public ContattiBarche Contatti { get; set; } = new();
    public string Descrizione { get; set; } = "";
    public List<string> CampiScheda { get; set; } = new();
    public List<string> InformazioniUtili { get; set; } = new();
    public string MessaggioWhatsapp { get; set; } = "";
    public string EmailOggetto { get; set; } = "";
    public string EmailCorpo { get; set; } = "";
}

public class Barca
{
    public string Slug { get; set; } = "";       // finisce nell'indirizzo: minuscolo, senza spazi, unico
    public string Nome { get; set; } = "";
    public string Anno { get; set; } = "";
    public string Lunghezza { get; set; } = "";
    public string Prezzo { get; set; } = "Trattativa riservata";
    public bool InVendita { get; set; } = true;
    public List<string> Foto { get; set; } = new();   // la prima è la copertina
    public string? Descrizione { get; set; }          // vuota = testo predefinito
    public Dictionary<string, string> Scheda { get; set; } = new();
    public List<string> Dotazioni { get; set; } = new();
    public List<string>? InformazioniUtili { get; set; } // vuota = testi predefiniti
}

public class Catalogo
{
    public Predefiniti Predefiniti { get; set; } = new();
    public List<Barca> Barche { get; set; } = new();
}

/// <summary>Legge Data/barche.json e lo ricarica da solo quando il file cambia (niente riavvio).</summary>
public class BarcheService
{
    private static readonly JsonSerializerOptions Opt = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly string _path;
    private readonly ILogger<BarcheService> _log;
    private readonly object _lock = new();
    private Catalogo? _cache;
    private DateTime _stamp;

    public BarcheService(IWebHostEnvironment env, ILogger<BarcheService> log)
    {
        _path = Path.Combine(env.ContentRootPath, "Data", "barche.json");
        _log = log;
    }

    public Catalogo Dati
    {
        get
        {
            lock (_lock)
            {
                var stamp = File.Exists(_path) ? File.GetLastWriteTimeUtc(_path) : DateTime.MinValue;
                if (_cache is null || stamp != _stamp)
                {
                    try
                    {
                        _cache = JsonSerializer.Deserialize<Catalogo>(File.ReadAllText(_path), Opt) ?? new Catalogo();
                    }
                    catch (Exception ex)
                    {
                        // File non valido: si mantengono gli ultimi dati buoni finché non viene corretto
                        _log.LogError(ex, "Impossibile leggere {Path}", _path);
                        _cache ??= new Catalogo();
                    }
                    _stamp = stamp;
                }
                return _cache;
            }
        }
    }

    public IEnumerable<Barca> InVendita => Dati.Barche.Where(b => b.InVendita);

    public Barca? Trova(string slug) =>
        Dati.Barche.FirstOrDefault(b => string.Equals(b.Slug, slug, StringComparison.OrdinalIgnoreCase));
}
