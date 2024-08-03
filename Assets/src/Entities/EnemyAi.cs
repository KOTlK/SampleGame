using UnityEngine;

public class EnemyAi : CharacterInput {
    public Enemy Enemy;
    
    private void Start(){
        if(Singleton<Player>.Exist) {
            Enemy.Target = Enemy.Em.GetHandle(Singleton<Player>.Instance.Id);
        }
        Enemy = GetComponent<Enemy>();
    }
    
    public override void Execute() {
        if(Enemy.Em.GetEntity<Character>(Enemy.Target, out var target)) {
            if(target != null && target.IsDead == false) {
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
}
