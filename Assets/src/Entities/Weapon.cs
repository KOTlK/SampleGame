using UnityEngine;

public class Weapon : Entity {
    public EntityHandle Owner;
    public Projectile BulletPrefab;
    public Transform  Muzzle;
    public float      FireRate; //bullets per second;
    public bool       CanShoot;
    
    private float _timePassed;
    
    public override void Execute() {
        _timePassed += Time.deltaTime;
        
        if(_timePassed >= 1 / FireRate) {
            CanShoot = true;   
        }
    }
    
    public void Shoot(Vector3 direction) {
        if(CanShoot) {
            var bulletHandle = Em.CreateEntity(
                               BulletPrefab, 
                               Muzzle.position, 
                               Quaternion.Euler(direction), 
                               BulletPrefab.transform.localScale);
            if(Em.GetEntity<Projectile>(bulletHandle, out var bullet)) {
                bullet.Shoot(direction, Owner);
                CanShoot = false;
                _timePassed = 0f;
            }
        }
    }
}