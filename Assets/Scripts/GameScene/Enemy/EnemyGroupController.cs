//-------------------------------------------------------
//
//  EnemyGroupController.cs
//
//  概要
//  敵の集団を管理するスクリプト
//
//  更新履歴
//
//  2026/05/19  作成
//
//  2026/05/20  集団の数が多いほど移動速度が落ちるように
//
//  2026/07/06  3D対応。Vector2→Vector3に変更。
//
//-------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyGroupController : MonoBehaviour
{
    [Tooltip("範囲内の敵の量で移動速度を変化させる")]
    [SerializeField] private float groupSlowRadius;
    [Tooltip("最大減速倍率")]
    [SerializeField] private float minSpeedMultiplier;
    [Tooltip("最大減速になる敵の数")]
    [SerializeField] private int maxGroupSizeForSlow;
    [Tooltip("敵グループの未発見時歩き回る範囲")]
    [SerializeField] private float groupWanderRadius = 5f;
    [Tooltip("敵グループの次の目標地点を決める間隔")]
    [SerializeField] private float groupWanderInterval = 3f;

    [Header("グループ合流・UI設定")]
    [Tooltip("このグループはシャウトでのみ発見状態になる")]
    [SerializeField] private bool shoutOnly = false;
    [SerializeField] private float mergeRadius = 4f;
    [SerializeField] private TMPro.TextMeshProUGUI countText;
    public List<EnemyController> Enemies => _enemies;
    public bool IsDiscovered => _isDiscovered;

    [Header("グループHPバー設定")]
    [Tooltip("作ったHPバーのCanvasプレハブをここに割り当てます")]
    [SerializeField] private GameObject hpBarPrefab;
    [Tooltip("HPバーのオフセット")]
    [SerializeField] private Vector3 hpBarOffset = new Vector3(0, -1.0f, 0);

    public bool IsShoutOnly => shoutOnly;
    private float _initialGroupMaxHp = 0f;
    private GameObject _spawnedHpBar;
    private Image _hpBarFillImage;

    private Vector3 _groupWanderTarget;                         
    private float _groupWanderTimer;
    private Vector3 _groupOrigin;                               

    private bool _isDiscovered;
    private List<EnemyController> _enemies = new List<EnemyController>();
    private void Start()
    {
        _initialGroupMaxHp = 0f;

        int targetActiveCount = PlayerPrefs.GetInt("NextGroupSize", 4);
        float targetEnemyHp = PlayerPrefs.GetFloat("NextEnemyHP", 10f);
        List<EnemyController> allChildEnemies = new List<EnemyController>(GetComponentsInChildren<EnemyController>());

        for (int i = 0; i < allChildEnemies.Count; i++)
        {
            if (i >= targetActiveCount)
            {
                Destroy(allChildEnemies[i].gameObject);
            }
            else
            {
                _enemies.Add(allChildEnemies[i]);
                if (allChildEnemies[i] != null)
                {
                    allChildEnemies[i].SetMaxHP(targetEnemyHp);
                    _initialGroupMaxHp += allChildEnemies[i].GetMaxHP();
                }
            }
        }

        _groupOrigin = transform.position;
        _groupWanderTarget = GetNewGroupWanderTarget();

        if (hpBarPrefab != null && _enemies.Count > 0)
        {
            _spawnedHpBar = Instantiate(hpBarPrefab, transform);
            Transform healthBarTransform = _spawnedHpBar.transform.Find("Background/HealthBar");
            if (healthBarTransform != null)
            {
                _hpBarFillImage = healthBarTransform.GetComponent<Image>();
            }
        }
    }

    private void Update()
    {
        _enemies.RemoveAll(e => e == null || e.GetCurrentHP() <= 0);

        if (countText != null)
        {
            if (_enemies.Count > 0)
            {
                countText.text = _enemies.Count.ToString();
                countText.transform.position = CalcGroupCenter() + new Vector3(0, 1.5f, 0);
            }
            else countText.text = "";
        }

        if (_enemies.Count == 0)
        {
            if (_spawnedHpBar != null) Destroy(_spawnedHpBar);
            return;
        }

        UpdateGroupHPBar();
        TryMergeWithNearbyGroups();

        if (!_isDiscovered)
        {
            foreach (EnemyController enemy in _enemies)
            {
                if (enemy.IsDiscovered)
                {
                    DiscoverAll();
                    break;
                }
            }
        }

        if (!_isDiscovered)
        {
            _groupWanderTimer -= Time.deltaTime;
            if (_groupWanderTimer <= 0f)
            {
                _groupWanderTarget = GetNewGroupWanderTarget();
            }

            foreach (EnemyController enemy in _enemies)
            {
                enemy.SetWanderTarget(_groupWanderTarget);
            }
        }

        foreach (EnemyController enemy in _enemies)
        {
            int nearbyCount = CountNearbyEnemies(enemy);
            float speedMultiplier = CalcSpeedMultiplier(nearbyCount);
            enemy.SetMoveSpeed(enemy.GetBaseSpeed() * speedMultiplier);
        }

        Vector3 groupCenter = CalcGroupCenter();

        foreach (EnemyController enemy in _enemies)
        {
            if (enemy == null) continue;

            Vector3 toCenter = groupCenter - enemy.transform.position;  
            float distanceToCenter = toCenter.magnitude;

            if (distanceToCenter > 2.0f)
            {
                enemy.SetWanderTarget(groupCenter);
            }
        }
    }

    private Vector3 GetNewGroupWanderTarget()   
    {
        _groupWanderTimer = groupWanderInterval;
        Vector2 randomCircle = Random.insideUnitCircle * groupWanderRadius;
        return _groupOrigin + new Vector3(randomCircle.x, 0f, randomCircle.y);  // XZ平面に変換
    }

    private void DiscoverAll()
    {
        Debug.Log("敵グループ:発見！");
        _isDiscovered = true;
        foreach (EnemyController enemy in _enemies)
        {
            enemy.SetDiscovered(true);
        }
    }

    private int CountNearbyEnemies(EnemyController target)
    {
        int count = 0;
        foreach (EnemyController enemy in _enemies)
        {
            if (enemy == target) continue;
            float dist = Vector3.Distance(
                target.transform.position,
                enemy.transform.position
            );
            if (dist <= groupSlowRadius) count++;
        }
        return count;
    }

    private float CalcSpeedMultiplier(int nearbyCount)
    {
        if (nearbyCount <= 0) return 1f;
        float t = Mathf.Clamp01((float)nearbyCount / maxGroupSizeForSlow);
        return Mathf.Lerp(1f, minSpeedMultiplier, t);
    }

    public void AlertGroup()
    {
        DiscoverAll();
    }

    private void TryMergeWithNearbyGroups()
    {
        EnemyGroupController[] allGroups = FindObjectsByType<EnemyGroupController>(FindObjectsSortMode.None);
        foreach (EnemyGroupController otherGroup in allGroups)
        {
            if (otherGroup == this || otherGroup.Enemies.Count == 0) continue;

            if (this.shoutOnly != otherGroup.shoutOnly) continue;
            //ShoutOnly の設定が違うグループとは合流しない
            float dist = Vector3.Distance(CalcGroupCenter(), otherGroup.CalcGroupCenter());
            if (dist <= mergeRadius)
            {
                if (otherGroup.IsDiscovered && !_isDiscovered)
                {
                    DiscoverAll();
                }

                this._initialGroupMaxHp += otherGroup._initialGroupMaxHp;

                foreach (EnemyController enemy in otherGroup.Enemies)
                {
                    if (enemy == null) continue;
                    enemy.transform.SetParent(this.transform);
                    _enemies.Add(enemy);
                }

                otherGroup.Enemies.Clear();
                if (otherGroup.Enemies.Count == 0)
                {
                    otherGroup.gameObject.SetActive(false);
                    if (otherGroup._spawnedHpBar != null) Destroy(otherGroup._spawnedHpBar);
                }
            }
        }
    }

    public Vector3 CalcGroupCenter()    
    {
        if (_enemies.Count == 0) return transform.position;
        Vector3 sumPosition = Vector3.zero;
        int validCount = 0;
        foreach (EnemyController enemy in _enemies)
        {
            if (enemy != null)
            {
                if (validCount > 0)
                {
                    float distFromCurrentAverage = Vector3.Distance(sumPosition / validCount, enemy.transform.position); 
                    if (distFromCurrentAverage > 10f) continue;
                }

                sumPosition += enemy.transform.position;
                validCount++;
            }
        }
        return validCount > 0 ? sumPosition / validCount : transform.position;
    }

    private void UpdateGroupHPBar()
    {
        _enemies.RemoveAll(enemy => enemy == null || enemy.GetCurrentHP() <= 0);

        if (_enemies.Count == 0)
        {
            if (_spawnedHpBar != null) _spawnedHpBar.SetActive(false);
            return;
        }

        if (_spawnedHpBar == null) return;
        _spawnedHpBar.SetActive(true);

        EnemyController lowestEnemy = null;
        float lowestY = float.MaxValue;

        float currentGroupTotalMaxHp = 0f;
        float currentGroupTotalCurrentHp = 0f;

        Vector3 groupCenter = CalcGroupCenter();

        foreach (EnemyController enemy in _enemies)
        {
            if (enemy == null) continue;

            float distFromCenter = Vector3.Distance(groupCenter, enemy.transform.position);
            if (distFromCenter > 10f) continue;

            if (enemy.transform.position.y < lowestY)
            {
                lowestY = enemy.transform.position.y;
                lowestEnemy = enemy;
            }

            currentGroupTotalMaxHp += enemy.GetMaxHP();
            currentGroupTotalCurrentHp += enemy.GetCurrentHP();
        }

        if (lowestEnemy != null)
        {
            Vector3 enemyPos = lowestEnemy.transform.position;
            Vector3 targetPosition = new Vector3(
                enemyPos.x + hpBarOffset.x,
                enemyPos.y + hpBarOffset.y,
                enemyPos.z + hpBarOffset.z      // Z軸も対応
            );
            _spawnedHpBar.transform.position = targetPosition;
        }

        if (_hpBarFillImage != null && _initialGroupMaxHp > 0)
        {
            _hpBarFillImage.fillAmount = currentGroupTotalCurrentHp / _initialGroupMaxHp;
        }
    }
}
