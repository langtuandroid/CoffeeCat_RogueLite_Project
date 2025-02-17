using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CoffeeCat.Datas;
using CoffeeCat.FrameWork;
using CoffeeCat.RogueLite;
using CoffeeCat.Utils;
using CoffeeCat.Utils.Defines;
using Sirenix.OdinInspector;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using UnityEngine.Events;
using ResourceManager = CoffeeCat.FrameWork.ResourceManager;

namespace CoffeeCat
{
    public partial class Player : MonoBehaviour
    {
        [Title("Status")]
        [SerializeField] protected PlayerAddressablesKey playerName = PlayerAddressablesKey.NONE;
        [SerializeField] protected PlayerAddressablesKey normalAttackProjectile = PlayerAddressablesKey.NONE;
        [SerializeField] protected PlayerLevelData playerLevelData = null;
        [ShowInInspector, ReadOnly] protected PlayerStat stat;

        [Title("Transform")]
        [SerializeField] protected Transform tr = null;
        [SerializeField] protected Transform projectileTr = null;

        private PlayerActiveSkill normalAttackData = null;
        private Rigidbody2D rigid = null;
        private bool isPlayerInBattle = false;
        private bool hasFiredProjectile = false;
        private bool isPlayerDamaged = false;
        private bool isInvincible = false;
        private bool isDead = false;

        // Property
        public Transform Tr => tr;
        public Transform ProjectileTr => projectileTr;
        public PlayerStat Stat => stat;

        private void Start()
        {
            rigid = GetComponent<Rigidbody2D>();
            normalAttackData = DataManager.Inst.PlayerActiveSkills.DataDictionary[(int)normalAttackProjectile];
            playerLevelData.Initialize();
            
            LoadResources();
            SetStat();
            // NormalAttack();
            CheckInvincibleTime();
            
            

            StageManager.Inst.AddListenerMonsterKilledByPlayer(GetExp);
            StageManager.Inst.AddListenerRoomFirstEnteringEvent(PlayerEnteredRoom);
            StageManager.Inst.AddListenerClearedRoomEvent(PlayerClearedRoom);
        }

        private void OnEnable() 
        {
            InputManager.BindDirectionInputUpdateEvent(Move);
            InputManager.BindDirectionInputEndEvent(MoveEnd);
        }

        private void OnDisable() 
        {
            InputManager.ReleaseDirectionInputUpdateEvent(Move);
            InputManager.ReleaseDirectionInputEndEvent(MoveEnd);
        }

        private void LoadResources()
        {
            SafeLoader.Regist(normalAttackProjectile.ToStringEx(), onCompleted: completed =>
            {
                if (!completed)
                    CatLog.WLog($"{normalAttackProjectile.ToStringEx()} Load Failed");
            });
            
            SafeLoader.Regist("LevelUp", onCompleted: completed =>
            {
                if (!completed)
                    CatLog.WLog($"{normalAttackProjectile.ToStringEx()} Load Failed");
            });
        }

        private void SetStat()
        {
            stat = DataManager.Inst.PlayerStats.DataDictionary[playerName.ToStringEx()];
            stat.SetCurrentHp();
            
            StageManager.Inst.InvokeIncreasePlayerHP(stat.CurrentHp, stat.MaxHp);
        }

        private void Move(Vector2 direction) 
        {
            if (isDead)
                return;
            
            rigid.velocity = new Vector2(direction.x, direction.y) * stat.MoveSpeed;
            
            if (isPlayerInBattle) 
                return;

            if (direction.x != 0f || direction.y != 0f) 
            {
                SwitchingPlayerDirection(rigid.velocity.x < 0);    
            }
        }

        private void MoveEnd() 
        {
            rigid.velocity = Vector2.zero;
        }

        private void SwitchingPlayerDirection(bool isSwitching)
        {
            // Default Direction is Right
            // isSwitching : true -> Left, false -> Right
            var lossyScale = tr.lossyScale;
            tr.localScale = isSwitching switch
            {
                true  => new Vector3(-2f, lossyScale.y, lossyScale.z),
                false => new Vector3(2f, lossyScale.y, lossyScale.z)
            };
        }

