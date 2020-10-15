using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TransformAnimationTextureBaker : MonoBehaviour
{
    public Transform root;
    public AnimationClip[] clips;

    private void Reset()
    {
        if (GetComponent<Animator>() != null)
            root = transform;
    }

    [ContextMenu("bake texture")]
    void Bake()
    {
        if (root == null || clips.Length < 1)
            return;
        var transforms = root.GetComponentsInChildren<Renderer>().Select(r => r.transform).ToArray();
        var trsCount = transforms.Length;

        var min = Vector3.positiveInfinity;
        var max = Vector3.negativeInfinity;

        foreach (var c in clips)
        {
            var height = (int)(c.length * c.frameRate);
            var dt = 1f / c.frameRate;

            var posTex = CreateBakeTexture(trsCount, height, c.isLooping);
            var rotTex = CreateBakeTexture(trsCount, height, c.isLooping);
            var scaleTex = CreateBakeTexture(trsCount, height, c.isLooping);

            for (var i = 0; i < height; i++)
            {
                var t = i * dt;
                c.SampleAnimation(root.gameObject, t);
                for (var n = 0; n < trsCount; n++)
                {
                    var trs = transforms[n];
                    var pos = trs.position;
                    var rot = trs.rotation;
                    var scale = trs.lossyScale;
                    posTex.SetPixel(n, i, new Color(pos.x, pos.y, pos.z));
                    rotTex.SetPixel(n, i, new Color(rot.x, rot.y, rot.z, rot.w));
                    scaleTex.SetPixel(n, i, new Color(scale.x, scale.y, scale.z));
                }
            }

#if UNITY_EDITOR
            if (!AssetDatabase.IsValidFolder($"Assets/{root.name}"))
                AssetDatabase.CreateFolder("Assets", root.name);

            AssetDatabase.CreateAsset(posTex, $"Assets/{root.name}/{c.name}_posTex.asset");
            AssetDatabase.CreateAsset(rotTex, $"Assets/{root.name}/{c.name}_rotTex.asset");
            AssetDatabase.CreateAsset(scaleTex, $"Assets/{root.name}/{c.name}_scaleTex.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }
        clips[0].SampleAnimation(root.gameObject, 0);
    }

    [ContextMenu("reset anim")]
    void ResetAnim()
    {
        clips[0].SampleAnimation(root.gameObject, 0);
    }

    [ContextMenu("combine mesh")]
    void CombineMesh()
    {
        var rs = root.GetComponentsInChildren<Renderer>();
        var materialCombineMap = new Dictionary<Material, List<CombineInstance>>();

        for (var idx = 0; idx < rs.Length; idx++)
        {
            var r = rs[idx];
            var mats = r.sharedMaterials;
            var mesh = r.GetComponent<MeshFilter>().sharedMesh;
            mesh.SetUVs(2, Enumerable.Repeat(Vector2.right * idx, mesh.vertexCount).ToList());
            for (var count = 0; count < mats.Length; count++)
            {
                var mat = r.sharedMaterials[count];
                if (!materialCombineMap.ContainsKey(mat))
                    materialCombineMap.Add(mat, new List<CombineInstance>());
                var instance = new CombineInstance()
                {
                    mesh = mesh,
                    subMeshIndex = count % mesh.subMeshCount,
                    transform = Matrix4x4.identity,
                };
                materialCombineMap[mat].Add(instance);
            }
        }

        foreach (var pair in materialCombineMap)
        {
            var mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(pair.Value.ToArray());

#if UNITY_EDITOR
            if (!AssetDatabase.IsValidFolder($"Assets/{root.name}"))
                AssetDatabase.CreateFolder("Assets", root.name);

            AssetDatabase.CreateAsset(mesh, $"Assets/{root.name}/{pair.Key.name}_mesh.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }
    }

    Texture2D CreateBakeTexture(int w, int h, bool isLooping)
    {
        return new Texture2D(w, h, TextureFormat.RGBAHalf, false)
        {
            wrapModeU = TextureWrapMode.Clamp,
            wrapModeV = isLooping ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
    }
}
