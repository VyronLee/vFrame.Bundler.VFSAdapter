# vFrame Bundler 虚拟文件系统适配器

![vFrame](https://img.shields.io/badge/vFrame-Bundler_VFSAdapter-blue) [![Made with Unity](https://img.shields.io/badge/Made%20with-Unity-57b9d3.svg?style=flat&logo=unity)](https://unity3d.com) [![License](https://img.shields.io/badge/License-Apache%202.0-brightgreen.svg)](#License)

[vFrame Bundler](https://github.com/VyronLee/vFrame.Bundler) 支持通过实现 AssetBundler 加载适配器的方式自定义加载逻辑。此仓库利用` AssetBundle.LoadFromFile`接口中传入文件偏移量的方式，实现在不释放虚拟文件系统vpk文件的前提下加载 AssetBundle。

[English Version (Power by ChatGPT)](./README_en.md)

# 安装

该 Package 依赖于 [vFrame Core](https://github.com/VyronLee/vFrame.Core)、[vFrame Bundler](https://github.com/VyronLee/vFrame.Bundler) 以及 [vFrame VFS](https://github.com/VyronLee/vFrame.VFS)，使用前请先按照各仓库说明自行安装

推荐使用 Unity Package Manager 方式安装本工具，使用下面的链接进行导入：

https://github.com/VyronLee/vFrame.Bundler.VFSAdapter.git#upm

如需指定版本，链接后面带上版本号即可

# 使用说明

只需要在创建 `Bundler` 对象前，创建该适配器，然后传入即可：
```csharp
// 创建加载适配器
var adapter = AssetBundleCreateAdapter.Create(_fileSystemManager); // 自行修改为实际的文件系统管理器
// 创建Bundler设置对象
var options = new BundlerOptions {
    Mode = BundlerMode.AssetBundle,
    AssetBundleCreateAdapter = adapter,
    SearchPaths = new [] { Application.dataPath },
};
// 加载Bundler清单文件
var manifestJson = File.ReadAllText(_bundlerManifestPath); // 自行修改为实际的清单文件路径
var manifest = BundlerManifest.FromJson(manifestJson);
// 实例化Bundler对象
_bundler = new Bundler(manifest, options);
_bundler.SetLogLevel(LogLevel.Error);
```

## License

[Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0)