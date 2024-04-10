using UnityEngine;
using CoffeeCat.Utils;

namespace CoffeeCat.FrameWork {
    [DisallowMultipleComponent]
    public class GenericSingleton<T> : MonoBehaviour where T : MonoBehaviour {
        // Destroy ���� Ȯ�ο�
        private static bool _shuttingDown = false;
        private static object _lock = new object();
        private static T _instance;

        /// <summary>
        /// Check Singleton Instance Exist 
        /// </summary>
        public static bool IsExist {
            get {
                return _instance != null;
            }
        }

        public static T Instance {
            get {
                // ���� ���� �� Object ���� �̱����� OnDestroy �� ���� ���� �� ���� �ִ�. 
                // �ش� �̱����� gameObject.Ondestory() ������ ������� �ʰų� ����Ѵٸ� null üũ�� ������
                if (_shuttingDown) {
                    Debug.LogWarning("[Singleton] Instance '" + typeof(T).Name + "' already destroyed. Returning null.");
                    return null;
                }

                lock (_lock)    //Thread Safe
                {
                    if (_instance == null) {
                        // �ν��Ͻ� ���� ���� Ȯ��
                        _instance = (T)FindObjectOfType(typeof(T));

                        // ���� �������� �ʾҴٸ� �ν��Ͻ� ����
                        if (_instance == null) {
                            // ���ο� ���ӿ�����Ʈ�� ���� �̱��� Attach
                            var singletonObject = new GameObject();
                            _instance = singletonObject.AddComponent<T>();
                            singletonObject.name = typeof(T).Name + " (Singleton)";

                            // Make instance persistent.
                            DontDestroyOnLoad(singletonObject);

                            // Singleton Initialize Call. 
                            _instance.SendMessage(nameof(GenericSingleton<T>.Initialize)); // Type 1. SendMessage
                            //_instance.GetComponent<GenericSingleton<T>>().Initialize();  // Type 2. GetComponent
                            CatLog.Log($"Initialized Singleton {typeof(T).Name}");
                        }
                    }
                    return _instance;
                }
            }
        }

        // �� �̱��� ������ ��� ����
        protected GenericSingleton() { }

        /// <summary>
        /// ���� ���� �ۼ� ����
        /// </summary>
        protected virtual void Initialize() { }

        //private void Awake() => Initialize();

        private void OnApplicationQuit() => _shuttingDown = true;

        private void OnDestroy() => _shuttingDown = true;

        public virtual void ReleaseSingleton() {
            _instance = null;
            Destroy(this);
        }
    }
}
