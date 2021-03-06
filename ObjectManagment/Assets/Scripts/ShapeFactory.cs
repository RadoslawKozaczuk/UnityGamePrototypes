﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// To add such an asset to our project, we'll have to add an entry for it to Unity's menu.
// The simplest way to do this is by adding the CreateAssetMenu attribute to our class.
// Thanks to that we can now create our factory via Assets › Create › Shape Factory.
[CreateAssetMenu]
// Factory doesn't need a position, rotation, or scale, or an Update method.
// So it doesn't need to be a component, which would have to be attached to a game object.
// Instead, it can exist on it own, not part of a specific scene, but part of the project.
// In other words, it is an asset. This is possible, by having it extend ScriptableObject instead of MonoBehaviour.
public class ShapeFactory : ScriptableObject
{
    [SerializeField] Shape[] _prefabs;
    [SerializeField] Material[] _materials;
    [SerializeField] bool _recycle;
    // We use lists instead of stacks because they survive recompilation in play mode, while stacks don't. 
    // Unity doesn't serialize stacks. You could use stacks instead, but lists work just fine.
    List<Shape>[] _pools;
    // We cannot make _pools Serializable because that would make Unity save the _pools as part of the asset, 
    // persisting it between editor play sessions and including it in builds.

    Scene _poolScene;

    public Shape Get(int shapeId = 0, int materialId = 0)
    {
        if (_recycle)
        {
            // unfortunately we cannot create pools in the constructor because at the moment of constructing
            // prefabs table is not yet fill in with elements
            if (_pools == null)
                CreatePools();
            
            var pool = _pools[shapeId]; // get the correct pool
            int lastIndex = pool.Count - 1;
            if (lastIndex >= 0) // check if we have anything in this pool
            {
                var shape = pool[lastIndex];
                shape.gameObject.SetActive(true);
                pool.RemoveAt(lastIndex);
                return shape;
            }
            else
            {
                var shape = Instantiate(_prefabs[shapeId]);
                shape.ShapeId = shapeId;
                SceneManager.MoveGameObjectToScene(shape.gameObject, _poolScene);
                return shape;
            }
        }

        return Instantiate(_prefabs[shapeId]);
    }

    public Shape GetRandom() => Get(Random.Range(0, _prefabs.Length), Random.Range(0, _materials.Length));

    /// <summary>
    /// If recycle is set to true the object will be returned to the Factory's object pool.
    /// Otherwise the object will be destroyed.
    /// </summary>
    public void Destroy(Shape shape)
    {
        if (_recycle)
        {
            if (_pools == null)
                CreatePools();

            _pools[shape.ShapeId].Add(shape);
            // we disable the object so it is no longer used in the scene
            shape.gameObject.SetActive(false);
        }
        else
        {
            // Destroy works on either a game object, a component, or an asset.
            // To get rid of the entire shape object and not just its Shape component, 
            // we have to explicitly destroy the game object that the component is a part of.
            // We can access it via the component's gameObject property.
            Destroy(shape.gameObject);
        }
    }

    void CreatePools()
    {
        _pools = new List<Shape>[_prefabs.Length];
        for (int i = 0; i < _pools.Length; i++)
            _pools[i] = new List<Shape>();

        // Scene is a struct, not a direct reference to the actual scene. 
        // As it is not serializable, recompilation resets the struct to its default values, which indicates an unloaded scene.
        // To avoid this problem we have to request a fresh connection.
        // This problem may only occur in the Editor.
        if (Application.isEditor)
        {
            _poolScene = SceneManager.GetSceneByName(name); // get the scene by the name of this object
            if (_poolScene.isLoaded)
            {
                // Second possible problem in this scenario is losing the list that keep track on the inactive objects.
                // We can solve this by repopulating the lists.
                GameObject[] rootObjects = _poolScene.GetRootGameObjects();
                for (int i = 0; i < rootObjects.Length; i++)
                {
                    Shape pooledShape = rootObjects[i].GetComponent<Shape>();
                    if (!pooledShape.gameObject.activeSelf)
                        _pools[pooledShape.ShapeId].Add(pooledShape);
                }
                return;
            }
        }

        _poolScene = SceneManager.CreateScene(name);
    }
}
