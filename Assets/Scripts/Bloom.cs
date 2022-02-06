using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class Bloom : MonoBehaviour
{

    [Range(1, 16)]
    public int iterations = 1;
    [Range(0, 10)]
    public float threshold = 1;
    [Range(0, 1)]
    public float softThreshold = 0.5f;
    [Range(0, 10)]
    public float intensity = 1;

    public Shader bloomShader;
    public bool debug;

    [NonSerialized]
    Material bloom;

    const int BoxDownPrefilterPass = 0;
    const int BoxDownPass = 1;
    const int BoxUpPass = 2;
    const int ApplyBloomPass = 3;
    const int DebugBloomPass = 4;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (bloom == null) 
        {
            bloom = new Material(bloomShader);
            bloom.hideFlags = HideFlags.HideAndDontSave;
        }
        float knee = threshold * softThreshold;
        Vector4 filter;
        filter.x = threshold;
        filter.y = filter.x - knee;
        filter.z = 2f * knee;
        filter.w = 0.25f / (knee + 0.00001f);
        bloom.SetVector("_Filter", filter);
        bloom.SetFloat("_Intensity", Mathf.GammaToLinearSpace(intensity));
        RenderTexture[] textures = new RenderTexture[16];

        int width = source.width;
        int height = source.height;
        RenderTextureFormat format = source.format;

        RenderTexture currentDestination = textures[0] = RenderTexture.GetTemporary(
                width,height,0,format
            );
        /*
         * 需要模糊图像时并不需要对深度缓存区域处理，因为上方函数第三个参数为0
         * 因为我们打开了HDR选项，所以第四项直接采用原本的图像格式
         */

        Graphics.Blit(source, currentDestination,bloom,BoxDownPrefilterPass);
        RenderTexture currentSource = currentDestination;

        int i = 1;
        for (; i < iterations; i++)
        {
            width /= 2;
            height /= 2;
            if (height < 2) break;
            currentDestination = textures[i] =
                RenderTexture.GetTemporary(width, height, 0, format);
            Graphics.Blit(currentSource, currentDestination,bloom,BoxDownPass);
            //RenderTexture.ReleaseTemporary(currentSource);
            currentSource = currentDestination;
        }
        for (i -= 2; i >= 0; i--) 
        {
            currentDestination = textures[i];
            textures[i] = null;
            Graphics.Blit(currentSource, currentDestination,bloom,BoxUpPass);
            RenderTexture.ReleaseTemporary(currentSource);
            currentSource = currentDestination;
        }


        if (debug)
        {
            Graphics.Blit(currentSource, destination, bloom, DebugBloomPass);
        }
        else
        {
            bloom.SetTexture("_SourceTex", currentSource);
            Graphics.Blit(source, destination, bloom, ApplyBloomPass);
        }
        RenderTexture.ReleaseTemporary(currentSource);

        
    }
}

