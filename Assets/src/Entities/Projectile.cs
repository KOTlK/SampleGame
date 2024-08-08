using UnityEngine;

public class Projectile : Entity {
    public float     Speed;
    public float     Size;
    public float     TimeToLive;
    public Damage    Damage;
    public LayerMask Mask;
    
    private Vector3 _direction;
    private EntityHandle  _sender;
    
    private static Collider[] CollisionBuffer = new Collider[32];
    
    public void Shoot(Vector3 direction, EntityHandle sender) {
        _direction = direction;
        _sender    = sender;
        Damage.sender = sender;
    }

    public override void Save(ISaveFile sf)
    {
        base.Save(sf);
        sf.Write(nameof(Speed), Speed);
        sf.Write(nameof(Size), Size);
        sf.Write(nameof(TimeToLive), TimeToLive);
        sf.WriteObject(nameof(Damage), Damage);
        sf.Write(nameof(Mask), Mask.value);
        sf.Write(nameof(_direction), _direction);
        sf.WriteObject(nameof(_sender), _sender);
    }

    public override void Load(ISaveFile sf)
    {
        base.Load(sf);
        Speed = sf.Read(nameof(Speed), Speed);
        Size = sf.Read(nameof(Size), Size);
        TimeToLive = sf.Read(nameof(TimeToLive), TimeToLive);
        Damage = sf.ReadValueType<Damage>(nameof(Damage));
        Mask.value = sf.Read(nameof(Mask), Mask.value);
        _direction = sf.Read(nameof(_direction), _direction);
        _sender = sf.ReadValueType<EntityHandle>(nameof(_sender));
    }


    public override void Execute() {
        TimeToLive -= Time.deltaTime;
        
        if(TimeToLive <= 0) {
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
                if(character.Id != _sender.Id) {
                    character.ApplyDamage(Damage);
                    Em.DestroyEntity(Id);
                    return;
                }
            }
        }
    }
}
