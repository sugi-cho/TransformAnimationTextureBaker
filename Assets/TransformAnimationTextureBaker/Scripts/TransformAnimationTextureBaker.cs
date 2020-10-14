using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TransformAnimationTextureBaker : MonoBehaviour
{
    public Transform transformRoot;
    public GameObject animationRoot;
    public AnimationClip[] clips;
    public int fps = 30;

    public Bounds bounds;

    [ContextMenu("bake texture")]
    void Bake()
    {
        if (transformRoot == null || clips.Length < 1)
            return;
        var transforms = transformRoot.GetComponentsInChildren<Renderer>().Select(r => r.transform).ToArray();
        var trsCount = transforms.Length;

        var min = Vector3.positiveInfinity;
        var max = Vector3.negativeInfinity;

        foreach (var c in clips)
        {
            var length = c.length;
            var height = Mathf.NextPowerOfTwo((int)(length * fps));
            var dt = length / (height - 1);

            var posTex = CreateBakeTexture(trsCount, height, c.isLooping);
            var rotTex = CreateBakeTexture(trsCount, height, c.isLooping);
            var scaleTex = CreateBakeTexture(trsCount, height, c.isLooping);

            for (var i = 0; i < height; i++)
            {
                var t = i * dt;
                c.SampleAnimation(animationRoot, t);
                for (var n = 0; n < trsCount; n++)
                {
                    var trs = transforms[n];
                    var pos = transformRoot.InverseTransformPoint(trs.position);
                    var rot = trs.rotation;
                    var scale = trs.lossyScale;
                    posTex.SetPixel(n, i, new Color(pos.x, pos.y, pos.z));
                    rotTex.SetPixel(n, i, new Color(rot.x, rot.y, rot.z, rot.w));
                    scaleTex.SetPixel(n, i, new Color(scale.x, scale.y, scale.z));

                    min = Vector3.Min(min, pos);
                    max = Vector3.Max(max, pos);
                }
            }
            bounds.min = min;
            bounds.max = max;

#if UNITY_EDITOR
            AssetDatabase.CreateAsset(posTex, $"Assets/{animationRoot.name}_{c.name}_posTex.asset");
            AssetDatabase.CreateAsset(rotTex, $"Assets/{animationRoot.name}_{c.name}_rotTex.asset");
            AssetDatabase.CreateAsset(scaleTex, $"Assets/{animationRoot.name}_{c.name}_scaleTex.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }
    }

    [ContextMenu("combine mesh")]
    void CombineMesh()
    {
        var rs = transformRoot.GetComponentsInChildren<Renderer>();
        var materialCombineMap = new Dictionary<Material, List<CombineInstance>>();

        for (var idx = 0; idx < rs.Length; idx++)
        {
            var r = rs[idx];
            var mats = r.sharedMaterials;
            var mesh = r.GetComponent<MeshFilter>().sharedMesh;
            for (var count = 0; count < mats.Length; count++)
            {
                var mat = r.sharedMaterials[count];
                if (!materialCombineMap.ContainsKey(mat))
                    materialCombineMap.Add(mat, new List<CombineInstance>());
                mesh.SetUVs(2, Enumerable.Repeat(Vector2.right * idx, mesh.vertexCount).ToList());
                var instance = new CombineInstance()
                {
                    mesh = mesh,
                    subMeshIndex = count % mesh.subMeshCount,
                    transform = Matrix4x4.identity,
                };
                materialCombineMap[mat].Add(instance);
            }
        }

        foreach(var pair in materialCombineMap)
        {
            var mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(pair.Value.ToArray());
            mesh.bounds = bounds;

#if UNITY_EDITOR
            AssetDatabase.CreateAsset(mesh, $"Assets/{animationRoot.name}_{pair.Key.name}_mesh.asset");
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
