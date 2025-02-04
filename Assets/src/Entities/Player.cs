using UnityEngine;

public class Player : Character {
    public Transform WeaponSlot;
    
    private EntityHandle _weapon;

    public override void Save(ISaveFile sf) {
        base.Save(sf);
        sf.WriteObject(_weapon, nameof(_weapon));
    }

    public override void Load(ISaveFile sf) {
        base.Load(sf);
        _weapon = sf.ReadValueType<EntityHandle>(nameof(_weapon));
        Singleton<SaveSystem>.Instance.LoadingOver += LoadWeapon;
    }

    public void GiveWeapon(EntityHandle weapon) {
        _weapon = weapon;
        if(Em.GetEntity<Weapon>(weapon, out var e)) {
            e.AttachToSlot(WeaponSlot);
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

    private void LoadWeapon(ISaveFile sf) {
        Singleton<SaveSystem>.Instance.LoadingOver -= LoadWeapon;
        GiveWeapon(_weapon);
    }
}
