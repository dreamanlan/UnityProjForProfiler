using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class AssetBundleInfo
{
    public string assetBundleName;
    public string[] assetNames;
    public string[] dependencies;
}

public class AssetInformation : ScriptableObject
{
    public AssetBundleInfo[] assetBuildItems;

    private Dictionary<string, AssetBundleInfo> assetBuildInfoDict = new Dictionary<string, AssetBundleInfo>();
    private Dictionary<string, string> assetBundleNameDict = new Dictionary<string, string>();

    public void Awake()
    {
        if (assetBuildItems == null)
            return;
        assetBuildInfoDict.Clear();
        assetBundleNameDict.Clear();
        PrepareLUT();
    }

    private void PrepareLUT()
    {
        if (null == assetBuildItems || assetBuildInfoDict.Count > 0 || assetBundleNameDict.Count > 0)
            return;
        for (int i = 0; i < assetBuildItems.Length; i++) {
            string abName = assetBuildItems[i].assetBundleName;
            try {
                assetBuildInfoDict.Add(abName, assetBuildItems[i]);
            } catch {
                AssetBundleInfo abInfo;
                if (assetBuildInfoDict.TryGetValue(abName, out abInfo)) {
                    CsLibrary.LogSystem.Error("assetBundle duplicate:{0}->[{1}] and [{2}]", abName, string.Join(",", assetBuildItems[i].assetNames), string.Join(",", abInfo.assetNames));
                }
            }
            for (int j = 0; j < assetBuildItems[i].assetNames.Length; j++) {
                string assetName = assetBuildItems[i].assetNames[j];
                string bundleName = assetBuildItems[i].assetBundleName;
                try {
                    assetBundleNameDict.Add(assetName, bundleName);
                } catch {
                    string otherBundleName;
                    if (assetBundleNameDict.TryGetValue(assetName, out otherBundleName)) {
                        CsLibrary.LogSystem.Error("asset duplicate:{0}->[{1}] and [{2}]", assetName, bundleName, otherBundleName);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 获得 AssetBuildInfo
    /// </summary>
    /// <returns>The asset build info by asset name.</returns>
    /// <param name="assetName">Asset name.</param>
    public AssetBundleInfo GetAssetBuildInfoByAssetName(string assetName)
    {
        PrepareLUT();
        string assetBundleName;
        if (!assetBundleNameDict.TryGetValue(assetName.StrToLower(), out assetBundleName))
            return null;

        return GetAssetBuildInfoByAssetBundleName(assetBundleName);
    }

    /// <summary>
    /// 获得 AssetBuildInfo
    /// </summary>
    /// <returns>The asset build info by asset bundle name.</returns>
    /// <param name="assetBundleName">Asset bundle name.</param>
    public AssetBundleInfo GetAssetBuildInfoByAssetBundleName(string assetBundleName)
    {
        PrepareLUT();
        AssetBundleInfo info;
        if (assetBuildInfoDict.TryGetValue(assetBundleName.StrToLower(), out info))
            return info;

        return null;
    }

}
