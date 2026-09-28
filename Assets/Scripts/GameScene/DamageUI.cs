using UnityEngine;
using TMPro; 

public class DamageUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private float moveSpeed = 1.0f; // 上昇スピード
    [SerializeField] private float fadeDuration = 0.8f; // 消えるまでの時間

    private float timer = 0f;
    private Color originalColor;

    public void Setup(int damageAmount)
    {
        if (damageText != null)
        {
            damageText.text = damageAmount.ToString();
            originalColor = damageText.color;
        }
    }

    private void Update()
    {
        // 上方向に移動
        transform.position += Vector3.up * (moveSpeed * Time.deltaTime);

        // 徐々に透明にして消去
        timer += Time.deltaTime;
        if (damageText != null)
        {
            float alpha = Mathf.Lerp(1.0f, 0f, timer / fadeDuration);
            damageText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
        }

        if (timer >= fadeDuration)
        {
            Destroy(gameObject);
        }
    }
}
