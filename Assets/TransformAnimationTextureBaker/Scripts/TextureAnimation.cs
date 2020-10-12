using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TextureAnimation : MonoBehaviour
{
    MaterialPropertyBlock mpb { get { if (_mpb == null) _mpb = new MaterialPropertyBlock(); return _mpb; } }
    MaterialPropertyBlock _mpb;

    readonly int PropIndex = Shader.PropertyToID("_Idx");
    readonly int PropTime = Shader.PropertyToID("_T");

    public Renderer[] renderers;
    public int fps = 30;
    public float t;

    private void Reset()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void OnValidate()
    {
        SetProp();
    }

    void SetProp()
    {
        for (var i = 0; i < renderers.Length; i++)
        {
            SetFloat(renderers[i], PropIndex, i);
            SetFloat(renderers[i], PropTime, t*fps);
        }
    }

    void SetFloat(Renderer r, int propId, float val)
    {
        r.GetPropertyBlock(mpb);
        mpb.SetFloat(propId, val);
        r.SetPropertyBlock(mpb);
    }
}
