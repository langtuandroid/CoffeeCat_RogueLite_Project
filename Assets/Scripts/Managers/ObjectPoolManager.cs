using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniRx;
using Sirenix.OdinInspector;
using CoffeeCat.Utils;
using CoffeeCat.Utils.SerializedDictionaries;
using CoffeeCat.Utils.Defines;

namespace CoffeeCat.FrameWork
{
    public class ObjectPoolManager : GenericSingleton<ObjectPoolManager>
    {
        [Space(5f), Title("POOL INFORMATION")]
        [SerializeField, DictionaryDrawerSettings(DisplayMode = DictionaryDisplayOptions.ExpandedFoldout, IsReadOnly = true)] 
        private StringPoolInformationDictionary originInformationDict = null;

        [Space(5f), Title("POOL DICTIONARY")]
        [SerializeField, DictionaryDrawerSettings(DisplayMode = DictionaryDisplayOptions.ExpandedFoldout, IsReadOnly = true)] 
        private StringGameObjectStackDictionary poolStackDict = null;

        [Space(5f), Title("ROOT PARENT DICTIONARY")]
        [SerializeField, DictionaryDrawerSettings(DisplayMode = DictionaryDisplayOptions.ExpandedFoldout, IsReadOnly = true)]
        private StringTransformDictionary rootParentDict = null;

        // Original Pool Dictionary
        private StringGameObjectStackDictionary originPoolStackDictionary = null;

        #region TEMP COLLECTIONS
        private List<GameObject> tempResultList = null;
        #endregion

        private void Start() => SceneManager.Instance.OnSceneChangeBeforeEvent += OnSceneChangeBeforeEvent;

        protected override void Initialize() {
            originInformationDict = new StringPoolInformationDictionary();
            poolStackDict = new StringGameObjectStackDictionary();
            rootParentDict = new StringTransformDictionary();
            originPoolStackDictionary = new StringGameObjectStackDictionary();
            tempResultList = new List<GameObject>();
        }

        #region ADD TO POOL

        public void AddToPool(PoolInformation newInfo) {
            // Load Origin PoolObject is Only Sync
            newInfo?.TryOriginPrefabLoadSync(AddToObjectPoolDictionary);
        }

        public void AddToPool(params PoolInformation[] newInfos) {
            foreach (var information in newInfos) {
                AddToPool(information);
            }
        }

        private void AddToObjectPoolDictionary(PoolInformation information) {
            if (poolStackDict.ContainsKey(information.PoolObject.name)) {
                CatLog.Log($"{information.PoolObject.name} is Already Containing Object Pool Dictionary.");
                return;
            }

            // Add Origin Information Dictionary
            originInformationDict.Add(information.PoolObject.name, information);

            // Get Parent Data
            Transform parent = null;
            if (information.HasRootParent) {
                if (!information.HasCustomRootParent) {
                    string rootParentName = information.PoolObject.name + "_Root";
                    parent = new GameObject(rootParentName).GetComponent<Transform>();
                }
                else {
                    parent = information.CustomRootParent;
                }

                rootParentDict.Add(information.PoolObject.name, parent);
            }

            // Add Pool Dictionary
            Stack<GameObject> poolStack = new Stack<GameObject>(information.InitSpawnCount);
            for (int i = 0; i < information.InitSpawnCount; i++) {
                var clone = Instantiate(information.PoolObject, parent);
                clone.SetActive(information.IsStayEnabling);
                clone.name = information.PoolObject.name;
                poolStack.Push(clone);
            }

            poolStackDict.Add(information.PoolObject.name, poolStack);
            originPoolStackDictionary.Add(information.PoolObject.name, new Stack<GameObject>(poolStack));
        }

        #endregion

        #region SPAWN

        private GameObject Spawn(string key, Vector3 position, Quaternion rotation, bool isParentSet, Transform parent) {
            if (poolStackDict.ContainsKey(key) == false) {
                CatLog.ELog($"ObjectSpawn Key: {key} is Not Exist in Pool Dictionary.");
                return null;
            }

            if (poolStackDict[key].Count <= 0) {
                Transform rootParent = (rootParentDict.ContainsKey(key)) ? rootParentDict[key] : null;
                var clone = Instantiate(originInformationDict[key].PoolObject, rootParent);
                clone.name = key;
                poolStackDict[key].Push(clone);
                originPoolStackDictionary[key].Push(clone);
            }

            var resultTr = poolStackDict[key].Pop().transform;
            if (isParentSet) {
                resultTr.SetParent(parent);
            }

            resultTr.position = position;
            resultTr.rotation = rotation;
            resultTr.gameObject.SetActive(true);
            return resultTr.gameObject;
        }

        private T Spawn<T>(string key, Vector3 position, Quaternion rotation, bool isParentSet, Transform parent) where T : Component {
            var spawnGameObject = Spawn(key, position, rotation, isParentSet, parent);
            if (spawnGameObject == null) {
                return null;
            }
                
            if (spawnGameObject.TryGetComponent(out T result) == false) {
                CatLog.ELog($"Spawned PoolObject {spawnGameObject.name} is Not Exist {nameof(T)} Component");
                Despawn(spawnGameObject);
            }

            return result;
        }

