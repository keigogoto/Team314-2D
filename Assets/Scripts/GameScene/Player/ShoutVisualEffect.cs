//-------------------------------------------------------
//
//  ShoutVisualEffect.cs
//
//  概要
//  シャウトの範囲を前方後円墳型で表示するエフェクト
//
//  更新履歴
//
//  2026/05/25  作成
//  2026/07/06  3D対応。
//
//-------------------------------------------------------
using UnityEngine;

public class ShoutVisualEffect : MonoBehaviour
{
    [Tooltip("シャウトが表示される時間")]
    [SerializeField] private float displayTime = 2f;
    [Tooltip("プレイヤーの後ろに出る判定の大きさ")]
    [SerializeField] private float backCircleRadius = 1f;
    [Tooltip("奥行き")]
    [SerializeField] private float fanRange = 5f;
    [Tooltip("角度")]
    [SerializeField] private float fanAngle = 90f;
    [SerializeField] private Color effectColor = new Color(1f, 1f, 0f, 0.5f);

    private LineRenderer _lineRenderer;
    private float _timer;

    private void Awake()
    {
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.startWidth = 0.05f;
        _lineRenderer.endWidth = 0.05f;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.loop = true;
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.startColor = effectColor;
        _lineRenderer.endColor = effectColor;
    }

    public void Show(Vector3 origin, Vector3 direction)
    {
        _timer = displayTime;
        DrawShape(origin, direction);
    }

    private void Update()
    {
        _timer -= Time.deltaTime;

        float alpha = Mathf.Clamp01(_timer / displayTime);
        _lineRenderer.startColor = new Color(effectColor.r, effectColor.g, effectColor.b, alpha);
        _lineRenderer.endColor = new Color(effectColor.r, effectColor.g, effectColor.b, alpha);

        if (_timer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void DrawShape(Vector3 origin, Vector3 direction)
    {
        int circleSegments = 20;
        int fanSegments = 20;
        int totalPoints = circleSegments + 1 + fanSegments + 1 + 1;
        _lineRenderer.positionCount = totalPoints;

        // XZ平面での角度を計算
        float baseAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        int index = 0;

        // 後ろの円を描く
        for (int i = 0; i < circleSegments; i++)
        {
            float angle = (360f / circleSegments) * i * Mathf.Deg2Rad;
            Vector3 point = origin + new Vector3(
                Mathf.Cos(angle) * backCircleRadius,
                origin.y,
                Mathf.Sin(angle) * backCircleRadius
            );
            _lineRenderer.SetPosition(index++, point);
        }

        // 扇の左端へ
        float leftAngle = (baseAngle + fanAngle / 2f) * Mathf.Deg2Rad;
        Vector3 leftEdge = origin + new Vector3(
            Mathf.Sin(leftAngle) * fanRange,
            origin.y,
            Mathf.Cos(leftAngle) * fanRange
        );
        _lineRenderer.SetPosition(index++, leftEdge);

        // 扇の弧を描く
        for (int i = 0; i <= fanSegments; i++)
        {
            float t = (float)i / fanSegments;
            float angle = (baseAngle + fanAngle / 2f - fanAngle * t) * Mathf.Deg2Rad;
            Vector3 point = origin + new Vector3(
                Mathf.Sin(angle) * fanRange,
                origin.y,
                Mathf.Cos(angle) * fanRange
            );
            _lineRenderer.SetPosition(index++, point);
        }

        // 扇の右端を円に戻す
        float rightAngle = (baseAngle - fanAngle / 2f) * Mathf.Deg2Rad;
        Vector3 rightEdge = origin + new Vector3(
            Mathf.Sin(rightAngle) * fanRange,
            origin.y,
            Mathf.Cos(rightAngle) * fanRange
        );
        _lineRenderer.SetPosition(index++, rightEdge);
    }
}
