using UnityEngine;

public class TestEntity : Entity {
    public bool  Controlled = false;
    public float SearchRadius = 10f;
    public float Speed = 10f;

    private static uint[] _queriedEntities = new uint[256];

    public override void Execute() {
        if(Controlled) {
            var x = Input.GetAxis("Horizontal");
            var z = Input.GetAxis("Vertical");
            var y = 0;

            if(Input.GetKey(KeyCode.Space)) {
                y = 1;
            }else if(Input.GetKey(KeyCode.LeftShift)) {
                y = -1;
            }

            MoveEntity(new Vector3(x, y, z) * (Time.deltaTime * Speed));

            var nearbyEntitiesCount = QueryNearbyEntities(SearchRadius, _queriedEntities);
            for(var i = 0; i < nearbyEntitiesCount; ++i) {
                if(Em.GetEntity(Em.GetHandle(_queriedEntities[i]), out var e)) {
                    Debug.DrawLine(transform.position, e.transform.position, Color.red);
                }
            } 
        }
    }
}