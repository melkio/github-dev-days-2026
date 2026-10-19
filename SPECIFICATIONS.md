# TaskFlow — Specifiche funzionali e tecniche v1.0

**Stato:** specifica per l'implementazione  
**Versione:** 1.0 (baseline del talk *Build your team with GitHub Copilot CLI*)  
**Destinatario:** GitHub Copilot CLI / agente di coding

## 1. Obiettivo e istruzioni per l'implementazione

Implementa da zero il progetto **TaskFlow**, una piccola REST API per gestire le attività di un team. Il risultato deve essere una baseline eseguibile, testata e facilmente comprensibile durante una demo live.

**Vincoli vincolanti:**

1. Implementa **soltanto le funzionalità v1.0** descritte qui. Non anticipare il requisito evolutivo della v1.1.
2. Crea realmente il codice, la soluzione .NET, la migrazione EF Core, i test e la documentazione operativa; non limitarti a produrre una proposta.
3. Mantieni il codice semplice, idiomatico, leggibile e organizzato per funzionalità (*vertical slices*). Evita astrazioni eccessive.
4. Nessun servizio cloud, container, database server o credenziale esterna deve essere necessario per avviare il progetto.
5. Esegui `dotnet restore`, `dotnet build` e `dotnet test`, correggendo gli errori prima di dichiarare completata l'implementazione.
6. Se una decisione tecnica minore non è definita, scegli l'alternativa più semplice, documentala nel README e non ampliare lo scope.
7. **Non creare** custom agents, skill, workflow di orchestrazione o file in `.github/agents/` in questa fase.
8. Non creare commit Git automaticamente.

## 2. Stack tecnologico

