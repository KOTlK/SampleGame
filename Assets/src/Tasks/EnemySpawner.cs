using UnityEngine;
using System.Collections;

[System.Serializable]
public class EnemySpawner : IEnumerator {
    public Vector3 MaxBounds;
    public Vector3 MinBounds;
    public Vector3 WorldCenter = Vector3.zero;
    public Enemy   Prefab;
    public float   SpawnRate; // spawns per minute
    
    private float _delay;

    public object Current => null;
    
    public void Reset() { }
    
    public bool MoveNext() {
        var em = Singleton<EntityManager>.Instance;
        
        _delay += Time.deltaTime;
            
        if(_delay >= 60f / SpawnRate) {
            em.CreateEntity(Prefab, GetRandomPosition());
            _delay = 0f;
        }
        
        return true;
    }
    
    private Vector3 GetRandomPosition(){
        var x = Random.Range(MinBounds.x, MaxBounds.x);
        var z = Random.Range(MinBounds.z, MaxBounds.z);
        
        return new Vector3(x, 0, z);
    }
}
