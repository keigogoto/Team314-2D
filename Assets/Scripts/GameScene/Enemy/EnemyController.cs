//--------------------------------------
//
//  EnemyController.cs
//
//  概要
//  敵の挙動を制御するスクリプト
//
//  更新履歴
//
//  2026/04/27  作成
//              敵をプレイヤーのタグを持っているオブジェクトに向かわせるように。
//              敵がプレイヤーに当たった時にプレイヤーにダメージを与えるようにした。
//
//  2026/04/28  敵がプレイヤーの攻撃に当たった時に吹き飛び、画面外に行ったときに消滅するようになった
//                  敵の吹っ飛ばす速度はPlayerAttack.csのknockBackForceで変更
//
//  2026/05/04  ノックバック中に他の敵と衝突しないようレイヤーを切り替えるように
//
//  2026/05/10  敵にHPとレベルを追加
//
//  2026/05/18  プレイヤーの攻撃が当たってHPが0になったときに画面端に行くまで吹き飛び続けるように変更。
//              ExperienceManagerにヒットした数を渡していたので倒した数を渡すように変更。
//              敵を倒したときに回転しながら飛んでいくのを追加
//
//  2026/05/20  敵がその辺をふらつくように、またプレイヤータグを持っているのを追いかけていたのを
//              プレイヤータグを持っているのが近づいてきたときに、追いかけ始めるように変更
//
//--------------------------------------
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem.Processors;
using UnityEngine.AI; 
[RequireComponent(typeof(NavMeshAgent))] 
public class EnemyController : MonoBehaviour
{
    [Header("敵のステータス設定")]
    [Tooltip("死んだときのSE")] 
    [SerializeField] private AudioClip deathSound; 
    private AudioSource _audioSource;
    [Tooltip("敵の移動速度")]
    [SerializeField] private float moveSpeed;       //  移動速度
    [Tooltip("プレイヤーに接触したときにプレイヤーに与えるダメージ")]
    [SerializeField] private int attackDamage;      //  接触時のダメージ
    [Tooltip("敵の攻撃インターバル")]
    [SerializeField] private float attackInterval;  //  攻撃間隔
    [Tooltip("敵の最大HP")]
    [SerializeField] private float maxHp;           //  敵の最大HP
    [Tooltip("敵の最大レベル")]
    [SerializeField] private int level;             //  敵のレベル
    [Tooltip("敵の吹き飛ばしたときの回転力")]
    [SerializeField] private float rotationForce = 5f;     // 敵が吹き飛ぶ時の回転力

    [Header("UI設定")]
    [Tooltip("生成するDamageCanvasプレハブ")]
    [SerializeField] private GameObject damageUIPrefab;

    [Header("敵のさまよう・索敵関係")]
    [Tooltip("プレイヤーを発見する距離")]
    [SerializeField] private float detectionRange = 5f;     // プレイヤーを発見する距離
    [Tooltip("敵が初期沸き位置からさまよう範囲")]
    [SerializeField] private float wanderRadius = 3f;       // さまよう範囲
    [Tooltip("次の目標地点を決める間隔")]
    [SerializeField] private float wanderInterval = 2f;     // 次の目標地点を決める間隔

    [Tooltip("プレイヤーの方を向く回転速度")] 
    [SerializeField] private float rotationSpeed = 10f;

    private NavMeshAgent _agent; 
    private float _attackTimer;
    private bool _isKnockedBack; // 吹き飛んでいるか
    private float _currentHp;    // 敵の現在HP
    private Transform _player;
    private Rigidbody _rb;
    private PlayerHealth _playerHp;
    private float _stopDistance;
    private bool _isDead;
    private bool _isFalling;
    public float MaxHp => maxHp;    // 外部からmaxHpを参照用

    //  敵集団管理用
    private bool _isDiscovered;                             // プレイヤーを発見しているか
    private Vector3 _wanderTarget;                          // さまよう目標地点
    private float _wanderTimer;                             // さまようタイマー
    private float _currentMoveSpeed;                        // 現在の移動速度（グループから設定される)


