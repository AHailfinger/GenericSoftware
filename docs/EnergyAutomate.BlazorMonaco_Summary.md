# Blazor UI for Code Template Management (From BLAZOR_UI_GUIDE)

## Overview
`EnergyAutomate.BlazorMonaco` provides the UI components for the code editor and diff viewer within the EnergyAutomate platform. It utilizes the Monaco editor (the engine behind VS Code) to provide a high-quality coding experience for managing energy calculation and adjustment scripts.

## Key Components
- **StandaloneCodeEditor**: A Blazor component that wraps the Monaco editor. It includes:
    - Syntax highlighting for C#
    - Line numbers
    - Large editor area (18 lines)
    - Readonly display for the programming language
    - Template description display
    - Optional text field for documentation (ChangeNotes)
    - Save and Reset buttons with loading spinners and success/error alerts
- **StandaloneDiffEditor**: A Blazor component for comparing two versions of a script side-by-side.
- **Bridge & Helpers**: Provides the necessary glue between Blazor's lifecycle and the Monaco editor's JavaScript API.

## Technologies
- .NET 8+
- Blazor
- Monaco Editor (JavaScript)

## 🎨 Features der UI

### 1. **Template-Auswahl** (Linke Seitenleiste)
- ✅ Liste aller Templates sortiert nach Topic und DisplayName
- ✅ Filterfunktionen (z.B. nach Topic, Name, Beschreibung)
- ✅ Detailansicht für jedes Template
- ✅ Möglichkeit, Templates zu markieren und zu bearbeiten

### 2. **Code-Editor** (Zentrale Ansicht)
- ✅ Großer Code-Editor mit Syntax-Highlighting
- ✅ Zeilennummern
- ✅ Eingabe von Code-Beispielen
- ✅ Automatische Erkennung von Code-Fehlern
- ✅ Möglichkeit, Code zu speichern und zu laden

### 3. **Diff-Viewer** (Zentrale Ansicht)
- ✅ Vergleich zweier Versionen eines Scripts
- ✅ Zeilenweise Anzeige der Unterschiede
- ✅ Möglichkeit, Diff-Viewer zu speichern und zu laden

### 4. **Code-Template-Management** (Zentrale Ansicht)
- ✅ Möglichkeit, Code-Beispiele zu speichern und zu laden
- ✅ Möglichkeit, Code-Beispiele zu bearbeiten
- ✅ Möglichkeit, Code-Beispiele zu markieren
- ✅ Möglichkeit, Code-Beispiele zu löschen

### 5. **Benutzer- und Projektmanagement** (Zentrale Ansicht)
- ✅ Möglichkeit, Benutzer und Projekte zu verwalten
- ✅ Möglichkeit, Benutzer und Projekte zu markieren
- ✅ Möglichkeit, Benutzer und Projekte zu löschen

### 6. **Einstellungen** (Zentrale Ansicht)
- ✅ Möglichkeit, Einstellungen zu verwalten
- ✅ Möglichkeit, Einstellungen zu markieren
- ✅ Möglichkeit, Einstellungen zu löschen

### 7. **Code-Beispiel-Verwaltung** (Zentrale Ansicht)
- ✅ Möglichkeit, Code-Beispiele zu speichern und zu laden
- ✅ Möglichkeit, Code-Beispiele zu bearbeiten
- ✅ Möglichkeit, Code-Beispiele zu markieren
- ✅ Möglichkeit, Code-Beispiele zu löschen

### 8. **Code-Editor-Verwaltung** (Zentrale Ansicht)
- ✅ Möglichkeit, Code-Editor zu speichern und zu laden
- ✅ Möglichkeit, Code-Editor zu bearbeiten
- ✅ Möglichkeit, Code-Editor zu markieren
- ✅ Möglichkeit, Code-Editor zu löschen

### 9. **Code-Template-Verwaltung** (Zentrale Ansicht)
- ✅ Möglichkeit, Code-Template zu speichern und zu laden
- ✅ Möglichkeit, Code-Template zu bearbeiten
- ✅ Möglichkeit, Code-Template zu markieren
- ✅ Möglichkeit, Code-Template zu löschen

### 10. **Code-Beispiel-Verwaltung** (Zentrale Ansicht)
- ✅ Möglichkeit, Code-Beispiele zu speichern und zu laden
- ✅ Möglichkeit, Code-Beispiele zu bearbeiten
- ✅ Möglichkeit, Code-Beispiele zu markieren
- ✅ Möglichkeit, Code-Beispiele zu löschen
