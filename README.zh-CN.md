[English](README.md) | [简体中文](README.zh-CN.md)

# vFrame Bundler 虚拟文件系统适配器

`vFrame.Bundler.VFSAdapter` 将 `vFrame.Bundler` 与 `vFrame.VFS` 连接起来，使 Bundler 可以直接从 `.vpk` 包内加载 AssetBundle，而不需要先解压到磁盘。

## 特性

- 直接实现 `IAssetBundleCreateAdapter`，可无缝接入 `BundlerOptions.AssetBundleCreateAdapter`
- 通过 `AssetBundle.LoadFromFile` 和文件偏移量直接读取 `.vpk` 内的 AssetBundle
- 同时提供同步加载 `CreateAssetBundle` 与异步加载 `CreateRequest`
- 仅遍历实现了 `IPackageVirtualFileSystem` 的文件系统，避免误用普通目录型 VFS
- 提供 `OnLoadFailed` 事件，便于记录失败的包路径与 bundle 路径
- 找不到 bundle 或底层加载失败时抛出 `BundleLoadFailedException`
- 运行时代码只有一个程序集，依赖关系清晰：`vFrame.Bundler`、`vFrame.Core`、`vFrame.VFS`

## 环境要求

- Unity 仓库工程版本：`2022.3.62f3`
- UPM 包声明最低版本：`2021.3`
- 需要先安装并可正常使用以下包：
  - `com.vyronlee.vframe.bundler`
  - `com.vyronlee.vframe.vfs`
- 运行时程序集依赖：
  - `vFrame.Bundler`
  - `vFrame.Core`
  - `vFrame.VFS`

## 安装

### 通过 UPM Git URL 安装

在 Unity 中打开 `Window > Package Manager > Add package from git URL...`，依次添加以下依赖：

```text
https://github.com/VyronLee/vFrame.Bundler.git#upm
https://github.com/VyronLee/vFrame.VFS.git#upm-vfs
https://github.com/VyronLee/vFrame.Bundler.VFSAdapter.git#upm
```

也可以直接编辑项目的 `Packages/manifest.json`：

```json
{
  "dependencies": {
    "com.vyronlee.vframe.bundler": "https://github.com/VyronLee/vFrame.Bundler.git#upm",
    "com.vyronlee.vframe.vfs": "https://github.com/VyronLee/vFrame.VFS.git#upm-vfs",
    "com.vyronlee.vframe.bundler.vfs-adapter": "https://github.com/VyronLee/vFrame.Bundler.VFSAdapter.git#upm"
  }
}
```

### 包标识

- 包名：`com.vyronlee.vframe.bundler.vfs-adapter`
- 版本：`1.0.1`
- 运行时代码目录：`Assets/vFrame.Bundler.VFSAdapter/Runtime`

## 快速开始

下面的最小示例演示如何把适配器接入 `Bundler`。关键点是：先准备 `IFileSystemManager`，再创建 `AssetBundleCreateAdapter`，最后把它赋值给 `BundlerOptions.AssetBundleCreateAdapter`。

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

## 用法

### 场景 1：把适配器注入 Bundler

`AssetBundleCreateAdapter` 通过 `IAssetBundleCreateAdapter` 与 Bundler 集成。只要 `BundlerOptions.AssetBundleCreateAdapter` 被赋值，Bundler 在加载 AssetBundle 时就会走这层适配器。

```csharp
var adapter = AssetBundleCreateAdapter.Create(fileSystemManager);
var options = new BundlerOptions {
    Mode = BundlerMode.AssetBundle,
    AssetBundleCreateAdapter = adapter,
};
```

### 场景 2：从已挂载的 `.vpk` 包同步加载

同步入口是 `CreateAssetBundle(string path)`。它会遍历 `IFileSystemManager.GetEnumerator()` 返回的文件系统，只处理实现了 `IPackageVirtualFileSystem` 的项，然后读取 `PackageBlockInfo.Offset` 并调用 `AssetBundle.LoadFromFile(...)`。

```csharp
var adapter = AssetBundleCreateAdapter.Create(fileSystemManager);
var assetBundle = adapter.CreateAssetBundle("bundles/ui/common");
```

### 场景 3：从绝对路径发起异步加载

异步入口是 `CreateRequest(string path)`。方法内部会先调用 `PathUtils.AbsolutePathToRelativeDataPath(path)`，再到已挂载的包文件系统中查找相对路径。

```csharp
var adapter = AssetBundleCreateAdapter.Create(fileSystemManager);
var request = adapter.CreateRequest(Application.dataPath + "/AssetBundles/ui/common");
yield return request;
var assetBundle = request.assetBundle;
```

### 场景 4：监听加载失败事件

当底层 `AssetBundle.LoadFromFile` 或 `AssetBundle.LoadFromFileAsync` 返回空值时，适配器会触发 `OnLoadFailed`，随后抛出 `BundleLoadFailedException`。

```csharp
var adapter = AssetBundleCreateAdapter.Create(fileSystemManager);
adapter.OnLoadFailed += (packageFilePath, bundlePath) => {
    Debug.LogError($"Bundle load failed. package={packageFilePath}, bundle={bundlePath}");
};
```

## 依赖关系

### 必需依赖

- `vFrame.Bundler`：定义 `IAssetBundleCreateAdapter`、`BundlerOptions`、`BundlerManifest`、`BundleLoadFailedException`
- `vFrame.VFS`：提供 `IFileSystemManager`、`IPackageVirtualFileSystem`、`PackageFilePath`、`GetBlockInfo`
- `vFrame.Core`：提供 `CreateAbility<TInstance, TArg>` 生命周期基类

### 程序集定义

`Assets/vFrame.Bundler.VFSAdapter/Runtime/vFrame.Bundler.VFSAdapter.asmdef` 引用了以下程序集：

- `vFrame.Bundler`
- `vFrame.Core`
- `vFrame.VFS`

## 使用说明

### 加载流程

1. 从 `IFileSystemManager` 枚举所有已注册文件系统
2. 过滤出实现了 `IPackageVirtualFileSystem` 的包文件系统
3. 根据 bundle 路径检查 `Exist(...)`
4. 通过 `GetBlockInfo(...)` 取得文件偏移量
5. 使用 `AssetBundle.LoadFromFile` 或 `AssetBundle.LoadFromFileAsync` 从 `PackageFilePath` 直接加载

### 路径约定

- `CreateAssetBundle(string path)` 直接使用传入路径进行查找
- `CreateRequest(string path)` 会先把绝对路径转换为相对 `Assets/` 数据路径
- 如果你的 VFS 内部记录的是相对路径，异步调用时优先传入 Unity 工程内的绝对路径更稳妥

## 注意事项

- 适配器只识别 `IPackageVirtualFileSystem`，普通目录型文件系统不会参与查找
- 在创建适配器之前，应先完成 `.vpk` 包挂载，否则会在遍历结束后抛出 `BundleLoadFailedException`
- `OnLoadFailed` 的回调异常会被 `Debug.LogException` 捕获，但随后仍会抛出 `BundleLoadFailedException`
- 适配器继承自 `CreateAbility<AssetBundleCreateAdapter, IFileSystemManager>`，使用完成后应调用 `Destroy()`
- 该仓库只有 Runtime 程序集，没有 Editor 扩展或测试程序集

## 许可证

本项目基于 [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0) 许可协议发布。