        private void NormalAttack()
        {
            var currentCoolTime = normalAttackData.SkillCoolTime;

            this.UpdateAsObservable()
                .Skip(TimeSpan.Zero)
                .Where(_ => isPlayerInBattle && !isDead)
                .Select(_ => currentCoolTime += Time.deltaTime)
                .Where(_ => currentCoolTime >= normalAttackData.SkillCoolTime)
                .Subscribe(_ =>
                {
                    var targetMonster = FindNearestMonster();

                    if (targetMonster == null) return;

                    // Battle 중에는 Player의 방향을 기본공격의 타겟 방향으로 전환
                    var targetMonsterStatus = targetMonster.GetComponent<MonsterStatus>();
                    var targetDirection = targetMonsterStatus.GetCenterTr().position - projectileTr.position;
                    targetDirection = targetDirection.normalized;
                    SwitchingPlayerDirection(targetDirection.x < 0 ? true : false);

                    // 기본 공격 Projectile 스폰 및 발사
                    var spawnObj =
                        ObjectPoolManager.Inst.Spawn(normalAttackProjectile.ToStringEx(), projectileTr.position);
                    var projectile = spawnObj.GetComponent<PlayerNormalProjectile>();
                    projectile.Fire(stat, normalAttackData, projectileTr.position, targetDirection);

                    // 쿨타임 초기화 및 발사 여부
                    currentCoolTime = 0;
                    hasFiredProjectile = true;
                }).AddTo(this);

            Transform FindNearestMonster()
            {
                Collider2D[] result = new Collider2D[Defines.SPAWN_MONSTER_MAX_COUNT];
                var count = Physics2D.OverlapCircleNonAlloc
                    (Tr.position, normalAttackData.SkillRange, result, 1 << LayerMask.NameToLayer("Monster"));

                if (count <= 0) return null;

                var target = result.Where(Collider2D => Collider2D != null)
                                   .Select(Collider2D => Collider2D.GetComponent<MonsterStatus>())
                                   .Where(monster => monster.IsAlive)
                                   .OrderBy(monster => Vector2.Distance(projectileTr.position,
                                                                        monster.GetCenterTr().position))
                                   .FirstOrDefault();

                return target == null ? null : target.transform;
            }
        }

        private void CheckInvincibleTime()
        {
            this.ObserveEveryValueChanged(_ => isPlayerDamaged)
                .Skip(TimeSpan.Zero)
                .Where(_ => isPlayerDamaged)
                .Subscribe(_ =>
                {
                    isInvincible = true;
                    Observable.Timer(TimeSpan.FromSeconds(stat.InvincibleTime))
                              .Subscribe(__ => isInvincible = false);
                }).AddTo(this);
        }
        
        private void GetExp(float exp) 
        {
            playerLevelData.AddExp(exp);
            if (!playerLevelData.isReadyLevelUp()) 
                return;
            
            playerLevelData.LevelUp();
            var levelUpEffect = ObjectPoolManager.Inst.Spawn("LevelUp", tr);
            levelUpEffect.transform.localPosition = Vector3.zero;

            Observable.Timer(TimeSpan.FromSeconds(2.5f))
                      .Subscribe(_ => { EnableSkillSelect(); }).AddTo(this);
            
            StageManager.Inst.InvokeIncreasePlayerExp(playerLevelData.GetCurrentExp(), playerLevelData.GetExpToNextLevel());
        }

        private void OnDead()
        {
            rigid.velocity = Vector2.zero;
            StageManager.Inst.OnPlayerKilled.Invoke();
            isDead = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Monster와 충돌
            if (other.gameObject.TryGetComponent(out MonsterStatus monsterStat))
            {
                var damageData = DamageData.GetData(monsterStat.CurrentStat, stat);
                OnDamaged(damageData);
            }
        }

        private void PlayerEnteredRoom(RoomDataStruct roomData)
        {
            switch (roomData.RoomType)
            {
                case RoomType.PlayerSpawnRoom:
                    break;
                case RoomType.MonsterSpawnRoom:
                    isPlayerInBattle = true;
                    break;
                case RoomType.ShopRoom:
                    break;
                case RoomType.BossRoom:
                    break;
                case RoomType.RewardRoom:
                    break;
                case RoomType.EmptyRoom:
                    break;
                case RoomType.ExitRoom:
                    break;
            }
        }

        private void PlayerClearedRoom(RoomDataStruct roomData)
        {
            switch (roomData.RoomType)
            {
                case RoomType.MonsterSpawnRoom:
                    isPlayerInBattle = false;
                    break;
                case RoomType.BossRoom:
                    break;
            }
        }

        #region Public Methods

        public bool IsWalking() => rigid.velocity.x != 0 || rigid.velocity.y != 0;

        public void StartAttack() => hasFiredProjectile = true;

        public bool IsAttacking() => hasFiredProjectile;

        public void FinishAttackAnimation() => hasFiredProjectile = false;

        public bool IsDamaged() => isPlayerDamaged;

        public void FinishHitAnimation() => isPlayerDamaged = false;

        public bool IsDead() => isDead;

        public void UpdateStat()
        {
            // UI 에서 해줄 것
            /// 사용자의 입력을 받아서 enhanceData 객체 생성
            /// var enhance = new PlayerEnhanceData();
            /// enhance.MaxHp = 100;
            /// 만들어진 객체를 enhanceData.SaveEnhanceData()로 저장
            /// enhance.SaveEnhanceData();
            /// 사용자가 (스탯 포인트 등을 가지고) 강화를 확정하면 UpdateStat() 호출

            var enhanceData = PlayerEnhanceData.GetEnhanceData();
            stat.StatEnhancement(enhanceData);
        }

        public void OnDamaged(DamageData damageData)
        {
            if (isInvincible)
                return;
            
            var calculatedDamage = damageData.CalculatedDamage;
            stat.CurrentHp -= calculatedDamage;
            DamageTextManager.Inst.OnFloatingText(calculatedDamage, tr.position, true);
            isPlayerDamaged = true;

            if (stat.CurrentHp <= 0)
            {
                stat.CurrentHp = 0;
                OnDead();
            }
            
            StageManager.Inst.InvokeDecreasePlayerHP(stat.CurrentHp, stat.MaxHp);
        }
        
        #endregion
    }
}