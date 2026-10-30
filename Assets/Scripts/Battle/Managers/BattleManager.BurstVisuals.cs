using System.Collections.Generic;
using UnityEngine;

public partial class BattleManager
{
    private sealed class BurstMaterialState
    {
        internal Material[] Original, Applied;
        internal bool Colored;
    }
    private readonly Dictionary<Renderer, BurstMaterialState> burstRendererMaterials = new(); // 只拥有实例副本，不改共享资产。
    private bool burstGrayVisible;
    private float nextBurstVisualScan;

    private void UpdateBurstVisuals()
    {
        if (!burstGrayVisible || Time.unscaledTime < nextBurstVisualScan) return;
        nextBurstVisualScan = Time.unscaledTime + 0.1f;
        RefreshBurstVisuals();
    }

    private void RefreshBurstVisuals()
    {
        if (!burstGrayVisible) return;
        var fallback = Resources.Load<Shader>("Battle/BurstGrayscale");
        foreach (var renderer in FindObjectsOfType<Renderer>())
        {
            if (renderer.GetComponentInParent<Canvas>() != null) continue;
            var piece = renderer.GetComponentInParent<PieceController>();
            bool selected = piece != null && selectedBurstActors.Contains(piece) && !piece.isDead;
            if (burstRendererMaterials.TryGetValue(renderer, out var previous))
            {
                if (previous.Colored == selected) continue;
                RestoreBurstRenderer(renderer);
            }
            if (selected)
            {
                var original = renderer.sharedMaterials;
                var colored = (Material[])original.Clone();
                bool copied = false;
                for (int i = 0; i < original.Length; i++)
                    if (original[i] != null && original[i].IsKeywordEnabled("GREYSCALE_ON"))
                    {
                        colored[i] = new Material(original[i]);
                        colored[i].DisableKeyword("GREYSCALE_ON");
                        copied = true;
                    }
                if (copied)
                {
                    burstRendererMaterials.Add(renderer, new BurstMaterialState { Original = original, Applied = colored, Colored = true });
                    renderer.sharedMaterials = colored;
                }
                continue;
            }
            var originals = renderer.sharedMaterials;
            var applied = new Material[originals.Length];
            for (int i = 0; i < originals.Length; i++)
            {
                var original = originals[i];
                if (original == null) continue;
                if (original.HasProperty("_GreyscaleLuminosity"))
                {
                    applied[i] = new Material(original);
                    applied[i].EnableKeyword("GREYSCALE_ON");
                    applied[i].SetColor("_GreyscaleTintColor", Color.white);
                    applied[i].SetFloat("_GreyscaleBlend", 1f);
                }
                else if (fallback != null)
                {
                    var copy = new Material(fallback);
                    if (original.HasProperty("_BaseMap")) copy.mainTexture = original.GetTexture("_BaseMap");
                    else if (original.HasProperty("_MainTex")) copy.mainTexture = original.mainTexture;
                    if (original.HasProperty("_BaseColor")) copy.color = original.GetColor("_BaseColor");
                    else if (original.HasProperty("_Color")) copy.color = original.color;
                    if (original.HasProperty("_MainTex"))
                    {
                        copy.mainTextureScale = original.mainTextureScale;
                        copy.mainTextureOffset = original.mainTextureOffset;
                    }
                    applied[i] = copy;
                }
                else applied[i] = original;
            }
            burstRendererMaterials.Add(renderer, new BurstMaterialState { Original = originals, Applied = applied });
            renderer.sharedMaterials = applied;
        }
    }

    private void RestoreBurstRenderer(Renderer renderer)
    {
        if (!burstRendererMaterials.TryGetValue(renderer, out var state)) return;
        if (renderer != null)
        {
            var current = renderer.sharedMaterials;
            for (int i = 0; i < current.Length && i < state.Applied.Length; i++)
                if (current[i] == state.Applied[i]) current[i] = state.Original[i];
            renderer.sharedMaterials = current;
        }
        for (int i = 0; i < state.Applied.Length; i++)
            if (state.Applied[i] != null && state.Applied[i] != state.Original[i]) Destroy(state.Applied[i]);
        burstRendererMaterials.Remove(renderer);
    }

    private void RestoreBurstVisuals()
    {
        burstGrayVisible = false;
        foreach (var renderer in new List<Renderer>(burstRendererMaterials.Keys)) RestoreBurstRenderer(renderer);
    }
}
