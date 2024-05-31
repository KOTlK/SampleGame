using UnityEngine;

public class SpatialHashingTest : MonoBehaviour {
    public EntityManager EntityManager;
    public int           EntitiesCount;
    public Entity        Prefab;
    public float         SpawnRadius = 20f;

    private void Start() {
        for(var i = 0; i < EntitiesCount; ++i) {
            var position = Random.insideUnitSphere * SpawnRadius;
            var entity = EntityManager.CreateEntity(Prefab, position, Quaternion.identity, Vector3.one);
        }
    }

    private void Update() {
        EntityManager.Execute();
    }
}