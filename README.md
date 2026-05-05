[English](README.md) | [简体中文](README.zh-CN.md)

# vFrame Bundler Virtual File System Adapter

`vFrame.Bundler.VFSAdapter` bridges `vFrame.Bundler` and `vFrame.VFS` so Bundler can load AssetBundles directly from `.vpk` package files without extracting them to disk first.

## Features

- Implements `IAssetBundleCreateAdapter` and plugs directly into `BundlerOptions.AssetBundleCreateAdapter`
- Uses `AssetBundle.LoadFromFile` with a file offset to read bundles from inside `.vpk` files
- Supports both synchronous loading through `CreateAssetBundle` and asynchronous loading through `CreateRequest`
- Scans only file systems that implement `IPackageVirtualFileSystem`
- Exposes `OnLoadFailed` for logging the package path and bundle path on failures
- Throws `BundleLoadFailedException` when the bundle cannot be found or Unity fails to open it
- Keeps the runtime surface small with explicit dependencies on `vFrame.Bundler`, `vFrame.Core`, and `vFrame.VFS`

## Requirements

- Repository Unity version: `2022.3.62f3`
- Package minimum Unity version in `package.json`: `2021.3`
- Install and configure these packages before using the adapter:
  - `com.vyronlee.vframe.bundler`
  - `com.vyronlee.vframe.vfs`
- Runtime assembly references:
  - `vFrame.Bundler`
  - `vFrame.Core`
  - `vFrame.VFS`

## Installation

### Install with UPM Git URLs

In Unity, open `Window > Package Manager > Add package from git URL...` and add these dependencies:

```text
https://github.com/VyronLee/vFrame.Bundler.git#upm
https://github.com/VyronLee/vFrame.VFS.git#upm-vfs
https://github.com/VyronLee/vFrame.Bundler.VFSAdapter.git#upm
```

You can also edit your project's `Packages/manifest.json` directly:

```json
{
  "dependencies": {
    "com.vyronlee.vframe.bundler": "https://github.com/VyronLee/vFrame.Bundler.git#upm",
    "com.vyronlee.vframe.vfs": "https://github.com/VyronLee/vFrame.VFS.git#upm-vfs",
    "com.vyronlee.vframe.bundler.vfs-adapter": "https://github.com/VyronLee/vFrame.Bundler.VFSAdapter.git#upm"
  }
}
```

### Package Identity

- Package name: `com.vyronlee.vframe.bundler.vfs-adapter`
- Version: `1.0.1`
- Runtime code location: `Assets/vFrame.Bundler.VFSAdapter/Runtime`

## Quick Start

This minimal setup shows how to wire the adapter into `Bundler`. The key order is: prepare an `IFileSystemManager`, create `AssetBundleCreateAdapter`, then assign it to `BundlerOptions.AssetBundleCreateAdapter`.

```csharp
using System.IO;
using UnityEngine;
using vFrame.Bundler;
using vFrame.Bundler.VFSAdapter;
using vFrame.VFS;

public class BundlerBootstrap : MonoBehaviour
{
    [SerializeField] private string _manifestPath;

    private Bundler _bundler;
    private IFileSystemManager _fileSystemManager;
    private AssetBundleCreateAdapter _adapter;

    private void Start() {
        var manifestJson = File.ReadAllText(_manifestPath);
        var manifest = BundlerManifest.FromJson(manifestJson);

        _adapter = AssetBundleCreateAdapter.Create(_fileSystemManager);

        var options = new BundlerOptions {
            Mode = BundlerMode.AssetBundle,
            AssetBundleCreateAdapter = _adapter,
        };

        _bundler = new Bundler(manifest, options);
    }

    private void OnDestroy() {
        _adapter?.Destroy();
        _bundler?.Destroy();
        _fileSystemManager?.Destroy();
    }
}
```

## Usage

### Scenario 1: Inject the adapter into Bundler