    private int _defaultLayer;
    private int _knockbackLayer;

    public bool IsFalling => _isFalling;    //外部から落下中か参照用

    public int Level => level;                      // Experienceからレベルを参照用
    public float GetBaseSpeed() => moveSpeed;   // 基本速度を返す

   
    public float GetMaxHP() => maxHp;
    public float GetCurrentHP() => _currentHp;
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _agent = GetComponent<NavMeshAgent>();
        _audioSource = GetComponent<AudioSource>();
        _currentHp = maxHp;                          // 敵の最大HP

        // AIの速度を設定
        if (_agent != null)
        {
            _agent.speed = moveSpeed;
        }

        Collider enemyCol = GetComponent<Collider>();
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            _player = playerObject.transform;
            _playerHp = playerObject.GetComponent<PlayerHealth>();
            Collider playerCol = playerObject.GetComponent<Collider>();
            _stopDistance = enemyCol.bounds.extents.x + playerCol.bounds.extents.x;

            if (_agent != null) _agent.stoppingDistance = _stopDistance;
        }


        _defaultLayer = gameObject.layer;
        _knockbackLayer = LayerMask.NameToLayer("EnemyKnockback");
        Physics.IgnoreLayerCollision(_knockbackLayer, _defaultLayer, true);
        Physics.IgnoreLayerCollision(_knockbackLayer, _knockbackLayer, true);

        _currentMoveSpeed = moveSpeed;
        _wanderTarget = GetNewWanderTarget();

        if (_agent != null)
        {
            _agent.updatePosition = false;
            _agent.updateRotation = false;
        }

        _agent.enabled = true;
        
    }

    void Update()
    {
        //未発見の時だけシャウトオブジェクトの接近を監視
        if (!_isDiscovered)
        {
            CheckShoutHit();
        }
    }

    void FixedUpdate()
    {
        if (_player == null || _agent == null) return;
        if (_isFalling) return;
        if (_playerHp != null && _playerHp.IsDead)
        {
            if (_agent.isOnNavMesh) _agent.isStopped = true;
            _rb.linearVelocity = Vector3.zero;
            return;
        }
        if (_isKnockedBack) return;

        // 未発見なら自動発見チェック
        if (!_isDiscovered)
        {
            // 所属グループを取得
            EnemyGroupController group = GetComponentInParent<EnemyGroupController>();
            bool isShoutOnly = group != null && group.IsShoutOnly;

            Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1.5f); // 判定半径1.5m
            foreach (var hit in hitColliders)
            {
                if (hit.GetComponent<ShoutProjectile>() != null || hit.GetComponentInParent<ShoutProjectile>() != null)
                {
                    if (group != null)
                    {
                        group.AlertGroup(); // グループ全体を発見状態にする
                    }
                    else
                    {
                        SetDiscovered(true); // 単体の場合は自分を発見状態にする
                    }
                    break;
                }
            }

            // ② プレイヤーとの距離による自動発見チェック（shoutOnly でない場合のみ実行）
            if (!_isDiscovered && !isShoutOnly)
            {
                float distToPlayer = Vector3.Distance(_rb.position, _player.position);
                if (distToPlayer <= detectionRange)
                {
                    SetDiscovered(true);
                }
            }
        }

        if (_isDiscovered)
        {
            Chase();
        }
        else
        {
            Wander();
        }
    }

    private void CheckShoutHit()
    {
        ShoutProjectile[] shouts = FindObjectsByType<ShoutProjectile>(FindObjectsSortMode.None);
        foreach (var shout in shouts)
        {
            // XZ平面での距離を計算（Y軸のズレを無視）
            Vector3 enemyPos = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 shoutPos = new Vector3(shout.transform.position.x, 0f, shout.transform.position.z);

            if (Vector3.Distance(enemyPos, shoutPos) <= 3.0f) // 半径3m以内に入ったら反応
            {
                EnemyGroupController group = GetComponentInParent<EnemyGroupController>();
                if (group != null)
                {
                    group.AlertGroup();
                }
                else
                {
                    SetDiscovered(true);
                }
                break;
            }
        }
    }

    // 追跡処理（既存のFixedUpdateの移動処理を移動）
    private void Chase()
    {
        if (_agent.isOnNavMesh)
        {
            _agent.stoppingDistance = _stopDistance;
            _agent.SetDestination(_player.position);

            
            Vector3 targetDirection = _agent.steeringTarget - transform.position;
            targetDirection.y = 0; // 上下方向の移動は無視する

            Vector3 direction = targetDirection.normalized;

            // 障害物を避ける方向へ、Rigidbodyの物理で実際に移動させる
            Vector3 newPosition = _rb.position + direction * _currentMoveSpeed * Time.fixedDeltaTime;
            _rb.MovePosition(newPosition);
        }

        Vector3 lookAtPlayerDir = _player.position - transform.position;
        lookAtPlayerDir.y = 0; // 地面に対して水平に回転させる（お辞儀や傾きを防止）

        if (lookAtPlayerDir != Vector3.zero)
        {
            // transform.rotation を直接更新して滑らかに回転させる
            Quaternion targetRotation = Quaternion.LookRotation(lookAtPlayerDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    // さまよう処理
    private void Wander()
    {
        if (_agent.isOnNavMesh)
        {
            _agent.stoppingDistance = 0f;
            _agent.SetDestination(_wanderTarget);

            
            Vector3 targetDirection = _agent.steeringTarget - transform.position;
            targetDirection.y = 0; // 上下方向の移動は無視する

            Vector3 direction = targetDirection.normalized;

            // 障害物を避ける方向へ、Rigidbodyの物理で実際に移動させる
            Vector3 newPosition = _rb.position + direction * _currentMoveSpeed * Time.fixedDeltaTime;
            _rb.MovePosition(newPosition);
        }
    }

    // さまよう目標地点をランダムに決める
    private Vector3 GetNewWanderTarget()
    {
        _wanderTimer = wanderInterval;
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        return _rb.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
    }

    // グループから発見状態を設定する
    public void SetDiscovered(bool discovered)
    {
        // 変化がないときは通知しない
        if (_isDiscovered == discovered) return;

        _isDiscovered = discovered;

        if (discovered)
        {
            EnemyManager.Instance?.NotifyDiscovered();
        }
        else
        {
            EnemyManager.Instance?.NotifyLost();
        }
    }

    // グループから速度を設定する
    public void SetMoveSpeed(float speed)
    {
        _currentMoveSpeed = speed;
    }

    public bool IsDiscovered => _isDiscovered;  // グループから参照用

    private void OnCollisionStay(Collision collision)
    {

        if (_isDead) return;

        if (!collision.gameObject.CompareTag("Player")) return;

        //PlayerHealth colPlayerHp = collision.gameObject.GetComponent<PlayerHealth>();
        PlayerHealth colPlayerHp = collision.gameObject.GetComponentInParent<PlayerHealth>();
        if (colPlayerHp != null)
        {
            colPlayerHp.TakeDamage(attackDamage);
        }
    }

    public bool TakeDamage(float damage)
    {
        if (_isDead) return false;
        _currentHp -= damage;

        ShowDamageUI((int)damage);

        if (_currentHp <= 0)
        {
            Die();
            return true;    // 死亡
        }
        return false;       // 生存
    }

    private void ShowDamageUI(int damage)
    {
        if (damageUIPrefab == null) return;

        
        Vector3 spawnPosition = transform.position + Vector3.up * 1.5f;

       
        spawnPosition += new Vector3(Random.Range(-0.2f, 0.2f), 0, Random.Range(-0.2f, 0.2f));

        GameObject uiObj = Instantiate(damageUIPrefab, spawnPosition, Quaternion.identity);
        DamageUI damageUI = uiObj.GetComponent<DamageUI>();
        if (damageUI != null)
        {
            damageUI.Setup(damage);
        }
    }

    private void Die()
    {
        _isDead = true;

        if (_audioSource != null && deathSound != null)
        {
            _audioSource.PlayOneShot(deathSound);
        }

        if (_agent.isOnNavMesh) _agent.isStopped = true;

        // 発見状態だった場合は通知
        if (_isDiscovered)
        {
            _isDiscovered = false;
            EnemyManager.Instance?.NotifyLost();
        }

        foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;

        // 1.5秒後にこのオブジェクトを完全に消去（SEが鳴り終わるのを待つ）
        Destroy(gameObject, 1.5f);

    }

    public void KnockBack(Vector3 direction, float force)
    {
        _isKnockedBack = true;
        if (_agent.isOnNavMesh) _agent.isStopped = true;
        _rb.linearVelocity = Vector3.zero;
        _rb.freezeRotation = false;
        _rb.linearDamping = 0f;    // ノックバック中はDampingをオフ
        _rb.AddForce(direction * force, ForceMode.Impulse);
        _rb.AddTorque(Vector3.up * rotationForce, ForceMode.Impulse);
        StartCoroutine(KnockBackCoroutine());
    }

    public void SmallKnockBack(Vector3 direction, float force)
    {
        if (_isDead) return;
        StartCoroutine(SmallKnockBackCoroutine(direction, force));
    }

    private IEnumerator SmallKnockBackCoroutine(Vector3 direction, float force)
    {
        _isKnockedBack = true;
        if (_agent.isOnNavMesh) _agent.isStopped = true;
        _rb.AddForce(direction * force, ForceMode.Impulse);
        yield return new WaitForSeconds(0.1f);  // 追跡を止める時間（Inspectorで調整できないので短めに固定）
        _isKnockedBack = false;
        if (_agent.isOnNavMesh && !_isDead) _agent.isStopped = false;
    }

    private IEnumerator KnockBackCoroutine()
    {
        gameObject.layer = _knockbackLayer;
        yield return new WaitForSeconds(0.3f);

        if (!_isDead)
        {
            _isKnockedBack = false;
            _rb.freezeRotation = true;
            _rb.rotation = Quaternion.identity;
            _rb.linearDamping = 10f;    // 元に戻す
            gameObject.layer = _defaultLayer;
            if (_agent.isOnNavMesh) _agent.isStopped = false;
        }
    }

    private IEnumerator StunCoroutine(float duration)
    {
        _rb.linearVelocity = Vector3.zero;
        yield return new WaitForSeconds(duration);
    }

    // OnBecameInvisible：死亡時も消えるように条件を変更
    private void OnBecameInvisible()
    {
        if (_isKnockedBack || _isDead)
        {
            Destroy(gameObject);
        }
    }

    public void SetTarget(GameObject target)
    {
        if (target != null)
        {
            _player = target.transform;
        }
    }

    // グループから目標地点を受け取る
    public void SetWanderTarget(Vector3 target)
    {
        if (_isFalling || _isDead) return;
        _wanderTarget = target;
    }

    public void FallOff()
    {

        if (_isFalling) return;
        if (!_isDead && !_isKnockedBack) return;
        _isFalling = true;
        _isDead = true;
        _isKnockedBack = false;
        StopAllCoroutines();

        if (_agent != null) _agent.enabled = false;

        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic = true;
        _rb.freezeRotation = false;

        StartCoroutine(FallAndDestroy());

    }

    private IEnumerator FallAndDestroy()
    {
        float duration = 1.5f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 startPos = transform.position;  // Vector2で管理（Z軸を触らない）

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // イーズイン：最初はゆっくり、だんだん加速
            float eased = t * t;

            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            // Vector2で操作してZ軸を保持
            transform.position = new Vector3(
                startPos.x,
                startPos.y - eased * 3f,
                transform.position.z
            );

            yield return null;
        }

        Destroy(gameObject);
    }

    public void SetMaxHP(float newMaxHp)
    {
        maxHp = newMaxHp;
        _currentHp = newMaxHp;
    }

}
