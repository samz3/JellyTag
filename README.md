# JellyTag — Jellyfin Tagmanager Plugin

JellyTag is an administrative plugin for Jellyfin that allows you to manage metadata tags for movies and series/shows across your libraries individually and in bulk. It adds an interactive and user-friendly interface to the Jellyfin Administrator Dashboard, making library categorization, grouping, and cleanup extremely quick and efficient.

## Features

- **Dynamic Filtering:** Search for movies by name, filter by specific tags, or target a single media library folder.
- **Inline Editing:** Add and remove tags directly from the movie list with simple, single-click controls and inline text input.
- **Bulk Operations:** Select multiple movies to add, remove, or completely reset tags in a single action.
- **Secured API:** Implements administrative-only routing via ASP.NET Core policies, preventing unauthorized metadata changes.
- **Responsive Theme:** A custom dark-mode design integrated into the native Jellyfin dashboard with a clean visual grid and smooth micro-animations.

---

## Compatibility

JellyTag targets **.NET 8** (`net8.0`), ensuring compatibility with:
- **Jellyfin 10.9.x** (runs on .NET 8)
- **Jellyfin 10.10.x** (runs on .NET 8)
- **Jellyfin 10.11.x and newer** (runs on .NET 9, which loads and runs .NET 8 assemblies out of the box)

---

## Build Instructions

To compile the plugin from source, ensure you have the [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) or newer installed on your machine.

1. Open your terminal in the repository root directory.
2. Run the dotnet build command:
   ```powershell
   dotnet build -c Release
   ```
3. The compiled plugin file `JellyTag.dll` will be generated in:
   ```text
   bin/Release/net8.0/JellyTag.dll
   ```

---

## Manual Installation

1. Navigate to your Jellyfin Server data directory.
   - **Windows (Service):** `C:\ProgramData\Jellyfin\Server\plugins\`
   - **Windows (Portable):** `[YourJellyfinFolder]\plugins\`
   - **Linux / Docker:** `/config/plugins/` (or your mapped volume path)
2. Create a folder named `JellyTag` inside the `plugins` directory.
3. Copy the compiled `JellyTag.dll` file into the `JellyTag` directory:
   ```text
   plugins/
   └── JellyTag/
       └── JellyTag.dll
   ```
4. Restart your Jellyfin server.
5. Log in as an Administrator, open the **Dashboard**, scroll to the **Advanced** section in the left sidebar, and click on **Plugins**.
6. Select **JellyTag** to open the Tagmanager configuration page.

---

## Creating a GitHub Release Repository (Automatic Deployment)

Jellyfin manages plugin updates by parsing a remote JSON catalog. To publish your plugin releases on GitHub so that others (or your own servers) can install it automatically:

### 1. Host the Plugin DLL
Compile the plugin and upload the resulting `JellyTag.dll` to a GitHub Release or host it on a public server.

### 2. Create the Repository Manifest File (`manifest.json`)
Create a public JSON file named `manifest.json` on GitHub Pages or in a public repository:

```json
[
  {
    "guid": "e6d7b481-815d-4d3c-a1a3-c40c522964c3",
    "name": "JellyTag",
    "description": "Manage, filter, edit, and bulk update tags across your movies and series libraries.",
    "overview": "Adds a powerful tag manager to the administrator dashboard.",
    "owner": "samz3",
    "category": "Metadata",
    "versions": [
      {
        "version": "1.0.0",
        "changelog": "Initial release with compact horizontal layout, TV Show support, and floating bulk tag updates toolbar.",
        "targetAbi": "10.10.6.0",
        "sourceUrl": "https://github.com/samz3/JellyTag/releases/download/v1.0.0/JellyTag.dll",
        "checksum": "292E048F6A48E2C498A7D8AFAD056A7F5BE4BD74F4BAD3C0D1BB49BE2E86E71C"
      }
    ]
  }
]
```

### 3. Register your Repository in Jellyfin
1. Go to **Dashboard > Plugins** in your Jellyfin server.
2. Select the **Repositories** tab.
3. Click the **+** button to add a repository:
   - **Name:** JellyTag Repository
   - **URL:** The raw URL to your `manifest.json` file (e.g. `https://raw.githubusercontent.com/samz3/JellyTag/main/manifest.json`).
4. Select the **Catalog** tab. The **JellyTag** plugin will now appear in your catalog, allowing users to install it with a single click.
