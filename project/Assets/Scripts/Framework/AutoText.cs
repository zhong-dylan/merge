using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class AutoText : MonoBehaviour
{
    [SerializeField] private bool useDownloadedFont = true;

    private TMP_Text textComponent;

    private void Reset()
    {
        CacheComponent();
        ApplyDownloadedFontIfNeeded();
    }

    private void Awake()
    {
        CacheComponent();
        ApplyDownloadedFontIfNeeded();
    }

    private void OnEnable()
    {
        CacheComponent();
        FontMgr.I.FontAssetChanged += OnFontAssetChanged;
        ApplyDownloadedFontIfNeeded();
    }

    private void OnDisable()
    {
        var fontMgr = FindFirstObjectByType<FontMgr>();
        if (fontMgr != null)
        {
            fontMgr.FontAssetChanged -= OnFontAssetChanged;
        }
    }

    private void OnValidate()
    {
        CacheComponent();
        ApplyDownloadedFontIfNeeded();
    }

    public void ApplyDownloadedFontIfNeeded()
    {
        if (!useDownloadedFont)
        {
            return;
        }

        CacheComponent();
        if (textComponent == null)
        {
            return;
        }

        var fontMgr = FontMgr.I;
        if (fontMgr.HasDownloadedFontAsset)
        {
            ApplyFontAsset(fontMgr.DownloadedFontAsset);
        }
    }

    public static void EnsureFont(Component textTarget)
    {
        if (textTarget is not TMP_Text tmpText)
        {
            return;
        }

        EnsureComponent(tmpText).ApplyDownloadedFontIfNeeded();
    }

    public static AutoText EnsureComponent(TMP_Text tmpText)
    {
        if (tmpText == null)
        {
            return null;
        }

        var autoText = tmpText.GetComponent<AutoText>();
        if (autoText == null)
        {
            autoText = tmpText.gameObject.AddComponent<AutoText>();
        }

        return autoText;
    }

    private void CacheComponent()
    {
        if (textComponent == null)
        {
            textComponent = GetComponent<TMP_Text>();
        }
    }

    private void OnFontAssetChanged(TMP_FontAsset fontAsset)
    {
        ApplyFontAsset(fontAsset);
    }

    private void ApplyFontAsset(TMP_FontAsset fontAsset)
    {
        if (textComponent == null || fontAsset == null || textComponent.font == fontAsset)
        {
            return;
        }

        textComponent.font = fontAsset;
        textComponent.havePropertiesChanged = true;
        textComponent.SetAllDirty();
        textComponent.ForceMeshUpdate();
    }
}
