using UnityEngine;

public class Main : MonoBehaviour{
    public Player        Player;
    public PlayerInput   PlayerInput;
    public EntityManager EntityManager;
    public TaskRunner    TaskRunner;
    public EnemySpawner  SpawnerTask;
    
    private void Awake(){
        Singleton<EntityManager>.Create(EntityManager);
        Singleton<TaskRunner>.Create(TaskRunner);
        Singleton<Player>.Create(Player);
    }
    
    private void Start(){
        EntityManager.BakeEntities();
        
        TaskRunner.StartTask(TaskGroupType.Gameplay, SpawnerTask);
    }
    
    private void Update(){
        PlayerInput.Execute();
        EntityManager.Execute();
        
        TaskRunner.RunTaskGroup(TaskGroupType.Gameplay);
    }
}
