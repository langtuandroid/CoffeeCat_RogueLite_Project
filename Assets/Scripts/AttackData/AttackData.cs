using UnityEngine;

// NOTE: �ʹ� ���� �Լ��� �����ִ°� ����. �Լ� ȣ�⵵ ��¥�� �ƴ϶� �Դϴ�..
namespace CoffeeCat.Datas {
    // Keep Less Than 16Byte
    public struct AttackData {
        public float CalculatedDamage; // 4B
        public bool IsCritical;        // 1B

        public static AttackData GetMonsterAttackData(MonsterStat attackerStat, TempPlayerStat defenderStat) {
            var attackData = new AttackData() {
                CalculatedDamage = 0f, IsCritical = false
            };

            // ġ��Ÿ �߻� ���
            attackData.IsCritical = (attackerStat.CriticalChance - defenderStat.CriticalResist) >= Random.Range(1f, 100f);

            // �ּ� ~ �ִ� ������ ���� �� ����
            attackData.CalculatedDamage = Random.Range(attackerStat.MinDamage, attackerStat.MaxDamage);

            // ġ��Ÿ �߻� �� �������� ġ��Ÿ ���� ����
            attackData.CalculatedDamage *= (attackData.IsCritical) ? attackerStat.CriticalDamageMultiplier : 1f;

            // ����� ��� ��ġ �� ����� ��ġ ���
            float penetratedArmorValue = defenderStat.Defence - attackerStat.Penetration;
            penetratedArmorValue = (penetratedArmorValue < 0f) ? 0f : penetratedArmorValue;
            attackData.CalculatedDamage -= penetratedArmorValue;

            // ���̳ʽ� ������ ����
            attackData.CalculatedDamage = (attackData.CalculatedDamage < 0f) ? 0f : attackData.CalculatedDamage;
            return attackData;
        }

        public static AttackData GetMonsterSkillAttackData(MonsterStat attackerStat, MonsterSkillStat attackerSkillStat, TempPlayerStat defenderStat) {
            var attackData = new AttackData() { 
                CalculatedDamage= 0f, IsCritical = false
            };

            // ġ��Ÿ �߻� ���
            attackData.IsCritical = (attackerStat.CriticalChance - defenderStat.CriticalResist) >= Random.Range(1f, 100f);

            // �ּ� ~ �ִ� ������ ���� �� ����
            attackData.CalculatedDamage = Random.Range(attackerStat.MinDamage, attackerStat.MaxDamage);

            // *��ų ������ ���� ���� ����
            attackData.CalculatedDamage = attackerSkillStat.Damage + (attackData.CalculatedDamage * attackerSkillStat.Ratio);

            // ġ��Ÿ �߻� �� �������� ġ��Ÿ ���� ����
            attackData.CalculatedDamage *= (attackData.IsCritical) ? attackerStat.CriticalDamageMultiplier : 1f;

            // ����� ��� ��ġ �� ����� ��ġ ���
            float penetratedArmorValue = defenderStat.Defence - attackerStat.Penetration;
            penetratedArmorValue = (penetratedArmorValue < 0f) ? 0f : penetratedArmorValue;
            attackData.CalculatedDamage -= penetratedArmorValue;

            // ���̳ʽ� ������ ����
            attackData.CalculatedDamage = (attackData.CalculatedDamage < 0f) ? 0f : attackData.CalculatedDamage;
            return attackData;
        }

        public static AttackData GetPlayerAttackData(TempPlayerStat playerStat, MonsterStat monsterStat) {
            var attackData = new AttackData() {
                CalculatedDamage = 0f, IsCritical = false
            };
            return attackData;
        }

#if UNITY_EDITOR
        #region DAMAGE_FORMULA
        private void IsCriticalCalc(float attackerCritChangeValue, float defenderCritResistValue) {
            IsCritical = (attackerCritChangeValue - defenderCritResistValue) >= Random.Range(1f, 100f); // ������ ����� ġ��Ÿ �߻� üũ
        }

        private void CalcDamageInRange(float attackerMinDamageValue, float attackerMaxDamageValue) {
            CalculatedDamage = Random.Range(attackerMinDamageValue, attackerMaxDamageValue); // (�ּ�~�ִ�)���� �� ������ ��ġ ����
        }

        private void CalcCriticalMultiplier(float attackerCriticalChangeValue) {
            CalculatedDamage = CalculatedDamage * ((IsCritical) ? attackerCriticalChangeValue : 1f); // ġ��Ÿ ������ ���� ġ��Ÿ �¼� ���
        }

        private void CalcArmorAndPenetration(float attackerPenetrationValue, float defenderArmorValue) {
            float penetratedArmorValue = (defenderArmorValue - attackerPenetrationValue) < 0f ? 0f : defenderArmorValue; // 1) ����� ��ġ��ŭ ��� ��ġ ����
            CalculatedDamage = CalculatedDamage - penetratedArmorValue;                                                  // 2) ������ ��ġ���� ����� ����� ���� ��ġ��ŭ ����
        }

        private void CalcDamageBySkillRatio(float attackerSkillDefaultDamageValue, float attackerSkillRatioValue) {
            CalculatedDamage = attackerSkillDefaultDamageValue + (CalculatedDamage * attackerSkillRatioValue); // attackerSkillRatioValue = 0f(0%) ~ 1f(100%)
        }
        #endregion
#endif
    }

    [System.Serializable]
    public class TempPlayerStat {
        public float HP = 0f;
        public float MinDamage = 0f;
        public float MaxDamage = 0f;
        public float CriticalChance = 0f;
        public float CriticalDamageMultiplier = 1.25f; // Default
        public float Defence = 0f;
        public float CriticalResist = 0f;
        public float Penetration = 0f;
    }
}
