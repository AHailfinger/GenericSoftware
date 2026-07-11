# Blazor UI für Code Template Verwaltung

## 📋 Überblick

Eine vollständige Blazor-Komponente mit schöner UI für die Verwaltung von Code-Templates mit Versionierung, Editor und Histori-Viewer.

## 🎨 Features der UI

### 1. **Template-Auswahl** (Linke Seitenleiste)
- ✅ Liste aller Templates sortiert nach Topic und DisplayName
- ✅ Aktives Template hervorgehoben
- ✅ Scrollbar für viele Templates
- ✅ Responsive Design

### 2. **Code-Editor Tab** (Editor-Reiter)
- ✅ Syntax-Highlighting für C# (via Textarea mit monospace Font)
- ✅ Zeilennummern implizit durch Textare
- ✅ Große Editor-Fläche (18 Zeilen)
- ✅ Readonly-Anzeige der Programmiersprache
- ✅ Template-Beschreibung angezeigt

### 3. **Änderungsnotizen**
- ✅ Optionales Textfeld für Dokumentation
- ✅ Wird mit gespeichert in der Datenbank
- ✅ Erklärt warum das Template geändert wurde

### 4. **Speicher-Buttons**
- ✅ **Speichern** - Speichert Code mit ChangeNotes
- ✅ **Auf Standard zurücksetzen** - Reverts zu embedded Default
- ✅ Loading-Spinner während des Speicherns
- ✅ Error/Success-Meldungen mit Icons

### 5. **Versionshistorie Tab** (History-Reiter)
- ✅ Alle Versionen angezeigt
- ✅ Version-Badge (Primary + Success für aktuelle Version)
- ✅ Datum und Uhrzeit (DD.MM.YYYY HH:MM:SS)
- ✅ Benutzer der Änderung (CreatedBy)
- ✅ Änderungsnotizen wenn vorhanden
- ✅ **Rollback-Button** für alte Versionen

### 6. **Rollback-Funktionalität**
- ✅ Springt zu beliebiger früherer Version
- ✅ Erstellt neue Version bei Rollback (bleibt nachverfolgbar)
- ✅ Loading-Spinner während des Rollback
- ✅ Nur für nicht-aktuelle Versionen sichtbar
- ✅ Erfolgs-Feedback

### 7. **Benachrichtigungen**
- ✅ **Grüne Success-Alert** mit Icon für erfolgreiche Operationen
- ✅ **Rote Error-Alert** mit Icon bei Fehlern
- ✅ Wird automatisch angezeigt/versteckt
- ✅ Aussagekräftige Meldungen

## 🔧 Technische Implementierung

### Komponenten-Struktur

```
CodeTemplatesManagement.razor
├── Template-Liste (Left Sidebar)
├── Editor-Tab
│   ├── Template-Info
│   ├── Code-Textarea
│   ├── Change-Notes-Textarea
│   ├── Buttons (Save, Reset)
│   └── Alert-Messages
└── History-Tab
    ├── Version-List
    ├── Version-Item (für jede Version)
    │   ├── Version-Badge
    │   ├── Metadata (Datum, User, Notes)
    │   └── Rollback-Button (wenn nicht aktuell)
```

### HTTP-Client (CodeTemplateClient.cs)

```csharp
// HTTP-Calls zur API
GetAllTemplatesAsync()          // GET /api/codetemplates
GetTemplateAsync(key)            // GET /api/codetemplates/{key}
SaveTemplateAsync(key, code)     // POST /api/codetemplates/{key}/save
GetHistoryAsync(key)             // GET /api/codetemplates/{key}/history
RollbackAsync(key, version)      // POST /api/codetemplates/{key}/rollback/{version}
ResetAsync(key)                  // POST /api/codetemplates/{key}/reset
```

### Razor-Directive und State-Management

```csharp
@page "/code-templates"          // Route
@inject CodeTemplateClient       // HTTP-Client
@inject ILogger<...>             // Logging

// State-Variablen
private List<CodeTemplateViewModel>? templates
private CodeTemplateViewModel? selectedTemplate
private List<CodeTemplateHistoryViewModel>? history
private string currentCode
private string changeNotes
private string activeTab = "editor"
private bool isSaving = false
private int? latestVersion = null
```

