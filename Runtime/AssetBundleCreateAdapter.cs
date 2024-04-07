// ------------------------------------------------------------
//         File: AssetBundleCreateAdapter.cs
//        Brief: AssetBundleCreateAdapter.cs
//
//       Author: VyronLee, lwz_jz@hotmail.com
//
//      Created: 2024-4-7 16:59
//    Copyright: Copyright (c) 2024, VyronLee
// ============================================================

using System;
using UnityEngine;
using vFrame.Core.Base;
using vFrame.VFS;

namespace vFrame.Bundler.VFSAdapter
{
    public interface IBundleLoadFailedCallback
    {
        event Action<string, string> OnLoadFailed;
    }

    public class AssetBundleCreateAdapter : CreateAbility<AssetBundleCreateAdapter, IFileSystemManager>, IAssetBundleCreateAdapter
    {
        private IFileSystemManager _fileSystemManager;

        public event Action<string, string> OnLoadFailed;

        protected override void OnCreate(IFileSystemManager fileSystemManager) {
            _fileSystemManager = fileSystemManager;
        }

        protected override void OnDestroy() {
            _fileSystemManager = null;
        }

        public AssetBundle CreateAssetBundle(string path) {
            using (var enumerator = _fileSystemManager.GetEnumerator()) {
                while (enumerator.MoveNext()) {
                    var fileSystem = enumerator.Current as IPackageVirtualFileSystem;
                    if (null == fileSystem) {
                        continue;
                    }

                    if (!fileSystem.Exist(path)) {
                        continue;
                    }

                    var blockInfo = fileSystem.GetBlockInfo(path);
                    var ab = AssetBundle.LoadFromFile(fileSystem.PackageFilePath, 0u, (ulong)blockInfo.Offset);
                    if (ab) {
                        return ab;
                    }
                    HandleLoadFailed(fileSystem.PackageFilePath, path);
                }
            }
            throw new BundleLoadFailedException("Bundle not found in file system: " + path);
        }

        public AssetBundleCreateRequest CreateRequest(string path) {
            var relativePath = PathUtils.AbsolutePathToRelativeDataPath(path);
            using (var enumerator = _fileSystemManager.GetEnumerator()) {
                while (enumerator.MoveNext()) {
                    var fileSystem = enumerator.Current as IPackageVirtualFileSystem;
                    if (null == fileSystem) {
                        continue;
                    }

                    if (!fileSystem.Exist(relativePath)) {
                        continue;
                    }

                    var blockInfo = fileSystem.GetBlockInfo(relativePath);
                    var request = AssetBundle.LoadFromFileAsync(fileSystem.PackageFilePath, 0u, (ulong)blockInfo.Offset);
                    if (null != request) {
                        return request;
                    }
                    HandleLoadFailed(fileSystem.PackageFilePath, path);
                }
            }
            throw new BundleLoadFailedException("Bundle not found in file system: " + path);
        }

        private void HandleLoadFailed(string packageFilePath, string abPath) {
            try {
                OnLoadFailed?.Invoke(packageFilePath, abPath);
            }
            catch (Exception e) {
                Debug.LogException(e);
            }
            throw new BundleLoadFailedException($"Bundle load failed from vpk: {packageFilePath}, path: {abPath}");
        }
    }
}