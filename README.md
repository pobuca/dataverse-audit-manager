# Dataverse Audit Manager for XrmToolBox

An XrmToolBox plugin for viewing and changing Dataverse auditing at table and column level.

## Features

- Uses the active XrmToolBox connection; no connection string is stored by the plugin
- Displays environment auditing, retention, and user-access auditing status
- Searchable, sortable table grid with an audited-only filter and table-level audit toggles
- Loads table metadata first, then retrieves and caches fields only when a table is selected
- Searchable, sortable column grid with an audited-only filter and column-level audit toggles
- Enable or disable all visible, changeable tables or columns
- Stages changes locally until **Apply & Publish** is selected
- Reviews pending changes in a compact, filterable list with clear enable or disable actions
- Reverts individual pending changes or all pending changes from the review dialog
- Allows reviewed changes to be applied and published directly from the review dialog
- Requires confirmation before applying or reverting all changes
- Batches metadata retrievals and updates, skips no-op updates, publishes affected tables once, and verifies the effective settings afterward
- Prevents editing settings whose managed property cannot be changed and explicitly treats virtual fields as non-auditable
- Reverts pending changes
- Exports the complete effective configuration to JSON, loading any remaining field metadata on demand
- Includes custom 32×32 and 80×80 XrmToolBox branding icons

## Build

The plugin targets .NET Framework 4.8, matching XrmToolBox.

```powershell
dotnet restore --configfile NuGet.Config
dotnet build --configuration Release --no-restore
```

Output:

```text
bin\Release\net48\DataverseAuditManager.XrmToolBox.dll
```

## Install locally

1. Close XrmToolBox.
2. Create the `Plugins` directory beside `XrmToolBox.exe` if it does not exist.
3. Copy `DataverseAuditManager.XrmToolBox.dll` into that directory.
4. Start XrmToolBox and open **Dataverse Audit Manager**.
5. Connect to the desired Dataverse environment and select **Refresh**.

For the currently installed local version:

```powershell
.\Deploy.ps1 -XrmToolBoxPath "$env:USERPROFILE\Documents\XrmToolbox v1.2026.8.75"
```

Local installations are categorized as **Configuration**, **Data**, and **Security**. If XrmToolBox rebuilds its local plugin manifest after an update, close XrmToolBox and restore those categories with:

```powershell
.\Set-PluginCategories.ps1
```

## Safety behavior

Changing a checkbox only stages the change in the UI. Dataverse is not modified until **Apply & Publish** is selected and the confirmation dialog is accepted.

The apply operation uses:

- `ExecuteMultipleRequest` to batch metadata retrievals and updates
- `UpdateEntityRequest` for table auditing
- `UpdateAttributeRequest` for column auditing
- `PublishXmlRequest` for affected tables

After publishing, the plugin re-reads every changed table and field from the published metadata. Any setting Dataverse did not retain is restored to its effective value in the grid and reported as a verification warning.

If an operation fails partway through, refresh the grid to see the server's effective state before retrying.

## Logo assets

Logo source and generated icons are under `assets`:

- `audit-manager-logo-512.png`
- `audit-manager-logo-80.png`
- `audit-manager-logo-32.png`

Run `python assets/generate_logo.py` to regenerate the PNG files and the embedded `LogoResources.cs` constants.

## Development

The project uses the official `XrmToolBoxPackage` and follows the XrmToolBox `PluginBase` / `PluginControlBase` model.

- [XrmToolBox developer documentation](https://www.xrmtoolbox.com/documentation/for-developers/)
- [Debug an XrmToolBox tool](https://www.xrmtoolbox.com/documentation/for-developers/debug/)
- [Deploy a tool to the Tool Library](https://www.xrmtoolbox.com/documentation/for-developers/deploy-your-plugin-in-plugins-store/)
