using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

public sealed class Fade : MonoBehaviour
{
    [SerializeField] private Image image = null;

    private void Reset()
    {
        image = GetComponent<Image>();
    }

    private IEnumerator ChangeAlphaValueFrom0To1OverTime(
        float duration,
        Action on_completed,
        bool is_reversing = false
    )
    {
        if (!is_reversing) image.enabled = true;

        var elapsed_time = 0.0f;
        var color = image.color;

        while (elapsed_time < duration)
        {
            var elapsed_rate = Mathf.Min(elapsed_time / duration, 1.0f);
            color.a = is_reversing ? 1.0f - elapsed_rate : elapsed_rate;
            image.color = color;

            yield return null;
            elapsed_time += Time.deltaTime;
        }

        color.a = is_reversing ? 0f : 1f;
        image.color = color;



        if (is_reversing) image.enabled = false;
        if (on_completed != null) on_completed();
    }

    public void FadeIn(float duration, Action on_completed = null)
    {
        StartCoroutine(ChangeAlphaValueFrom0To1OverTime(duration, on_completed, true));
    }
    public void FadeOut(float duration, Action on_completed = null)
    {
        StartCoroutine(ChangeAlphaValueFrom0To1OverTime(duration, on_completed));
    }

}
