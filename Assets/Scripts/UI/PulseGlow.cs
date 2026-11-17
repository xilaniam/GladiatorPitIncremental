using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class PulseGlow : MonoBehaviour
{
    [SerializeField] Graphic graphic;
    [SerializeField] float minScale = 0.9f;
    [SerializeField] float maxScale = 1.15f;
    [SerializeField, Range(0f, 1f)] float minAlpha = 0.35f;
    [SerializeField, Range(0f, 1f)] float maxAlpha = 0.85f;
    [SerializeField, Tooltip("Seconds for one full in-and-out pulse")] float period = 1.4f;

    private Sequence pulse;

    private void OnEnable()
    {
        transform.localScale = Vector3.one * minScale;
        SetAlpha(minAlpha);

        pulse = Sequence.Create(cycles: -1, cycleMode: CycleMode.Yoyo, useUnscaledTime: true)
            .Group(Tween.Scale(transform, maxScale, period * 0.5f, Ease.InOutSine))
            .Group(Tween.Alpha(graphic, maxAlpha, period * 0.5f, Ease.InOutSine));
    }

    private void OnDisable()
    {
        if (pulse.isAlive) pulse.Stop();
    }

    private void SetAlpha(float alpha)
    {
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }
}
