using UnityEngine;

public class Projectile : Entity {
    public float     Speed;
    public float     Size;
    public float     TimeToLive;
    public Damage    Damage;
    public LayerMask Mask;
    
    private Vector3 _direction;
    private Entity  _sender;
    
    private static Collider[] CollisionBuffer = new Collider[32];
    
    public void Shoot(Vector3 direction, Entity sender) {
        _direction = direction;
        _sender    = sender;
        Damage.sender = sender.Id;
    }
    
    public override void Execute() {
        TimeToLive -= Time.deltaTime;
        
        if(TimeToLive <= 0){
            if(Em.IsAlive(Id)) {
                Em.DestroyEntity(Id);
            }
            return;
        }
        
        transform.position += _direction * Speed * Time.deltaTime;
        
        var collisionsCount = Physics.OverlapSphereNonAlloc(transform.position, Size, CollisionBuffer, Mask.value);
        
        for(var i = 0; i < collisionsCount; ++i) {
            var coll = CollisionBuffer[i];
            
            if(coll.TryGetComponent(out Character character)) {
                if(character != _sender) {
                    character.ApplyDamage(Damage);
                    Em.DestroyEntity(Id);
                    return;
                }
            }
        }
    }
}