## 📱 Benutzerflow

### 1. **Seite laden**
```
OnInitializedAsync()
  └─> GetAllTemplatesAsync()
      └─> Templates anzeigen
```

### 2. **Template auswählen**
```
SelectTemplate(template)
  ├─> currentCode = template.CurrentCode
  ├─> activeTab = "editor"
  └─> Alles anderen State reset
```

### 3. **Code editieren und speichern**
```
SaveTemplate()
  ├─> SaveTemplateAsync(key, code, changeNotes)
  ├─> Server speichert mit Versioning
  ├─> Update UI mit neuem Template
  └─> Success-Meldung anzeigen
```

### 4. **Versionshistorie anschauen**
```
SwitchToHistoryTab()
  ├─> LoadHistory()
  │   └─> GetHistoryAsync(key)
  ├─> activeTab = "history"
  └─> Alle Versionen anzeigen
```

### 5. **Zu alter Version zurück**
```
RollbackToVersion(version)
  ├─> RollbackAsync(key, version)
  ├─> Server erstellt neue Version basierend auf alter
  ├─> Update currentCode
  ├─> Reload History
  └─> Success-Meldung "Erfolgreich zu Version X zurück!"
```

## 🎯 Benutzeroberflächen-Highlights

### Icons
- 📝 Templates Verwaltung
- ✏️ Editor-Tab
- 📋 Versionshistorie
- 💾 Speichern
- 🔄 Auf Standard zurücksetzen
- ↩️ Rollback
- 📅 Datum
- ✅ Success-Meldung
- ⚠️ Error-Meldung

### Farben (Bootstrap)
- **Primary (Blau)**: Version-Badges, Rollback-Button
- **Success (Grün)**: Speichern, "AKTUELL"-Badge, Success-Alert
- **Danger (Rot)**: Error-Alert
- **Info (Hell-Blau)**: Loading-Meldungen
- **Warning (Gelb)**: Reset-Button (outline)

### Responsive Design
- **Auf PC**: 3-spaltig (Templates | Editor)
- **Auf Tablet/Mobile**: Stack (Templates über Editor)
- Scrollbare Template-Liste
- Dynamische Textarea-Größe

## 🔗 Navigation

Der Menu-Eintrag wurde hinzugefügt:
```
📝 Templates (Icon: code-slash)
```

Zu erreichen unter `/code-templates`

## 🚀 Verwendung

1. **Zur Seite navigieren**: Klick auf "Templates" im Menü
2. **Template auswählen**: Klick auf ein Template in der Liste
3. **Code editieren**: Im Editor-Textarea ändern
4. **Notizen hinzufügen**: Optional Änderungsnotizen schreiben
5. **Speichern**: Klick auf "💾 Speichern"
6. **History anschauen**: Wechsel zum "📋 Versionshistorie"-Tab
7. **Rollback**: Klick auf "↩️" neben einer älteren Version

## 💡 Best Practices

✅ **Immer Änderungsnotizen schreiben** - Hilft anderen zu verstehen was sich geändert hat  
✅ **Regelmäßig speichern** - Nicht zu viele Änderungen auf einmal  
✅ **History überprüfen** - Vor kritischen Änderungen History anschauen  
✅ **Rollback nutzen** - Wenn etwas schief geht, einfach rollback  
✅ **Standard nutzen** - Bei Unsicherheit "Auf Standard zurücksetzen" verwenden  

## 📚 Weitere Features (optional)

Wenn gewünscht könnten diese Features hinzugefügt werden:
- [ ] Syntax-Highlighting mit Monaco/ACE Editor
- [ ] Diff-Viewer zwischen zwei Versionen
- [ ] Code-Validierung vor Speichern
- [ ] Export/Import von Templates
- [ ] Suche nach Version/Benutzer/Datum
- [ ] Bulk-Reset aller Templates
- [ ] Template-Kommentare
