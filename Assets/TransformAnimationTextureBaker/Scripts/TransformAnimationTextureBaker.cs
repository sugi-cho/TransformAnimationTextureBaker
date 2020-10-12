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

    Transform[] transforms;

    [ContextMenu("bake texture")]
    void Bake()
    {
        if (transformRoot == null || clips.Length < 1)
            return;
        transforms = transformRoot.GetComponentsInChildren<Renderer>().Select(r => r.transform).ToArray();
        var trsCount = transforms.Length;

        foreach(var c in clips)
        {
            var length = c.length;
            var height = Mathf.NextPowerOfTwo((int)(length * fps));
            var dt = length / (height - 1);

            var posTex = CreateBakeTexture(trsCount, height, c.isLooping);
            var rotTex = CreateBakeTexture(trsCount, height, c.isLooping);
            var scaleTex = CreateBakeTexture(trsCount, height, c.isLooping);

            for(var i = 0; i < height; i++)
            {
                var t = i * dt;
                c.SampleAnimation(animationRoot, t);
                for(var n = 0; n < trsCount; n++)
                {
                    var trs = transforms[n];
                    var pos = transformRoot.InverseTransformPoint(trs.position);
                    var rot = trs.rotation;
                    var scale = trs.lossyScale;
                    posTex.SetPixel(n, i, new Color(pos.x, pos.y, pos.z));
                    rotTex.SetPixel(n, i, new Color(rot.x, rot.y, rot.z, rot.w));
                    scaleTex.SetPixel(n, i, new Color(scale.x, scale.y, scale.z));
                }
            }

#if UNITY_EDITOR
            AssetDatabase.CreateAsset(posTex, $"Assets/{animationRoot.name}_{c.name}_posTex.asset");
            AssetDatabase.CreateAsset(rotTex, $"Assets/{animationRoot.name}_{c.name}_rotTex.asset");
            AssetDatabase.CreateAsset(scaleTex, $"Assets/{animationRoot.name}_{c.name}_scaleTex.asset");
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
