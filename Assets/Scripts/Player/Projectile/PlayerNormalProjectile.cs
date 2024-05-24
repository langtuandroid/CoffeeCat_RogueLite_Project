using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CoffeeCat.Datas;
using CoffeeCat.FrameWork;
using DG.Tweening;
using UnityEngine;

namespace CoffeeCat
{
    public class PlayerNormalProjectile : PlayerProjectile
    {
        protected override void ProjectilePath(Vector3 direction, float speed, Vector3 startPos)
        {
            tr.DORewind();
            tr.DOMove(direction * 10f, speed)
              .SetRelative().SetSpeedBased().SetEase(Ease.Linear).SetDelay(0.1f).From(startPos)
              .OnComplete(() => ObjectPoolManager.Instance.Despawn(gameObject));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.gameObject.TryGetComponent(out MonsterStatus monsterStat))
                return;

            var damageData =
                DamageData.GetData(AttackData, monsterStat.CurrentStat);
            monsterStat.OnDamaged(damageData, true, tr.position, 10f);

            ObjectPoolManager.Instance.Despawn(gameObject);
        }
    }
}