`AssetBundleCreateAdapter` integrates with Bundler through `IAssetBundleCreateAdapter`. Once `BundlerOptions.AssetBundleCreateAdapter` is assigned, Bundler uses the adapter for AssetBundle creation.

```csharp
var adapter = AssetBundleCreateAdapter.Create(fileSystemManager);
var options = new BundlerOptions {
    Mode = BundlerMode.AssetBundle,
    AssetBundleCreateAdapter = adapter,
};
```

### Scenario 2: Load from mounted `.vpk` packages synchronously

`CreateAssetBundle(string path)` iterates over `IFileSystemManager.GetEnumerator()`, filters for `IPackageVirtualFileSystem`, reads `PackageBlockInfo.Offset`, and calls `AssetBundle.LoadFromFile(...)`.

```csharp
var adapter = AssetBundleCreateAdapter.Create(fileSystemManager);
var assetBundle = adapter.CreateAssetBundle("bundles/ui/common");
```

### Scenario 3: Start an asynchronous load from an absolute path

`CreateRequest(string path)` first calls `PathUtils.AbsolutePathToRelativeDataPath(path)`, then looks up that relative path in the mounted package file systems.

```csharp
var adapter = AssetBundleCreateAdapter.Create(fileSystemManager);
var request = adapter.CreateRequest(Application.dataPath + "/AssetBundles/ui/common");
yield return request;
var assetBundle = request.assetBundle;
```

### Scenario 4: Handle load failures

When `AssetBundle.LoadFromFile` or `AssetBundle.LoadFromFileAsync` returns `null`, the adapter raises `OnLoadFailed` and then throws `BundleLoadFailedException`.

```csharp
var adapter = AssetBundleCreateAdapter.Create(fileSystemManager);
adapter.OnLoadFailed += (packageFilePath, bundlePath) => {
    Debug.LogError($"Bundle load failed. package={packageFilePath}, bundle={bundlePath}");
};
```

## Dependencies

### Required packages

- `vFrame.Bundler`: defines `IAssetBundleCreateAdapter`, `BundlerOptions`, `BundlerManifest`, and `BundleLoadFailedException`
- `vFrame.VFS`: provides `IFileSystemManager`, `IPackageVirtualFileSystem`, `PackageFilePath`, and `GetBlockInfo`
- `vFrame.Core`: provides the `CreateAbility<TInstance, TArg>` lifecycle base class

### Assembly definition

`Assets/vFrame.Bundler.VFSAdapter/Runtime/vFrame.Bundler.VFSAdapter.asmdef` references:

- `vFrame.Bundler`
- `vFrame.Core`
- `vFrame.VFS`

## Usage Notes

### Loading flow

1. Enumerate registered file systems from `IFileSystemManager`
2. Keep only file systems that implement `IPackageVirtualFileSystem`
3. Check `Exist(...)` for the requested bundle path
4. Read the file offset through `GetBlockInfo(...)`
5. Load directly from `PackageFilePath` with `AssetBundle.LoadFromFile` or `AssetBundle.LoadFromFileAsync`

### Path behavior

- `CreateAssetBundle(string path)` looks up the path exactly as provided
- `CreateRequest(string path)` converts an absolute path into a relative `Assets/` data path first
- If your VFS stores relative bundle paths, passing a Unity project absolute path is the safest option for the async path

## Caveats

- The adapter only works with `IPackageVirtualFileSystem`; directory-based file systems are ignored
- Mount your `.vpk` packages before creating the adapter, or the final lookup will throw `BundleLoadFailedException`
- Exceptions thrown inside `OnLoadFailed` handlers are logged with `Debug.LogException`, but the adapter still throws `BundleLoadFailedException`
- The adapter inherits from `CreateAbility<AssetBundleCreateAdapter, IFileSystemManager>`, so call `Destroy()` when you are done with it
- This repository contains only a Runtime assembly, with no Editor assembly and no test assembly

## License

This project is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
