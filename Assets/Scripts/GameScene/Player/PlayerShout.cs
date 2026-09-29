//-------------------------------------------------------
//
//  PlayerShout.cs
//
//  概要
//  プレイヤーの叫び声を飛ばすスクリプト
//  声に当たった敵グループをプレイヤー発見状態にする
//
//  更新履歴
//
//  2026/05/24  作成
//  2026/07/06  3D対応。
//
//-------------------------------------------------------
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class PlayerShout : MonoBehaviour
{
    [Tooltip("声の見た目に使うPrefab")]
    [SerializeField] private GameObject shoutPrefab;
    [Tooltip("クールタイム")]
    [SerializeField] private float shoutCoolTime = 3f;

    private PlayerController _playerController;
    private float _coolTimer;
    private GameObject _visualEffectObject;
    private ShoutVisualEffect _visualEffect;

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        _visualEffectObject = new GameObject("ShoutVisualEffect");
        _visualEffect = _visualEffectObject.AddComponent<ShoutVisualEffect>();
    }

    private void Update()
    {
        if (_coolTimer > 0f)
        {
            _coolTimer -= Time.deltaTime;
        }
    }

    private void OnShout(InputValue value)
    {
        if (_coolTimer > 0f) return;
        _coolTimer = shoutCoolTime;

        Vector3 direction = _playerController.LastMoveDirection;

        // XZ平面での角度を計算
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        GameObject shout = Instantiate(shoutPrefab, transform.position, Quaternion.Euler(0, angle, 0));
        shout.GetComponent<ShoutProjectile>().SetDirection(direction);

        // エフェクト表示
        GameObject effectObj = new GameObject("ShoutVisualEffect");
        ShoutVisualEffect effect = effectObj.AddComponent<ShoutVisualEffect>();
        effect.Show(transform.position, direction);

        Debug.Log("Shout!");
    }
}
