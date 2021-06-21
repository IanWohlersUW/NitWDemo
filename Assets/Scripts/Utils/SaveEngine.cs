using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SaveEngine : MonoBehaviour
{
    // Quick demo of what a serialization function looks like
    public static Dictionary<string, bool> SerializeBools(string path) =>
        Resources.LoadAll(path, typeof(BoolReference))
            .Cast<BoolReference>()
            .ToDictionary(boolRef => AssetDatabase.GetAssetPath(boolRef),
                boolRef => boolRef.isTrue);

    // And restoration
    public static void RestoreBools(Dictionary<string, bool> serialized, string path)
    {
        var bools = Resources.LoadAll(path, typeof(BoolReference))
            .Cast<BoolReference>();
        foreach (BoolReference boolRef in bools)
        {
            var currPath = AssetDatabase.GetAssetPath(boolRef);
            if (serialized.ContainsKey(currPath))
                boolRef.isTrue = serialized[currPath];
        }
    }
}
