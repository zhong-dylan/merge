using UnityEngine;

public static class UIConst
{
    public const float DesignWidth = 1280f;
    public const float DesignHeight = 1920f;

    public static readonly Vector2 DesignResolution = new(DesignWidth, DesignHeight);
    public static readonly Vector2 HalfDesignResolution = new(DesignWidth * 0.5f, DesignHeight * 0.5f);
}