        #endregion

        #region DESPAWN

        public void Despawn(GameObject poolObject) {
            this.Despawn(poolObject, 0f);
        }

        public void Despawn(GameObject poolObject, float delaySeconds) {
            if (poolStackDict.TryGetValue(poolObject.name, out Stack<GameObject> objectPoolStack) == false) {
                CatLog.WLog($"this '{poolObject.name}' is Not Contains Object Pool Dictionary.");
                return;
            }

            if (delaySeconds <= 0f) {
                Execute();
                return;
            }

            Observable.Timer(TimeSpan.FromSeconds(delaySeconds))
                      .Skip(TimeSpan.Zero)
                      .TakeUntilDisable(poolObject)
                      .Subscribe(_ => {
                          Execute();
                      })
                      .AddTo(this);

            // Execute Despawn GameObject
            void Execute() {
                poolObject.SetActive(false);

                // Restore Root Parent
                var poolObjectTr = poolObject.transform;
                // Is Exsit Root Parent
                if (rootParentDict.ContainsKey(poolObject.name)) {
                    // If Changed Parent
                    if (!ReferenceEquals(rootParentDict[poolObject.name], poolObjectTr.parent)) {
                        poolObjectTr.SetParent(rootParentDict[poolObject.name]);
                    }
                }
                objectPoolStack.Push(poolObject);
            }
        }

        public void DespawnAll(string key) {
            foreach (var poolObject in GetActivatedPoolObjects(key)) {
                Despawn(poolObject);
            }
        }

        public void DespawnAll(params string[] keys) {
            foreach (var key in keys) {
                DespawnAll(key);
            }
        }

        #endregion

        #region METHOD OVERLOAD

        public GameObject Spawn(string key, Vector3 position, Quaternion rotation, Transform parent) {
            return this.Spawn(key, position, rotation, true, parent);
        }

        public GameObject Spawn(string key, Vector3 position, Quaternion rotation) {
            return this.Spawn(key, position, rotation, false, null);
        }

        public GameObject Spawn(string key, Vector3 position) {
            return this.Spawn(key, position, Quaternion.identity, false, null);
        }

        public GameObject Spawn(string key, Transform parent) {
            return this.Spawn(key, Vector3.zero, Quaternion.identity, true, parent);
        }

        public T Spawn<T>(string key, Vector3 position, Quaternion rotation, Transform parent) where T : Component {
            return this.Spawn<T>(key, position, rotation, true, parent);
        }

        public T Spawn<T>(string key, Vector3 position, Quaternion rotation) where T : Component {
            return this.Spawn<T>(key, position, rotation, false, null);
        }

        public T Spawn<T>(string key, Vector3 position) where T : Component {
            return this.Spawn<T>(key, position, Quaternion.identity, false, null);
        }

        public T Spawn<T>(string key, Transform parent) where T : Component {
            return this.Spawn<T>(key, Vector3.zero, Quaternion.identity, true, parent);
        }

        #endregion

        #region GETTER

        /// <summary>
        /// Return Activated PoolObjects Array
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public GameObject[] GetActivatedPoolObjects(string key) {
            if (!originPoolStackDictionary.TryGetValue(key, out Stack<GameObject> poolStack)) {
                CatLog.WLog($"Pool Dictionary Key Not Found. Null return. key: {key}");
                return Array.Empty<GameObject>();
            }

            //tempResultList.Clear();
            //tempResultList.AddRange(poolStack);
            //for (int i = tempResultList.Count - 1; i >= 0; i--) {
            //    if (!tempResultList[i].activeSelf) {
            //        tempResultList.Remove(tempResultList[i]);
            //    }
            //}
            //
            //var activatePoolObjects = poolStack.Where(go => go.activeSelf).ToArray();

            //return tempResultList.ToArray();
            
            return poolStack.Where(poolObject => poolObject.activeSelf).ToArray();
        }

        /// <summary>
        /// Return All PoolObjects Array
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public GameObject[] GetAllPoolObjects(string key) {
            if (!originPoolStackDictionary.TryGetValue(key, out Stack<GameObject> poolStack)) {
                CatLog.ELog($"Pool Dictionary Key Not Found. Null return. key: {key}");
                return null;
            }
            
            return poolStack.ToArray();
        }

        public bool IsExistInPoolDictionary(string key) {
            return poolStackDict.ContainsKey(key);
        }

        #endregion

        private void OnSceneChangeBeforeEvent(SceneName sceneName) {
            ClearPoolDictionary();
        }

        public void ClearPoolDictionary() {
            originInformationDict.Clear();
            originPoolStackDictionary.Clear();
            poolStackDict.Clear();
            rootParentDict.Clear();
        }
    }
}