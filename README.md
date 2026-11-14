# Armatori Salentini

Sito web ASP.NET Core Razor Pages per le esperienze in barca e il catalogo di imbarcazioni usate di Armatori Salentini.

## Avvio locale

È richiesto il .NET SDK indicato in `WebAppColax.csproj`.

```powershell
dotnet run --urls http://localhost:5174
```

## Moduli di contatto e prenotazione

Entrambi i moduli inviano una email tramite SMTP. La richiesta viene mostrata come ricevuta solo dopo l'invio riuscito; non rappresenta una prenotazione confermata. La disponibilità e la data vengono confermate dallo staff.

Configura questi valori come variabili d'ambiente nell'ambiente di esecuzione, senza inserirli nel repository:

| Variabile | Obbligatoria | Descrizione |
| --- | --- | --- |
| `Contatti__SmtpHost` | Sì | Host del server SMTP |
| `Contatti__SmtpPort` | Sì | Porta SMTP, ad esempio `587` |
| `Contatti__EnableSsl` | No | `true` per TLS (impostazione predefinita) |
| `Contatti__SmtpUser` | No | Utente SMTP; impostarlo insieme alla password |
| `Contatti__SmtpPassword` | No | Password SMTP; impostarla insieme all'utente |
| `Contatti__From` | Sì | Indirizzo mittente autorizzato dal provider |
| `Contatti__To` | Sì | Casella che riceve le richieste |

Se la configurazione manca, i moduli mostrano i contatti telefonici e email alternativi e non dichiarano la richiesta inviata.
