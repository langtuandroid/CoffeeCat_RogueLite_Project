using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CoffeeCat.Datas;
using CoffeeCat.FrameWork;
using DG.Tweening;
using UnityEngine;

namespace CoffeeCat
{
    public class PlayerProjectile : MonoBehaviour
    {
        [SerializeField] protected float knockBackForce = 0f; 
        protected Transform tr = null;
        protected ProjectileDamageData projectileDamageData = null;

        private void Awake()
        {
            tr = GetComponent<Transform>();
        }
        
        protected virtual void SetDamageData(PlayerStat playerStat, float skillBaseDamage = 0f, float skillCoefficient = 1f)
        {
            projectileDamageData = new ProjectileDamageData(playerStat, skillBaseDamage, skillCoefficient);
        }
    }
}