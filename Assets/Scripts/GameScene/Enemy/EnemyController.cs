//--------------------------------------
//
//  EnemyController.cs
//
//  概要
//  敵の挙動を制御するスクリプト
//
//--------------------------------------
using UnityEngine;
using System.Collections;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("敵のステータス設定")]
    [Tooltip("死んだときのSE")]
    [SerializeField] private AudioClip deathSound;
    private AudioSource _audioSource;
    [Tooltip("敵の移動速度")]
    [SerializeField] private float moveSpeed;
    [Tooltip("プレイヤーに接触したときにプレイヤーに与えるダメージ")]
    [SerializeField] private int attackDamage;
    [Tooltip("敵の攻撃インターバル")]
    [SerializeField] private float attackInterval;
    [Tooltip("敵の最大HP")]
    [SerializeField] private float maxHp;
    [Tooltip("敵の最大レベル")]
    [SerializeField] private int level;
    [Tooltip("敵の吹き飛ばしたときの回転力")]
    [SerializeField] private float rotationForce = 5f;

    [Header("UI設定")]
    [Tooltip("生成するDamageCanvasプレハブ")]
    [SerializeField] private GameObject damageUIPrefab;

    [Header("敵のさまよう・索敵関係")]
    [Tooltip("プレイヤーを発見する距離")]
    [SerializeField] private float detectionRange = 5f;
    [Tooltip("敵が初期沸き位置からさまよう範囲")]
    [SerializeField] private float wanderRadius = 3f;
    [Tooltip("次の目標地点を決める間隔")]
    [SerializeField] private float wanderInterval = 2f;

    [Tooltip("プレイヤーの方を向く回転速度")]
    [SerializeField] private float rotationSpeed = 10f;

    private NavMeshAgent _agent;
    private float _attackTimer;
    private bool _isKnockedBack;
    private float _currentHp;
    private Transform _player;
    private Rigidbody _rb;
    private PlayerHealth _playerHp;
    private float _stopDistance;
    private bool _isDead;
    private bool _isFalling;
    public float MaxHp => maxHp;

    //  敵集団管理用
    private bool _isDiscovered;
    private Vector3 _wanderTarget;
    private float _wanderTimer;
    private float _currentMoveSpeed;

    private int _defaultLayer;
    private int _knockbackLayer;

    public bool IsFalling => _isFalling;
    public int Level => level;
    public float GetBaseSpeed() => moveSpeed;

    public float GetMaxHP() => maxHp;
    public float GetCurrentHP() => _currentHp;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _agent = GetComponent<NavMeshAgent>();
        _audioSource = GetComponent<AudioSource>();
        _currentHp = maxHp;

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

        // 未発見ならプレイヤーとの距離で自動発見チェック
        if (!_isDiscovered)
        {
            float distToPlayer = Vector3.Distance(_rb.position, _player.position);
            if (distToPlayer <= detectionRange)
            {
                SetDiscovered(true);
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

    private void Chase()
    {
        if (_agent.isOnNavMesh)
        {
            _agent.stoppingDistance = _stopDistance;
            _agent.SetDestination(_player.position);

            Vector3 targetDirection = _agent.steeringTarget - transform.position;
            targetDirection.y = 0;

            Vector3 direction = targetDirection.normalized;
            Vector3 newPosition = _rb.position + direction * _currentMoveSpeed * Time.fixedDeltaTime;
            _rb.MovePosition(newPosition);
        }

        Vector3 lookAtPlayerDir = _player.position - transform.position;
        lookAtPlayerDir.y = 0;

        if (lookAtPlayerDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookAtPlayerDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void Wander()
    {
        if (_agent.isOnNavMesh)
        {
            _agent.stoppingDistance = 0f;
            _agent.SetDestination(_wanderTarget);

            Vector3 targetDirection = _agent.steeringTarget - transform.position;
            targetDirection.y = 0;

            Vector3 direction = targetDirection.normalized;
            Vector3 newPosition = _rb.position + direction * _currentMoveSpeed * Time.fixedDeltaTime;
            _rb.MovePosition(newPosition);
        }
    }

    private Vector3 GetNewWanderTarget()
    {
        _wanderTimer = wanderInterval;
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        return _rb.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
    }

    public void SetDiscovered(bool discovered)
    {
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

    public void SetMoveSpeed(float speed)
    {
        _currentMoveSpeed = speed;
    }

    public bool IsDiscovered => _isDiscovered;

    private void OnCollisionStay(Collision collision)
    {
        if (_isDead) return;
        if (!collision.gameObject.CompareTag("Player")) return;

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
            return true;
        }
        return false;
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

        if (_isDiscovered)
        {
            _isDiscovered = false;
            EnemyManager.Instance?.NotifyLost();
        }

        foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;

        Destroy(gameObject, 1.5f);
    }

    public void KnockBack(Vector3 direction, float force)
    {
        _isKnockedBack = true;
        if (_agent.isOnNavMesh) _agent.isStopped = true;
        _rb.linearVelocity = Vector3.zero;
        _rb.freezeRotation = false;
        _rb.linearDamping = 0f;
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
        yield return new WaitForSeconds(0.1f);
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
            _rb.linearDamping = 10f;
            gameObject.layer = _defaultLayer;
            if (_agent.isOnNavMesh) _agent.isStopped = false;
        }
    }

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
        Vector3 startPos = transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float eased = t * t;

            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
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
