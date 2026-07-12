# EnergyAutomate.CodeFactory Project Summary

## Overview
`EnergyAutomate.CodeFactory` is the subsystem responsible for the management and execution of energy calculation and adjustment scripts. It treats these scripts as versioned "Code Templates".

## Key Components
- **Template Management**: Handles the definition (`CodeTemplateDefinition.cs`), default provision (`DefaultCodeTemplateProvider.cs`), and versioning of scripts.
- **Script Execution**: 
    - `RuntimeCodeTemplateExecutor`: Executes the scripts at runtime.
    - `RoslynCodeFactory`: Uses the Roslyn compiler to compile and execute C# scripts dynamically.
- **Script Factories**: Provides specialized factories for different types of scripts (Energy, Distribution, etc.).

## Key Features & Implementation
### Code Template Versioning
- **Persistence**: `CodeTemplate` and `CodeTemplateHistory` tables store versioned scripts with SHA256 hashing to ensure only actual changes are versioned.
- **Workflow**: 
    - **Startup**: `RuntimeCodeTemplateStore` initializes templates from embedded defaults and loads them into an in-memory cache.
    - **Saving**: `DatabaseCodeTemplateStore` handles asynchronous background saving with `ChangeNotes` and user tracking.
    - **Rollback**: Supports jumping to any previous version, creating a new version entry to maintain a full audit trail.
- **Execution**: `RuntimeCodeTemplateExecutor` and `RoslynCodeFactory` allow for dynamic execution of C# scripts.

## Technologies
- .NET 8+
- Roslyn (Microsoft.CodeAnalysis)
- Entity Framework Core

# Code Template Versioning System (From TEMPLATE_VERSIONING)

## Architektur

### Datenbank-Entitäten

#### CodeTemplate
Aktuell aktive Version eines Templates

#### CodeTemplateHistory
Historische Versionen eines Templates

#### ChangeNotes
Änderungsnotizen für ein Template

#### CodeTemplateDefinition
Definition eines Templates

#### DefaultCodeTemplateProvider
Standard-Provider für Templates

#### RuntimeCodeTemplateStore
Store für aktive Templates

#### DatabaseCodeTemplateStore
Store für historische Templates

## Workflow

### Startup
`RuntimeCodeTemplateStore` initialisiert Templates aus eingebundenen Defaults und lädt sie in einen in-Memory Cache.

### Saving
`DatabaseCodeTemplateStore` behandelt asynchrone Hintergrundspeicherung mit `ChangeNotes` und Benutzerverfolgung.

### Rollback
Unterstützt das Springen zu einer früheren Version, indem es eine neue Versionseingabe erstellt, um eine vollständige Audit-Trace zu erhalten.

## Execution
`RuntimeCodeTemplateExecutor` und `RoslynCodeFactory` ermöglichen die dynamische Ausführung von C#-Scripts.