| Area | Scelta |
|---|---|
| Linguaggio | C#, con nullable reference types abilitati |
| Runtime | .NET 10, `net10.0` |
| API | ASP.NET Core Minimal APIs |
| Database | SQLite locale |
| Persistenza | Entity Framework Core 10 + provider SQLite |
| Migrazioni | EF Core migrations, incluse nel repository |
| Dependency Injection | Container DI nativo ASP.NET Core |
| Validazione | Validazione esplicita senza framework esterni |
| Logging | `ILogger<T>` e configurazione built-in |
| Documentazione HTTP | OpenAPI integrato, senza Swagger UI aggiuntiva |
| Test | xUnit + `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) |
| Test database | SQLite in-memory, **non** provider EF Core InMemory |
| Strumenti | .NET SDK CLI, Bash, Git |

Dipendenze NuGet minime e nelle rispettive versioni principali 10.x compatibili: `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design` (design-time), `Microsoft.AspNetCore.OpenApi`, `Microsoft.AspNetCore.Mvc.Testing`, xUnit, `Microsoft.NET.Test.Sdk`, runner xUnit. Includere eventuali dipendenze aggiuntive solo se strettamente necessarie e motivarle nel README.

## 3. Ambito funzionale

La v1.0 gestisce due entità: **Task** e **User**.

### 3.1 Task

| Campo | Tipo C# | Regole |
|---|---|---|
| `Id` | `Guid` | Chiave primaria; generato dal server |
| `Title` | `string` | Obbligatorio, trim, lunghezza 1–150 |
| `Description` | `string?` | Facoltativo, max 2000 caratteri; stringa vuota convertita in `null` |
| `Status` | `TaskStatus` | Enum: `Todo`, `InProgress`, `Done`; default `Todo` |
| `CreatedAtUtc` | `DateTimeOffset` | Impostato dal server alla creazione, UTC |
| `UpdatedAtUtc` | `DateTimeOffset` | Impostato dal server a ogni modifica, UTC |

Regole:

- `Id`, `CreatedAtUtc` e `UpdatedAtUtc` non sono modificabili dai client.
- `Status` può essere modificato **solo** dall'endpoint dedicato al cambio di stato.
- Nessun vincolo sulle transizioni di stato in v1.0; qualsiasi passaggio tra i tre stati è ammesso.
- Non utilizzare il tipo `System.Threading.Tasks.Task` come modello di dominio: chiamare l'entità `TaskItem` per evitare ambiguità.
- Serializzare `Status` come **stringa** nelle API JSON e come stringa nel database, non come valore numerico.

### 3.2 User

| Campo | Tipo C# | Regole |
|---|---|---|
| `Id` | `Guid` | Chiave primaria |
| `DisplayName` | `string` | Obbligatorio, lunghezza 1–100 |
| `Email` | `string` | Obbligatoria; univoca senza distinzione tra maiuscole/minuscole |
| `IsActive` | `bool` | Stato anagrafico |

In v1.0 gli utenti sono **di sola lettura**: vengono creati esclusivamente tramite seed. Non implementare operazioni di creazione, modifica o eliminazione utenti. Il flag `IsActive` viene restituito dalle API ma non influenza ancora le Task.

### 3.3 Fuori scope — da NON implementare

- Assegnazione di task a utenti, campo `AssigneeId`, relazioni Task–User.
- Scadenze, campo `DueDate`, filtro per task scadute.
- Notifiche, eventi di dominio o code di messaggistica.
- Autenticazione, autorizzazione, login e ruoli.
- UI web, frontend, paginazione, ordinamenti configurabili e ricerche full-text.
- Docker, Azure, code esterne, SMTP e microservizi.
- Custom agents e orchestrazione Copilot.

Questi elementi devono essere assenti **anche dai modelli e dallo schema del database**: serviranno per l'evoluzione mostrata durante il talk.

## 4. Contratti HTTP

**Base path:** `/api`  
**Convenzioni:** JSON `camelCase`, valori dell'enum come stringhe, timestamp ISO 8601 con offset UTC (`Z`/`+00:00`), GUID canonici. Gli endpoint di modifica restituiscono `204 No Content`, salvo specifiche diverse. Le liste vuote restituiscono `200` con `[]`.

### 4.1 Task endpoints

| Metodo | URL | Scopo | Esito |
|---|---|---|---|
| `GET` | `/api/tasks` | Elenco Task; filtro facoltativo `status` | `200` |
| `GET` | `/api/tasks/{id}` | Dettaglio Task | `200`, `404` |
| `POST` | `/api/tasks` | Crea Task | `201`, `400` |
| `PUT` | `/api/tasks/{id}` | Sostituisce titolo e descrizione | `204`, `400`, `404` |
| `PATCH` | `/api/tasks/{id}/status` | Aggiorna lo stato | `204`, `400`, `404` |
| `DELETE` | `/api/tasks/{id}` | Elimina Task | `204`, `404` |

**GET `/api/tasks`:**

- `?status=Todo`, `?status=InProgress` e `?status=Done` applicano il filtro.
- Il valore del filtro è case-insensitive, ma l'output usa i nomi canonici dell'enum.
- Un valore di filtro non valido restituisce `400` con errore di validazione.
- Senza filtro vengono restituite tutte le Task.
- Ordinamento stabile predefinito: `CreatedAtUtc` decrescente, quindi `Id` crescente. Nessuna paginazione nella v1.0.

**POST `/api/tasks`:**

```json
{
  "title": "Preparare la demo",
  "description": "Verificare i test della baseline"
}
```

Il server imposta `Id`, `Status = Todo`, `CreatedAtUtc` e `UpdatedAtUtc`. Risposta `201 Created`, header `Location: /api/tasks/{id}`, body con la rappresentazione completa della Task. Gli eventuali campi del modello riservati al server non devono essere accettati nel DTO di input.

**PUT `/api/tasks/{id}`:**

```json
{
  "title": "Preparare la demo aggiornata",
  "description": null
}
```

Aggiorna **solo** titolo e descrizione; `title` è obbligatorio e `description` è facoltativa. Non modifica lo stato. Non crea una Task se l'id non esiste. Restituisce `204` in caso di successo.

**PATCH `/api/tasks/{id}/status`:**

```json
{
  "status": "InProgress"
}
```

Accetta soltanto `Todo`, `InProgress`, `Done` (il valore JSON è case-insensitive in input) e restituisce `400` per altri valori. Aggiorna `UpdatedAtUtc` quando lo stato cambia.

**DELETE `/api/tasks/{id}`:** elimina fisicamente la Task, restituisce `204` se presente o `404` altrimenti.

### 4.2 User endpoints

| Metodo | URL | Scopo | Esito |
|---|---|---|---|
| `GET` | `/api/users` | Elenco utenti | `200` |
| `GET` | `/api/users/{id}` | Dettaglio utente | `200`, `404` |

L'elenco utenti è ordinato per `DisplayName` crescente e poi `Id`. Non esistono endpoint di scrittura degli utenti.

### 4.3 Endpoint tecnico

`GET /health` restituisce `200 OK` con:

```json
{
  "status": "Healthy"
}
```

L'endpoint segnala soltanto che l'applicazione è in esecuzione; non serve implementare una piattaforma di monitoraggio.

### 4.4 Errori e validazione

- Per i dati non validi utilizzare `400 Bad Request` e un corpo coerente con `ValidationProblemDetails` (con `errors` per campo).
- Per entità inesistenti utilizzare `404 Not Found` con corpo `ProblemDetails`.
- Per errori imprevisti utilizzare `500 Internal Server Error` con risposta `ProblemDetails`, senza esporre stack trace o dettagli sensibili ai client.
- Non restituire `200 OK` con un oggetto di errore nel body.
- Gestire i GUID malformati con risposta client error (`400`).
- Le stringhe di titolo fatte solo di spazi devono essere rifiutate.

## 5. Architettura e struttura richiesta

Scegliere **un unico progetto web** e **un progetto test**, con organizzazione per funzionalità. Evitare repository generici, unit of work custom, CQRS framework, MediatR e suddivisione in molti assembly.

```text
TaskFlow/
├── .github/
│   └── copilot-instructions.md
├── .config/
│   └── dotnet-tools.json
├── src/
│   └── TaskFlow.Api/
│       ├── Features/
│       │   ├── Tasks/
│       │   │   ├── TaskItem.cs
│       │   │   ├── TaskStatus.cs
│       │   │   ├── TaskDtos.cs
│       │   │   ├── TaskEndpoints.cs
│       │   │   └── TaskService.cs
│       │   └── Users/
│       │       ├── User.cs
│       │       ├── UserDtos.cs
│       │       └── UserEndpoints.cs
│       ├── Persistence/
│       │   ├── AppDbContext.cs
│       │   ├── DbSeeder.cs
│       │   └── Migrations/
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── TaskFlow.Api.csproj
├── tests/
│   └── TaskFlow.IntegrationTests/
│       ├── Infrastructure/
│       │   └── ApiFactory.cs
│       ├── TaskEndpointsTests.cs
│       ├── UserEndpointsTests.cs
│       └── TaskFlow.IntegrationTests.csproj
├── docs/
│   └── architecture.md
├── scripts/
│   ├── reset-db.sh
│   └── smoke-test.sh
├── .gitignore
├── Directory.Build.props
├── TaskFlow.slnx
└── README.md
```

**Responsabilità dei componenti:**

- `Program.cs`: composizione delle dipendenze, pipeline, inizializzazione database e registrazione endpoint. Deve rimanere piccolo.
- `TaskEndpoints.cs`, `UserEndpoints.cs`: mapping degli URL, binding input e conversione in HTTP result; usare gruppi di endpoint quando utile.
- `TaskService.cs`: logica applicativa delle Task e accesso al contesto EF tramite DI; non introdurre interfacce se non servono.
- `TaskDtos.cs`, `UserDtos.cs`: DTO separati dai modelli EF, senza serializzare direttamente entità del database.
- `AppDbContext.cs`: mapping EF, lunghezze, vincoli, enum come stringa, indice univoco per email normalizzata e indici realmente utili.
- `DbSeeder.cs`: inizializzazione riproducibile, idempotente.
- `architecture.md`: sintesi delle scelte e diagramma testuale dei componenti; poche pagine, niente documentazione generata inutilmente.
- `.github/copilot-instructions.md`: breve riepilogo delle convenzioni di codice del repository. **Non** creare file `.agent.md`.

**Qualità del codice:**

- `async/await` per le operazioni I/O, `CancellationToken` propagato alle query EF e alle operazioni asincrone.
- `AsNoTracking()` per le query di sola lettura.
- Usare DI e logging strutturato con `ILogger`, senza stampare informazioni sensibili.
- Non introdurre AutoMapper, FluentValidation, librerie mediator o altri framework non indispensabili.
- Abilitare nullable, implicit usings e warning di compilazione; mantenere i file piccoli e leggibili.
- Utilizzare `TimeProvider` tramite DI per i timestamp creati dall'applicazione, in modo da rendere i test riproducibili.

## 6. SQLite, migrazioni e dati iniziali

### 6.1 Database locale

- Salvare il database di sviluppo in `src/TaskFlow.Api/data/taskflow.db` oppure in un percorso equivalente **risolto in modo indipendente dalla working directory**, rispetto al content root del progetto API.
- Creare automaticamente la cartella `data` se assente.
- Ignorare nel `.gitignore` i file SQLite locali (`.db`, `-wal`, `-shm`) e le directory `bin/`, `obj/`, `TestResults/`.
- In Development, all'avvio applicare le migrazioni EF pendenti e avviare il seed idempotente. Non utilizzare `EnsureCreated()` per il database persistente, per non compromettere le migrazioni future.
- Committare la **migrazione iniziale** nella cartella `Persistence/Migrations/`.
- Includere un tool manifest locale con `dotnet-ef` compatibile con EF Core 10; la migrazione iniziale deve essere già presente, quindi non deve servire eseguire `dotnet ef` a ogni avvio.

### 6.2 Seed iniziale

Creare in modo idempotente:

- **4 utenti** con GUID prefissati: 3 attivi e 1 inattivo.
- **6 Task** con GUID prefissati: 2 `Todo`, 2 `InProgress`, 2 `Done`.
- Titoli e descrizioni realistici per un team software, ad esempio bugfix, refactoring, documentazione, release.
- Timestamp UTC prefissati per i dati seed, per avere liste e risultati stabili durante il talk.

Non associare Task e User. Non introdurre campi di assegnazione.

`reset-db.sh` deve eliminare **solo il database locale di sviluppo conosciuto** (comprese eventuali sidecar SQLite), ricrearlo attraverso le migrazioni ed eseguire il seed. Documentare che non va eseguito mentre l'applicazione è in uso. Lo script deve fallire con errore chiaro se non riesce a completare l'operazione.

## 7. Test automatici

Usare xUnit con `WebApplicationFactory<Program>`, invocando gli endpoint tramite `HttpClient`.

- Ogni test (o test class isolata) deve usare un database SQLite in-memory **indipendente** dagli altri test.
- Mantenere **aperta** la connessione SQLite in-memory per tutta la vita del relativo database di test.
- Sostituire correttamente la registrazione del DbContext nella factory e applicare lo schema tramite le migrazioni anche nei test.
- Non permettere che i test accedano o modifichino il database di sviluppo.
- Usare clock controllabile tramite `TimeProvider` quando si verificano timestamp.
- Test riproducibili, nessuna dipendenza dalla rete o da servizi esterni.

**Scenari minimi obbligatori:**

1. `CreateTask_WithValidData_ReturnsCreatedAndLocation`
2. `CreateTask_WithoutTitle_ReturnsBadRequest`
3. `CreateTask_WithWhitespaceTitle_ReturnsBadRequest`
4. `GetTask_WhenExists_ReturnsTask`
5. `GetTask_WhenMissing_ReturnsNotFound`
6. `ListTasks_WithoutFilter_ReturnsTasksInStableOrder`
7. `ListTasks_WithStatusFilter_ReturnsMatchingTasks`
8. `ListTasks_WithInvalidStatus_ReturnsBadRequest`
9. `UpdateTask_WithValidData_UpdatesTitleAndDescription`
10. `UpdateTask_DoesNotChangeStatus`
11. `UpdateStatus_WithValidStatus_UpdatesTask`
12. `UpdateStatus_WithInvalidStatus_ReturnsBadRequest`
13. `DeleteTask_WhenExists_RemovesTask`
14. `DeleteTask_WhenMissing_ReturnsNotFound`
15. `GetUsers_ReturnsSeededUsersIncludingInactive`
16. `GetUser_WhenMissing_ReturnsNotFound`
17. `Health_ReturnsHealthy`

È possibile distribuire i test in più classi per leggibilità. Tutti devono essere verdi con `dotnet test` dalla root.

## 8. Tooling e documentazione

Il `README.md` deve includere:

1. Scopo e confini funzionali della v1.0.
2. Prerequisiti: .NET SDK 10; nessuna istanza SQLite da installare.
3. Comandi per ripristino pacchetti, build, avvio, test.
4. URL locale d'esempio e percorso del documento OpenAPI (in Development, tipicamente `/openapi/v1.json`).
5. Esempi `curl` per creare una Task, elencarle, cambiare stato e leggere gli utenti.
6. Procedura di reset del database locale.
7. Breve descrizione della struttura del repository e delle convenzioni.
8. Eventuali scelte tecniche minori prese durante l'implementazione.

`scripts/smoke-test.sh` deve verificare almeno `/health`, `/api/tasks`, `/api/users` su un'applicazione **già avviata**, usando `curl` e un base URL configurabile (default `http://localhost:5000`); deve terminare con exit code diverso da zero in caso di errore HTTP. Non avviare automaticamente l'API nello smoke test.

