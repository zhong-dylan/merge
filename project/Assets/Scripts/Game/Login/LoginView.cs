using UnityEngine;
using UnityEngine.UI;

public class LoginView : ViewBase
{
    private const string ImgProgress = "img_progress";
    private const float ProgressSmoothSpeed = 4f;

    private float currentProgress;
    private float targetProgress;

    public override string PrefabPath => "Prefabs/UI_Login_local";
    protected override bool EnableAutoClose => false;

    public void SetProgress(float progress)
    {
        targetProgress = Mathf.Clamp01(progress);
    }

    protected override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        if (Mathf.Approximately(currentProgress, targetProgress))
        {
            ApplyProgress(targetProgress);
            return;
        }

        currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, deltaTime * ProgressSmoothSpeed);
        ApplyProgress(currentProgress);
    }

    private void ApplyProgress(float progress)
    {
        var image = GetImage(ImgProgress);
        if (image == null)
        {
            return;
        }

        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillAmount = Mathf.Clamp01(progress);
    }
}
