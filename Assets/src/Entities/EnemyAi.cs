using UnityEngine;

public class EnemyAi : CharacterInput {
    public Enemy Enemy;
    
    private void Awake(){
        if(Singleton<Player>.Exist) {
            Enemy.Target = Singleton<Player>.Instance.Id;
        }
    }
    
    public override void Execute() {
        var (alive, target) = Enemy.Em.GetEntity<Character>(Enemy.Target);
        
        if(target != null && target.IsDead == false && alive) {
            var direction = target.transform.position - Enemy.transform.position;
            var angle     = -(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            
            direction.y   = 0;
            MoveDirection = direction.normalized;
            LookDirection = angle;
            
            if(direction.magnitude < Enemy.AttackRadius) {
                if(Enemy.Attack()) {
                    Enemy.Em.DestroyEntity(Enemy.Id);
                }
            }
        }
    }
}
