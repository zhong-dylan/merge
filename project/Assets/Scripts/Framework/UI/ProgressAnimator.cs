using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ProgressAnimator : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private float smoothSpeed = 4f;

    private float currentProgress;
    private float targetProgress;

    private void Awake()
    {
        EnsureTargetImage();
        ApplyProgress(currentProgress);
    }

    private void OnValidate()
    {
        EnsureTargetImage();
        ApplyProgress(targetProgress);
    }

    private void Update()
    {
        if (Mathf.Approximately(currentProgress, targetProgress))
        {
            ApplyProgress(targetProgress);
            return;
        }

        currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, Time.deltaTime * smoothSpeed);
        ApplyProgress(currentProgress);
    }

    public void SetProgress(float progress, bool immediate = false)
    {
        targetProgress = Mathf.Clamp01(progress);
        if (!immediate)
        {
            return;
        }

        currentProgress = targetProgress;
        ApplyProgress(currentProgress);
    }

    private void EnsureTargetImage()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
    }

    private void ApplyProgress(float progress)
    {
        if (targetImage == null)
        {
            return;
        }

        targetImage.type = Image.Type.Filled;
        targetImage.fillMethod = Image.FillMethod.Horizontal;
        targetImage.fillAmount = Mathf.Clamp01(progress);
    }
}
