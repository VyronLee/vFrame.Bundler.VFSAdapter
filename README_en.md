# vFrame Bundler Virtual File System Adapter

![vFrame](https://img.shields.io/badge/vFrame-Bundler_VFSAdapter-blue) [![Made with Unity](https://img.shields.io/badge/Made%20with-Unity-57b9d3.svg?style=flat&logo=unity)](https://unity3d.com) [![License](https://img.shields.io/badge/License-Apache%202.0-brightgreen.svg)](#License)

The [vFrame Bundler](https://github.com/VyronLee/vFrame.Bundler) supports customizing the loading logic by implementing the AssetBundler loading adapter. This repository utilizes the file offset parameter in the `AssetBundle.LoadFromFile` interface to load AssetBundles without releasing the virtual file system vpk file.

# Installation

This Package depends on [vFrame Core](https://github.com/VyronLee/vFrame.Core), [vFrame Bundler](https://github.com/VyronLee/vFrame.Bundler), and [vFrame VFS](https://github.com/VyronLee/vFrame.VFS). Please install them according to the instructions in each repository before using.

It is recommended to install this tool using the Unity Package Manager by importing it with the following link:

https://github.com/VyronLee/vFrame.Bundler.VFSAdapter.git#upm

If you need to specify a version, just add the version number after the link.

# Usage Instructions

You only need to create this adapter before creating the `Bundler` object, and then pass it in:
```csharp
// Create a loading adapter
var adapter = AssetBundleCreateAdapter.Create(_fileSystemManager); // Modify to the actual file system manager
// Create Bundler settings object
var options = new BundlerOptions {
    Mode = BundlerMode.AssetBundle,
    AssetBundleCreateAdapter = adapter,
    SearchPaths = new [] { Application.dataPath },
};
// Load Bundler manifest file
var manifestJson = File.ReadAllText(_bundlerManifestPath); // Modify to the actual manifest file path
var manifest = BundlerManifest.FromJson(manifestJson);
// Instantiate Bundler object
_bundler = new Bundler(manifest, options);
_bundler.SetLogLevel(LogLevel.Error);
```

## License

[Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0)