using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

[ExecuteInEditMode]
public class TextureAnimation : MonoBehaviour,ITimeControl
{
    MaterialPropertyBlock mpb { get { if (_mpb == null) _mpb = new MaterialPropertyBlock(); return _mpb; } }
    MaterialPropertyBlock _mpb;

    readonly int PropIndex = Shader.PropertyToID("_Idx");
    readonly int PropTime = Shader.PropertyToID("_T");

    public Renderer[] renderers;
    public float length = 5f;
    public float t;

    private void Reset()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void OnValidate()
    {
        SetIdx();
        SetNormalizedTime();
    }

    void SetIdx()
    {
        for (var i = 0; i < renderers.Length; i++)
            SetFloat(renderers[i], PropIndex, i);
    }
    void SetNormalizedTime()
    {
        for (var i = 0; i < renderers.Length; i++)
            SetFloat(renderers[i], PropTime, t / length);
    }

    void SetFloat(Renderer r, int propId, float val)
    {
        r.GetPropertyBlock(mpb);
        mpb.SetFloat(propId, val);
        r.SetPropertyBlock(mpb);
    }

    public void SetTime(double time)
    {
        t = (float)time;
        SetNormalizedTime();
    }

    public void OnControlTimeStart()
    {
        
    }

    public void OnControlTimeStop()
    {

    }
}
