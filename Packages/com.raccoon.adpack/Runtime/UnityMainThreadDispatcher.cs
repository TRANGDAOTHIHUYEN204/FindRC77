using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RCKit.Ads
{
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        [SerializeField]
        public bool dontDestroyOnLoad = true;

        private static readonly Queue<Action> _executionQueue = new Queue<Action>();

        private static UnityMainThreadDispatcher instance = null;

        public static bool IsExist
        {
            get
            {
                if (instance == null)
                {
                    try
                    {
                        //UnityMainThreadDispatcher unityMainThreadDispatcher = Resources.Load<UnityMainThreadDispatcher>("UnityMainThreadDispatcher");
                        var obj = new GameObject("unity_main_thread");
                        var unityMainThreadDispatcher = obj.AddComponent<UnityMainThreadDispatcher>();

                        if (unityMainThreadDispatcher != null)
                        {
                            instance = Instantiate(unityMainThreadDispatcher);
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }

                return instance != null;
            }
        }

        public void Update()
        {
            lock (_executionQueue)
            {
                while (_executionQueue.Count > 0)
                {
                    _executionQueue.Dequeue()?.Invoke();
                }
            }
        }

        public static void Enqueue(IEnumerator action)
        {
            if (!IsExist)
            {
                throw new Exception("UnityMainThreadDispatcher could not find the UnityMainThreadDispatcher object. Please ensure you have added the Base/Resources/UnityMainThreadDispatcher Prefab to your scene.");
            }

            lock (_executionQueue)
            {
                _executionQueue.Enqueue(() =>
                {
                    if (instance != null)
                        instance.StartCoroutine(action);
                });
            }
        }

        public static void Enqueue(Action action)
        {
            if (!IsExist)
            {
                throw new Exception("UnityMainThreadDispatcher could not find the UnityMainThreadDispatcher object. Please ensure you have added the Base/Resources/UnityMainThreadDispatcher Prefab to your scene.");
            }

            Enqueue(instance.ActionWrapper(action));
        }

        private IEnumerator ActionWrapper(Action action)
        {
            action?.Invoke();
            yield return null;
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                if (dontDestroyOnLoad)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }

        }

        private void OnDestroy()
        {
            instance = null;
        }
    }
}