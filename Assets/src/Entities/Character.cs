using UnityEngine;

[System.Serializable]
public struct Damage : ISave {
    public int  amount;
    public EntityHandle sender;

    public void Save(ISaveFile sf) {
        sf.Write(nameof(amount), amount);
        sf.WriteObject(nameof(sender), sender);
    }

    public void Load(ISaveFile sf) {
        amount = sf.Read<int>(nameof(amount));
        sender = sf.ReadValueType<EntityHandle>(nameof(sender));
    }
}

public class Character : Entity {
    public float               Speed;
    public int                 Health;
    public CharacterInput      Input;
    public CharacterController CharacterController;

    public bool                IsDead => Health <= 0;

    public override void OnCreate() {
        base.OnCreate();
        Input = GetComponent<CharacterInput>();
        CharacterController = GetComponent<CharacterController>();
    }

    public override void OnBaking()
    {
        OnCreate();
    }

    public override void Save(ISaveFile sf) {
        base.Save(sf);
        sf.Write(nameof(Speed), Speed);
        sf.Write(nameof(Health), Health);
    }

    public override void Load(ISaveFile sf) {
        base.Load(sf);
        Speed = sf.Read<float>(nameof(Speed));
        Health = sf.Read<int>(nameof(Health));
    }

    public override void Execute() {
        Input.Execute();
        Walk(Input.MoveDirection * Speed);
        Rotate(Quaternion.AngleAxis(Input.LookDirection, Vector3.up));
    }
    
    public virtual void Walk(Vector3 move) {
        CharacterController.SimpleMove(move);
        Em.MovedEntities.Add(new MovedEntity{
            Id = Id, 
            NewPosition = transform.position
        });
    }
    
    public virtual void Rotate(Quaternion rotation) {
        transform.rotation = rotation;
    }
    
    public virtual void ApplyDamage(Damage damage) {
        Health -= damage.amount;
        if(Health <= 0) {
            Em.DestroyEntity(Id);
        }
    }
}
