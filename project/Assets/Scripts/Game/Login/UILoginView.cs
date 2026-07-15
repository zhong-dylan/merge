public class UILoginView : ViewBase
{
    private const string ImgProgress = "img_progress";
    private ProgressAnimator progressAnimator;

    public override string PrefabPath => "Prefabs/UI_Login_local";
    protected override bool EnableAutoClose => false;

    protected override void OnInit()
    {
        base.OnInit();
        progressAnimator = Utils.GetImage(this, ImgProgress)?.GetComponent<ProgressAnimator>();
        progressAnimator?.SetProgress(0f, true);
    }

    public void SetProgress(float progress)
    {
        progressAnimator?.SetProgress(progress);
    }
}
