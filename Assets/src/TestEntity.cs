using UnityEngine;

public class TestEntity : Entity {
    public bool  Controlled = false;
    public float SearchRadius = 10f;
    public float Speed = 10f;

    private static int[] _queriedEntities = new int[256];

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

            Move(new Vector3(x, y, z) * (Time.deltaTime * Speed));
            

            var nearbyEntitiesCount = Em.EntitiesTable.Query(transform.position, _queriedEntities, SearchRadius);
            for(var i = 0; i < nearbyEntitiesCount; ++i) {
                var (alive, ent) = Em.GetEntity(_queriedEntities[i]);
                Debug.DrawLine(transform.position, ent.transform.position, Color.red);
            } 
        }
    }
}