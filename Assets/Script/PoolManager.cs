using UnityEngine;
using System.Collections.Generic;

public class PoolManager
{
    [SerializeField] private GameObject gameObject;
    [SerializeField] private List<GameObject> poolGameObjects;
    [SerializeField] private List<GameObject> activeGameObjects;
    
    public IReadOnlyList<GameObject> PoolGameObjects => poolGameObjects;
    public IReadOnlyList<GameObject> ActiveGameObjects => activeGameObjects;

    public GameObject CreateGameObject(GameObject gameObjectPrefab, Vector3 position, Quaternion rotation,
        Vector3 direction)
    {
        if (gameObjectPrefab == null)
        {
            return null;
        }

        GameObject currentGameObject = null;
        for (int i = poolGameObjects.Count -1 ; i >= 0; i--)
        {
            if (gameObject.name == poolGameObjects[i].name || gameObject.name == poolGameObjects[i].name + "(Clone)")
            {
                currentGameObject = poolGameObjects[i];
                poolGameObjects.RemoveAt(i);
                break;
            }
        }

        if (currentGameObject == null)
        {
            currentGameObject = GameObject.Instantiate(gameObjectPrefab);
        }
        currentGameObject.transform.SetPositionAndRotation(position,  rotation);
        currentGameObject.SetActive(true);
        activeGameObjects.Add(currentGameObject);
    
        return currentGameObject;
    }
    public void ReturnObject(GameObject gameobject)
    {
        gameobject.SetActive(false);
        activeGameObjects.Remove(gameobject);
        poolGameObjects.Add(gameobject);
    }
}
