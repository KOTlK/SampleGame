using UnityEngine;

[System.Serializable]
public struct Damage {
    public int amount;
    public int sender;
}

public class Character : Entity {
    public float               Speed;
    public int                 Health;
    public CharacterInput      Input;
    public CharacterController CharacterController;

    public bool                IsDead => Health <= 0;
    
    public override void Execute() {
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
