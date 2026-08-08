# vFrame.Bundler.VFSAdapter

Thin adapter so `vFrame.Bundler` loads AssetBundles from `.vpk` packages (VFS) without extracting to disk first.

## Purpose

Implements `IAssetBundleCreateAdapter` from vFrame.Bundler, backed by `IFileSystemManager` from vFrame.VFS. Scans mounted `IPackageVirtualFileSystem` instances, reads bundle offset via `GetBlockInfo()`, and calls `AssetBundle.LoadFromFile()` with that offset.

## Assemblies & Dependencies

**Runtime only** — no Editor or Test assemblies.

`Runtime/vFrame.Bundler.VFSAdapter.asmdef` references:
- `vFrame.Bundler` — `IAssetBundleCreateAdapter`, `BundleLoadFailedException`
- `vFrame.Core` — `CreateAbility<TInstance, TArg>` lifecycle base
- `vFrame.VFS` — `IFileSystemManager`, `IPackageVirtualFileSystem`, `PathUtils`

Package: `com.vyronlee.vframe.bundler.vfs-adapter` (Unity 2021.3+)

## How the Adapter Wires Bundler → VFS

```csharp
using vFrame.Bundler;
using vFrame.Bundler.VFSAdapter;
using vFrame.VFS;

// 1. Create adapter with IFileSystemManager
var adapter = AssetBundleCreateAdapter.Create(fileSystemManager);

// 2. Inject into BundlerOptions
var options = new BundlerOptions {
    Mode = BundlerMode.AssetBundle,
    AssetBundleCreateAdapter = adapter,
};

// 3. Create Bundler
var bundler = new Bundler(manifest, options);
```

**Loading flow** (`CreateAssetBundle` / `CreateRequest`):
1. Enumerate `_fileSystemManager.GetEnumerator()`
2. Filter for `IPackageVirtualFileSystem` (directory VFS ignored)
3. Call `Exist(path)` to find the bundle
4. Get offset via `GetBlockInfo(path).Offset`
5. `AssetBundle.LoadFromFile(packageFilePath, 0, (ulong)offset)`
6. On failure: raise `OnLoadFailed` event → throw `BundleLoadFailedException`

**Path difference**:
- `CreateAssetBundle(path)` — uses path as-is
- `CreateRequest(path)` — calls `PathUtils.AbsolutePathToRelativeDataPath(path)` first

## Build & Test

**Compilation**: After editing `.cs` files, verify with:
```powershell
dotnet build "D:/Workspace/vFrame/vFrame.Bundler.VFSAdapter/vFrame.Bundler.VFSAdapter.csproj" --no-restore
```
(If `.csproj` missing, regenerate via Unity batch mode or open Unity.)

**No tests** — this package has no test assembly.

## Gotchas

- **Only `IPackageVirtualFileSystem`** — directory-based VFS is skipped during enumeration
- **Mount packages before creating adapter** — otherwise lookup throws `BundleLoadFailedException`
- **Lifecycle** — inherits `CreateAbility`, must call `Destroy()` when done
- **Event handler exceptions** — caught with `Debug.LogException` but adapter still throws
- **Failure semantics** — `LoadFromFile` returning `null` triggers `HandleLoadFailed` then throws

## Cross-Package Conventions

For workspace-level conventions (Unity 2022.3.62f3, multi-package structure, coding standards, compilation verification), see `D:/Workspace/vFrame/CLAUDE.md`.
