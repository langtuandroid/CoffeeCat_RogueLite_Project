using UnityEngine;
using UniRx.Triggers;
using Sirenix.OdinInspector;
using CoffeeCat.FrameWork;
using CoffeeCat.Utils.Defines;
using CoffeeCat.UI;
using CoffeeCat.Utils;

namespace CoffeeCat {
    public class DamageTextManager : GenericSingleton<DamageTextManager> {
        [Title("Information")]
        [SerializeField, ReadOnly] Camera mainCamera = null;
        [SerializeField, ReadOnly] Camera uiCamera = null;
        [SerializeField, ReadOnly] Canvas targetCanvas = null;
        [SerializeField, ReadOnly] RectTransform textsParentRectTr = null;
        [ShowInInspector, ReadOnly] public bool IsSetupCompleted { get; private set; } = false;

        [Title("Texts")]
        [SerializeField, ReadOnly] GameObject damageTextOriginGameObject = null;
        [SerializeField, ReadOnly] private int poolInitializingCount = 30;
        [SerializeField, ReadOnly] string spawnKey = string.Empty;
        string textFormat = "#,###";

        protected override void Initialize() {
            base.Initialize();
            SceneManager.Instance.OnSceneChangeBeforeEvent += Clear;
        }

        /// <summary>
        /// �� ���� �� �ݵ�� �ѹ� ȣ���ؾ� DamageText��� ��� ����
        /// </summary>
        /// <param name="textRenderCanvas"></param>
        /// <param name="damageTextParent"></param>
        public void Setup(Canvas textRenderCanvas, Camera uiCamera) {
            // Make Damage Texts Parent in Rendering Canvas
            this.mainCamera = Camera.main;
            this.uiCamera = uiCamera;
            this.targetCanvas = textRenderCanvas;
            var parentRectTr = new GameObject("DamageTextsParent").AddComponent<RectTransform>();
            parentRectTr.SetParent(this.targetCanvas.transform);
            parentRectTr.SetAnchorAndPivot(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            parentRectTr.ResetPosition();
            parentRectTr.SetAsFirstSibling();
            textsParentRectTr = parentRectTr;

            // Check DamageText Origin GameObject
            if (damageTextOriginGameObject == null) {
                // Load DamageText GameObject
                ResourceManager.Instance.AddressablesAsyncLoad<GameObject>("DamageText", false, (loadedGameObject) => {
                    damageTextOriginGameObject = loadedGameObject;
                    spawnKey = loadedGameObject.name;
                    InitializeObjectPool();
                });
            }
            else {
                InitializeObjectPool();
            }

            // ObjectPool Initializing
            void InitializeObjectPool() {
                PoolInformation poolInformation = PoolInformation.New(damageTextOriginGameObject, true, poolInitializingCount);
                ObjectPoolManager.Instance.AddToPool(poolInformation);
                IsSetupCompleted = true;
            }
        }

        /// <summary>
        /// �� ������ �� ȣ�� (���� �� ���� ����)
        /// </summary>
        /// <param name="sceneName"></param>
        private void Clear(SceneName sceneName) {
            this.targetCanvas = null;
            this.textsParentRectTr = null;
            IsSetupCompleted = false;
        }

        #region FUNCTIONS

        public void OnFloatingText(float damageCount, Vector2 startPosition) {
            OnFloatingText(damageCount.ToString(textFormat), startPosition);
        }

        public void OnFloatingText(string damageCountStr, Vector2 startPosition) {
            var spawnedDamageText = ObjectPoolManager.Instance.Spawn<DamageText>(spawnKey, Vector2.zero, Quaternion.identity, textsParentRectTr);
            Vector2 playPosition = UIHelper.WorldPositionToCanvasAnchoredPosition(mainCamera, startPosition, targetCanvas.GetComponent<RectTransform>());
            spawnedDamageText.OnFloating(playPosition, damageCountStr);
        }

        public void OnReflectingText(float damageCount, Vector2 startPosition, Vector2 direction) {
            OnReflectingText(damageCount.ToString(textFormat), startPosition, direction);
        }

        public void OnReflectingText(string damageCountStr, Vector2 startPosition, Vector2 direction) {
            var spawnedDamageText = ObjectPoolManager.Instance.Spawn<DamageText>(spawnKey, Vector2.zero, Quaternion.identity);
            //Vector2 playPosition = UIHelper.WorldPositionToCanvasAnchoredPosition(mainCamera, startPosition, targetCanvas.GetComponent<RectTransform>());
            spawnedDamageText.OnReflecting(startPosition, direction, damageCountStr);
        }

        public void OnTransmittanceText(float damageCount, Vector2 startPosition, Vector2 direction) {
            this.OnTransmittanceText(damageCount.ToString(textFormat), startPosition, direction);
        }

        public void OnTransmittanceText(string damageCountStr, Vector2 startPosition, Vector2 direction) {
            var spawnText = ObjectPoolManager.Instance.Spawn<DamageText>(spawnKey, Vector2.zero, Quaternion.identity);
            spawnText.OnTransmittance(startPosition, direction, damageCountStr);
        }

        #endregion
    }
}