Configurare un URL locale stabile per lo sviluppo (per esempio `http://localhost:5000`) e documentarlo. Gli script devono essere eseguibili su macOS e Linux con Bash, senza dipendenze come `jq`.

## 9. Criteri di accettazione / Definition of Done

L'attività è completata soltanto quando:

- [ ] Esistono i due progetti .NET 10 e il file soluzione `TaskFlow.slnx`.
- [ ] È rispettata la struttura descritta, salvo differenze minime e motivate.
- [ ] Le entità Task e User hanno soltanto i campi richiesti per la v1.0.
- [ ] Tutti gli endpoint richiesti funzionano e rispettano gli status code definiti.
- [ ] Input invalidi ed entità mancanti producono errori HTTP coerenti.
- [ ] Il database SQLite nasce e si popola automaticamente in Development, tramite migration e seed idempotente.
- [ ] La migrazione EF Core iniziale è inclusa nel repository.
- [ ] È possibile cancellare e ricreare il database con `scripts/reset-db.sh`.
- [ ] L'OpenAPI JSON è disponibile in Development.
- [ ] `dotnet restore`, `dotnet build` e `dotnet test` terminano correttamente.
- [ ] I test di integrazione usano un database isolato e non modificano quello locale.
- [ ] `scripts/smoke-test.sh` funziona su un'app già avviata.
- [ ] Il README spiega avvio, test, API e reset.
- [ ] **Non** sono presenti assegnazioni, scadenze, notifiche, autenticazione o custom agents.

## 10. Sequenza operativa richiesta a Copilot

1. Crea soluzione, progetti, configurazioni e directory.
2. Implementa modelli, DbContext, mapping e migrazione iniziale.
3. Implementa seed e inizializzazione del database di sviluppo.
4. Implementa gli endpoint Task, User, Health con DTO e validazioni.
5. Configura OpenAPI e gestione uniforme degli errori.
6. Implementa la factory dei test con SQLite in-memory isolato e la suite di integrazione.
7. Crea script di reset e smoke test, README, breve documento architetturale e istruzioni condivise.
8. Esegui restore, build e test; correggi gli errori trovati.
9. Al termine fornisci un riepilogo di file creati, comandi eseguiti, esito dei test ed eventuali limitazioni residue.

**Importante:** non procedere con la v1.1 in questa iterazione. La baseline verrà usata in un talk per mostrare come un team di agenti AI possa progettare e implementare funzionalità nuove su un progetto esistente.
