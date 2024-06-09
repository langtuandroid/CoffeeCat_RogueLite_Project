using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CoffeeCat.FrameWork;
using CoffeeCat.Utils;
using UnityEngine;

namespace CoffeeCat
{
    [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Local")]
    public class PlayerSkillEffect_Passive : PlayerSkillEffect
    {
        private const string passiveAddressableKey = "Passive";
        
        protected override void SkillEffect(PlayerStat playerStat)
        {
            if (playerSkillData is not PlayerPassiveSkill skillData)
            {
                CatLog.WLog("PlayerSkillEffect_Passive : skillData is null");
                return;
            }

            switch (skillData.SkillName)
            {
                case "SpeedUp": SpeedUp(playerStat, skillData);
                    break;
                case "DamageIncrease": DamageIncrease(playerStat, skillData);
                    break;
                case "CoolTimeReduce": CoolTimeReduce(skillData);
                    break;
            }

            PassiveSkillEffectSpawn();
        }

        public override void UpdateSkillData(PlayerSkill updateSkillData)
        {
            playerSkillData = updateSkillData;
            PassiveSkillEffectSpawn();
        }

        private void PassiveSkillEffectSpawn()
        {
            var spawnObj = ObjectPoolManager.Instance.Spawn(passiveAddressableKey, playerTr);
            spawnObj.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        }

        public PlayerSkillEffect_Passive(Transform playerTr, PlayerSkill playerSkillData)
        {
            this.playerTr = playerTr;
            this.playerSkillData = playerSkillData;

            // Passive Skill Effect는 모든 Passive Skill의 Addressable Key가 동일하기 때문에 중복 방지
            if (ObjectPoolManager.Instance.IsExistInPoolDictionary(passiveAddressableKey)) 
                return;
            
            ResourceManager.Instance.AddressablesAsyncLoad<GameObject>(passiveAddressableKey, false, (origin) =>
            {
                ObjectPoolManager.Instance.AddToPool(PoolInformation.Create(origin));
            });
        }

        #region PassiveSkillEffect

        private void SpeedUp(PlayerStat stat, PlayerPassiveSkill skillData) => stat.MoveSpeed += skillData.Delta;
        
        private void DamageIncrease(PlayerStat stat, PlayerPassiveSkill skillData) => stat.AttackPower += skillData.Delta;
        
        private void CoolTimeReduce(PlayerPassiveSkill skillData)
        {
            var player = playerTr.GetComponent<Player>();
            player.GetCoolTimeReduce(skillData.Delta);
        }
        
        #endregion
    }
}