using UnityEngine;
using Sirenix.OdinInspector;
using CoffeeCat.FrameWork;
using CoffeeCat.Datas;
using CoffeeCat.Utils;
using CoffeeCat.Utils.Defines;

// NOTE: ���� �̺�Ʈ(�浹, ������, �ð����)�� ���� �߰����� ����ü�� �����ϴ� AfterTask ����
namespace CoffeeCat {
    public class MonsterSkillProjectile : MonsterProjectile {
        [Title("Skill Projectile Datas")]
        [SerializeField] protected ProjectileSkillKey skillKey = ProjectileSkillKey.NONE;
        [SerializeField, ReadOnly] protected MonsterSkillStat skillData = null;

        [Title("Spawn Additional Projectile")]
        [SerializeField] protected AfterTask[] tasks = null;

        #region ADDITIONAL_PROJECTILE_SPANW

        [System.Serializable]
        public class AfterTask {
            public enum ActiveCondition {
                NONE,
                INITIALIZED,
                TIMER,
                ACTIVE,
                COLLISION,
                ATTACKPLAYER
            }

            public ActiveCondition Cond { get; private set; } = ActiveCondition.NONE;
            
            // �߰����� ����ü ���� Ű
            //[SerializeField] private ProjectileSkillKey Key = ProjectileSkillKey.NONE;

            public void Active() {
                // Active..
            }
        }

        private void ActiveAfterTask(AfterTask.ActiveCondition condition) {
            for (int i = 0; i < tasks.Length; i++) {
                if (tasks[i].Cond.Equals(condition)) {
                    tasks[i].Active();
                }
            }
        }

        #endregion

        protected override void SetBaseComponents() {
            base.SetBaseComponents();

            // Get SkillData in DataManager's Dictionary
            if (skillData == null) {
                if (DataManager.Instance.MonsterSkills.DataDictionary.TryGetValue(skillKey.ToString(), out MonsterSkillStat result) == false) {
                    CatLog.ELog($"Not Found Monster Skill Data. Name: {skillKey.ToString()}");
                }
                this.skillData = result;
            }
        }

        public override void Initialize(MonsterStat monsterStatData) {
            base.Initialize(monsterStatData);
        }

        #region COLLISION_BASE

        protected override void OnCollisionEnterWithPlayer(Collision2D playerCollision) {
            base.OnCollisionEnterWithPlayer(playerCollision);
        }

        protected override void OnCollisionEnterWithTargetLayer(Collision2D collision) {
            base.OnCollisionEnterWithTargetLayer(collision);
        }

        protected override void OnTriggerEnterWithPlayer(Collider2D playerCollider) {
            base.OnTriggerEnterWithPlayer(playerCollider);
        }

        protected override void OnTriggerEnterWithTargetLayer(Collider2D collider) {
            base.OnTriggerEnterWithTargetLayer(collider);
        }

        #endregion

        #region DAMAGED

        protected override void DamageToPlayer(TempPlayer player, Vector2 collisionPoint, Vector2 collisionDirection) {
            if (statData == null || skillData == null) {
                CatLog.ELog("Monster Projectiles Stat or Skill Data is Null.");
                return;
            }

            var attackData = AttackData.GetMonsterSkillAttackData(statData, skillData, player.Stats);
            player.OnDamaged(attackData, collisionPoint, collisionDirection);
            ObjectPoolManager.Instance.Despawn(this.gameObject);
        }

        #endregion
    }
}
