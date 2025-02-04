using UnityEngine;

public class Weapon : Entity {
    public EntityHandle Owner;
    public ResourceLink BulletPrefab;
    public Transform    Muzzle;
    public float        FireRate; //bullets per second;
    public bool         CanShoot;
    
    private float _timePassed;

    public override void Save(ISaveFile sf) {
        base.Save(sf);
        sf.WriteObject(BulletPrefab, nameof(BulletPrefab));
        sf.Write(FireRate, nameof(FireRate));
        sf.Write(CanShoot, nameof(CanShoot));
        sf.Write(_timePassed, nameof(_timePassed));
    }

    public override void Load(ISaveFile sf) {
        base.Load(sf);
        BulletPrefab = sf.ReadValueType<ResourceLink>(nameof(BulletPrefab));
        FireRate = sf.Read<float>(nameof(FireRate));
        CanShoot = sf.Read<bool>(nameof(CanShoot));
        _timePassed = sf.Read<float>(nameof(_timePassed));
    }

    public void AttachToSlot(Transform t) {
        transform.SetParent(t);
    }

    public override void Execute() {
        _timePassed += Time.deltaTime;
        
        if(_timePassed >= 1 / FireRate) {
            CanShoot = true;   
        }
    }
    
    public void Shoot(Vector3 direction) {
        if(CanShoot) {
            var bulletHandle = Em.CreateEntity(BulletPrefab, 
                                               Muzzle.position, 
                                               Quaternion.Euler(direction));
            if(Em.GetEntity<Projectile>(bulletHandle, out var bullet)) {
                bullet.Shoot(direction, Owner);
                CanShoot = false;
                _timePassed = 0f;
            }
        }
    }
}