# EF Core Migrations

Use the installed `dotnet-ef` tool to create and apply migrations manually.

## Prerequisites

```powershell
Set-Location D:\VisualStudio\Repos\GenericSoftware

dotnet restore .\EnergyAutomate\EnergyAutomate\EnergyAutomate.csproj
```

## Create a migration

```powershell
dotnet ef migrations add InitialSetup -c ApplicationDbContext -p .\EnergyAutomate\EnergyAutomate\EnergyAutomate.csproj -s .\EnergyAutomate\EnergyAutomate\EnergyAutomate.csproj
```

## Apply migrations to the database

```powershell
dotnet ef database update -c ApplicationDbContext -p .\EnergyAutomate\EnergyAutomate\EnergyAutomate.csproj -s .\EnergyAutomate\EnergyAutomate\EnergyAutomate.csproj
```

## Add another migration later

```powershell
dotnet ef migrations add ChangeTable -c ApplicationDbContext -p .\EnergyAutomate\EnergyAutomate\EnergyAutomate.csproj -s .\EnergyAutomate\EnergyAutomate\EnergyAutomate.csproj
```

## Notes

- `-p` points to the project that contains the `DbContext` and migrations.
- `-s` points to the startup project.
- The correct project name is `EnergyAutomate`.
- Run the commands from the repository root.
