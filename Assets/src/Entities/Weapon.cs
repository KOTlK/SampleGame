using UnityEngine;

public class Weapon : Entity {
    public EntityHandle Owner;
    public ResourceLink BulletPrefab;
    public Transform    Muzzle;
    public float        FireRate; //bullets per second;
    public bool         CanShoot;
    
    private float _timePassed;

    public override void Save(SaveFile sf) {
        base.Save(sf);
        sf.Write(nameof(BulletPrefab), BulletPrefab);
        sf.Write(nameof(FireRate), FireRate);
        sf.WriteBool(nameof(CanShoot), CanShoot);
        sf.Write(nameof(_timePassed), _timePassed);
    }

    public override void Load(SaveFile sf) {
        base.Load(sf);
        BulletPrefab = sf.ReadValueType<ResourceLink>(nameof(BulletPrefab));
        FireRate = sf.ReadFloat(nameof(FireRate));
        CanShoot = sf.ReadBool(nameof(CanShoot));
        _timePassed = sf.ReadFloat(nameof(_timePassed));
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