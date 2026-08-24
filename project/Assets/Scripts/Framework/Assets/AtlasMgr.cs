using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public sealed class AtlasMgr : MonoSingle<AtlasMgr>
{
    private const string AtlasRoot = "Atlas";
    private const string BgRoot = "Bg";
    private const string ShaderRoot = "Shaders";
    private const string GrayShaderName = "UIGray";

    private Loader loader;
    private readonly Dictionary<string, AtlasHandle> atlasHandles = new();
    private readonly Dictionary<string, SpriteHandle> spriteHandles = new();
    private readonly List<Action<Material>> pendingGrayMaterialCallbacks = new();
    private Shader grayShader;
    private Material grayMaterial;
    private int grayMaterialRefCount;
    private bool isGrayMaterialLoading;

    protected override void Init()
    {
        base.Init();
        EnsureLoader();
    }

    public Coroutine LoadSpriteFromAtlas(string atlasName, string spriteName, Action<Sprite> onCompleted)
    {
        EnsureLoader();
        return TimeMgr.I.RunCoroutine(LoadSpriteFromAtlasCoroutine(atlasName, spriteName, onCompleted));
    }

    public Coroutine LoadBgSprite(string spriteName, Action<Sprite> onCompleted)
    {
        EnsureLoader();
        return TimeMgr.I.RunCoroutine(LoadBgSpriteCoroutine(spriteName, onCompleted));
    }

    public void LoadGrayMaterial(Action<Material> onCompleted)
    {
        EnsureLoader();
        if (grayMaterial != null)
        {
            grayMaterialRefCount++;
            onCompleted?.Invoke(grayMaterial);
            return;
        }

        grayMaterialRefCount++;
        if (onCompleted != null)
        {
            pendingGrayMaterialCallbacks.Add(onCompleted);
        }

        if (isGrayMaterialLoading)
        {
            return;
        }

        isGrayMaterialLoading = true;
        loader.Load<Shader>(BuildShaderKey(GrayShaderName), shader =>
        {
            isGrayMaterialLoading = false;
            grayShader = shader;
            if (grayShader != null)
            {
                grayMaterial = new Material(grayShader)
                {
                    name = "UIGray_Runtime_Material",
                };
            }

            for (var i = 0; i < pendingGrayMaterialCallbacks.Count; i++)
            {
                pendingGrayMaterialCallbacks[i]?.Invoke(grayMaterial);
            }

            pendingGrayMaterialCallbacks.Clear();
        });
    }

    public void ReleaseGrayMaterial()
    {
        if (grayMaterialRefCount <= 0)
        {
            return;
        }

        grayMaterialRefCount--;
        if (grayMaterialRefCount > 0)
        {
            return;
        }

        if (grayMaterial != null)
        {
            Destroy(grayMaterial);
            grayMaterial = null;
        }

        grayShader = null;
        loader.Unload(BuildShaderKey(GrayShaderName));
    }

    public void ReleaseAtlasSprite(string atlasName)
    {
        if (string.IsNullOrWhiteSpace(atlasName))
        {
            return;
        }

        if (!atlasHandles.TryGetValue(atlasName, out var handle))
        {
            return;
        }

        handle.RefCount--;
        if (handle.RefCount > 0)
        {
            atlasHandles[atlasName] = handle;
            return;
        }

        loader.Unload(BuildAtlasKey(atlasName));
        atlasHandles.Remove(atlasName);
    }

    public void ReleaseBgSprite(string spriteName)
    {
        if (string.IsNullOrWhiteSpace(spriteName))
        {
            return;
        }

        if (!spriteHandles.TryGetValue(spriteName, out var handle))
        {
            return;
        }

        handle.RefCount--;
        if (handle.RefCount > 0)
        {
            spriteHandles[spriteName] = handle;
            return;
        }

        loader.Unload(BuildBgKey(spriteName));
        spriteHandles.Remove(spriteName);
    }

    private void EnsureLoader()
    {
        if (loader == null)
        {
            loader = GetComponent<Loader>();
            if (loader == null)
            {
                loader = gameObject.AddComponent<Loader>();
            }
        }
    }

    private IEnumerator LoadSpriteFromAtlasCoroutine(string atlasName, string spriteName, Action<Sprite> onCompleted)
    {
        if (string.IsNullOrWhiteSpace(atlasName) || string.IsNullOrWhiteSpace(spriteName))
        {
            onCompleted?.Invoke(null);
            yield break;
        }

        if (atlasHandles.TryGetValue(atlasName, out var existing))
        {
            existing.RefCount++;
            atlasHandles[atlasName] = existing;
            onCompleted?.Invoke(existing.Atlas != null ? existing.Atlas.GetSprite(spriteName) : null);
            yield break;
        }

        var isDone = false;
        SpriteAtlas loadedAtlas = null;
        loader.Load<SpriteAtlas>(BuildAtlasKey(atlasName), atlas =>
        {
            loadedAtlas = atlas;
            isDone = true;
        });

        yield return new WaitUntil(() => isDone);

        if (loadedAtlas == null)
        {
            onCompleted?.Invoke(null);
            yield break;
        }

        atlasHandles[atlasName] = new AtlasHandle
        {
            Atlas = loadedAtlas,
            RefCount = 1,
        };

        onCompleted?.Invoke(loadedAtlas.GetSprite(spriteName));
    }

    private IEnumerator LoadBgSpriteCoroutine(string spriteName, Action<Sprite> onCompleted)
    {
        if (string.IsNullOrWhiteSpace(spriteName))
        {
            onCompleted?.Invoke(null);
            yield break;
        }

        if (spriteHandles.TryGetValue(spriteName, out var existing))
        {
            existing.RefCount++;
            spriteHandles[spriteName] = existing;
            onCompleted?.Invoke(existing.Sprite);
            yield break;
        }

        var isDone = false;
        Sprite loadedSprite = null;
        loader.Load<Sprite>(BuildBgKey(spriteName), sprite =>
        {
            loadedSprite = sprite;
            isDone = true;
        });

        yield return new WaitUntil(() => isDone);

        if (loadedSprite == null)
        {
            onCompleted?.Invoke(null);
            yield break;
        }

        spriteHandles[spriteName] = new SpriteHandle
        {
            Sprite = loadedSprite,
            RefCount = 1,
        };

        onCompleted?.Invoke(loadedSprite);
    }

    private static string BuildAtlasKey(string atlasName)
    {
        return $"{AtlasRoot}/{atlasName}_remote";
    }

    private static string BuildBgKey(string spriteName)
    {
        return $"{BgRoot}/{spriteName}_remote";
    }

    private static string BuildShaderKey(string shaderName)
    {
        return $"{ShaderRoot}/{shaderName}_remote";
    }

    private struct AtlasHandle
    {
        public SpriteAtlas Atlas;
        public int RefCount;
    }

    private struct SpriteHandle
    {
        public Sprite Sprite;
        public int RefCount;
    }
}
