using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using LogLevel = BepInEx.Logging.LogLevel;
using Object = UnityEngine.Object;

namespace LC_GiftBox_Config.libs.UnityUtils;

public static class UnityUtils
{
    public static GameObject InstantiateInactive(GameObject prefab, Vector3? position = null, Quaternion? rotation = null, Transform? parent = null)
    {
        // Make inactive dummy parent
        GameObject dummyParent = new("[libs.UnityUtils.InstantiateInactive] dummyParent");
        dummyParent.SetActive(false);
        dummyParent.transform.SetParent(parent);
        
        // Instantiate new object in dummy parent and deactivate the object
        GameObject newObj = Object.Instantiate(original: prefab, position: position ?? Vector3.zero, rotation: rotation ?? Quaternion.identity, parent: dummyParent.transform);
        newObj.SetActive(false);

        // Move new object into actual parent and destroy dummy parent
        newObj.transform.SetParent(parent, worldPositionStays: false);
        Object.Destroy(dummyParent);

        return newObj;
    }
}