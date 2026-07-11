# Code Template Versionierung System

## Überblick

Das System verwaltet C#-Code-Templates für die Automatisierung mit vollständiger Versionierung, Auditierung und Rollback-Fähigkeit.

## Architektur

### Datenbank-Entitäten

#### CodeTemplate
Aktuell aktive Version eines Templates
- `Id`: Primary Key
- `Key`: Eindeutige Bezeichnung (z.B. "calculation.average-power")
- `Code`: Aktueller C#-Code
- `Version`: Aktuelle Versionsnummer
- `CreatedAt`: Erstellungszeitpunkt
- `LastModifiedAt`: Letzter Änderungszeitpunkt
- `LastModifiedBy`: Benutzer der letzten Änderung
- `IsActive`: Aktivierungsstatus

#### CodeTemplateHistory
Komplette Versionierungshistorie
- `Id`: Primary Key
- `CodeTemplateId`: Foreign Key zu CodeTemplate
- `Version`: Versionsnummer
- `Code`: C#-Code dieser Version
- `CreatedAt`: Zeitpunkt der Erstellung
- `CreatedBy`: Benutzer der Erstellung
- `ChangeNotes`: Optionale Änderungsnotizen
- `CodeHash`: SHA256-Hash des Codes (für schnelle Vergleiche)

### Services

#### DatabaseCodeTemplateStore
Verwaltet Persistierung und Versioning:
```csharp
// Template initialisieren
await store.InitializeTemplatesAsync(defaults, cancellationToken);

// Template speichern (neue Version erstellen)
var template = await store.SaveTemplateAsync(key, code, changeNotes, modifiedBy);

// History abrufen
var history = await store.GetHistoryAsync(key);

// Spezifische Version abrufen
var version = await store.GetVersionAsync(key, versionNumber);

// Zu Version rollback
var template = await store.RollbackAsync(key, targetVersion, modifiedBy);
```

#### RuntimeCodeTemplateStore
In-Memory Cache mit DB-Integration:
```csharp
// Initialisiert Templates aus DB beim Startup
await store.InitializeAsync(cancellationToken);

// Alle Templates abrufen (aus Memory)
var templates = store.GetTemplates();

// Template speichern (Memory + DB)
var template = await store.SaveTemplateAsync(key, code, changeNotes, modifiedBy);

// History abrufen
var history = await store.GetHistoryAsync(key);

// Rollback
var template = await store.RollbackAsync(key, targetVersion, modifiedBy);
```

## API Endpoints

### GET /api/codetemplates
Alle Templates abrufen
```json
Response: [
  {
    "key": "calculation.average-power",
    "displayName": "Average Power Calculation",
    "topic": "Calculation",
    "language": "csharp",
    "description": "...",
    "code": "...",
    "defaultCode": "..."
  }
]
```

### GET /api/codetemplates/{key}
Einzelnes Template abrufen

### POST /api/codetemplates/{key}/save
Template speichern (neue Version erstellen)
```json
Body: {
  "code": "...",
  "changeNotes": "Beschreibung der Änderungen"
}
Response: {
  "key": "...",
  "version": 2,
  "lastModifiedAt": "2025-01-15T10:30:00Z",
  "lastModifiedBy": "user@example.com"
}
```

### GET /api/codetemplates/{key}/history
Versionshistory abrufen
```json
Response: [
  {
    "version": 2,
    "createdAt": "2025-01-15T10:30:00Z",
    "createdBy": "user@example.com",
    "changeNotes": "Optimiert für bessere Performance"
  },
  {
    "version": 1,
    "createdAt": "2025-01-01T00:00:00Z",
    "createdBy": "system",
    "changeNotes": "Initial version from embedded template"
  }
]
```

### POST /api/codetemplates/{key}/rollback/{version}
Zu einer bestimmten Version rollback
```
POST /api/codetemplates/calculation.average-power/rollback/1
```

### POST /api/codetemplates/{key}/reset
Auf Standard-Embedded-Version zurücksetzen

## Startup-Workflow

1. **Database Migration**: EF Core migriert neue Tabellen
2. **Template Initialisierung**: 
   - `RuntimeCodeTemplateStore.InitializeAsync()` wird aufgerufen
   - `DatabaseCodeTemplateStore` lädt/erstellt Templates in DB
   - In-Memory Cache wird mit DB-Daten gefüllt
3. **Runtime**: 
   - Templates werden aus Memory-Cache gelesen (schnell)
   - Änderungen werden synchron in Memory und asynchron in DB gespeichert

## Versionierungsprozess

```
User speichert Template v1
         ↓
RuntimeCodeTemplateStore.SaveTemplateAsync()
         ↓
1. Update in-memory Code
2. Berechne Code-Hash (SHA256)
3. Vergleiche mit vorheriger Version
4. Falls unterschiedlich:
   - Speichere neue Version in DB
   - Erstelle CodeTemplateHistory-Eintrag
   - Inkrementiere Version-Nummer
5. Gebe aktualisiertes Template zurück
```

## Vorzüge des Systems

✅ **Versionierung**: Jede Änderung wird mit Metadaten aufgezeichnet
✅ **Auditierung**: Benutzer und Zeitstempel pro Version
✅ **Rollback**: Beliebig zu früheren Versionen springen
✅ **Änderungsnotizen**: Optional Beschreibung der Änderungen
✅ **Performance**: In-Memory Cache für schnellen Zugriff
✅ **Persistierung**: Alle Änderungen in Datenbank gesichert
✅ **Hash-Vergleich**: Nur echte Änderungen werden versioniert
✅ **Embedded Defaults**: Templates sind in Assembly eingebettet als Fallback

## Datenbankqueries

### Alle Versionen eines Templates
```sql
SELECT * FROM CodeTemplateHistories
WHERE CodeTemplateId IN (
  SELECT Id FROM CodeTemplates WHERE Key = 'calculation.average-power'
)
ORDER BY Version DESC
```

### Vergleich zwischen zwei Versionen
```csharp
var v1 = await store.GetVersionAsync(key, 1);
var v2 = await store.GetVersionAsync(key, 2);
// Vergleiche v1.Code mit v2.Code
```

### Änderungen eines Benutzers
```sql
SELECT * FROM CodeTemplateHistories
WHERE CreatedBy = 'user@example.com'
ORDER BY CreatedAt DESC
```

## Sicherheit

- Benutzer-Tracking: Jede Änderung protokolliert wer sie machte
- Audit Trail: Vollständige Historie aller Versionen
- Rollback-Sicherheit: Kann jederzeit zu funktionierender Version zurück
- Embedded Defaults: Fallback wenn DB nicht verfügbar

## Zusammenfassung

Das System bietet:
- ✅ **Persistente Speicherung** von Template-Änderungen
- ✅ **Vollständige Versionierung** mit Geschichte
- ✅ **Rollback-Funktionalität** zu beliebigen Versionen
- ✅ **Audit Trail** mit Benutzer-Tracking
- ✅ **Change Notes** für Dokumentation
- ✅ **Performance** durch In-Memory Caching
- ✅ **Zuverlässigkeit** durch DB-Persistierung
