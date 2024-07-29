using UnityEngine;

public class Player : Character {
    public Weapon    WeaponPrefab;
    public Transform WeaponSlot;
    
    private EntityHandle _weapon;

    public override void OnCreate() {
        _weapon = Em.CreateEntity(WeaponPrefab, 
                                          WeaponSlot.position, 
                                          Quaternion.identity, 
                                          Vector3.one, 
                                          WeaponSlot);
        if (Em.GetEntity<Weapon>(_weapon, out var e)) {
            e.Owner = Em.GetHandle(Id);
        }
    }
    
    public override void Execute() {
        base.Execute();
        if(Input.Shooting) {
            if(Em.GetEntity<Weapon>(_weapon, out var e)) {
                e.Shoot(new Vector3(Mathf.Sin(Input.LookDirection * Mathf.Deg2Rad), 
                                    0, 
                                    Mathf.Cos(Input.LookDirection * Mathf.Deg2Rad)));
            }
        }
    }
}
