using UnityEngine;

public enum GameState{
    MainMenu,
    Gameplay,
    Pause,
    GameEnd
}

public class Main : MonoBehaviour{
    public Player        Player;
    public PlayerInput   PlayerInput;
    public EntityManager EntityManager;
    public TaskRunner    TaskRunner;
    public EnemySpawner  SpawnerTask;
    
    private GameState _state;
    
    private void Awake(){
        TaskRunner = new TaskRunner();
        Singleton<EntityManager>.Create(EntityManager);
        Singleton<TaskRunner>.Create(TaskRunner);
        Singleton<Player>.Create(Player);
    }
    
    private void Start(){
        EntityManager.BakeEntities();
        ToMainMenu();
        TaskRunner.StartTask(TaskGroupType.Gameplay, SpawnerTask);
    }
    
    private void Update(){
        TaskRunner.RunTaskGroup(TaskGroupType.ExecuteAlways);
        switch(_state){
            case GameState.MainMenu:
            {
                if(Input.GetKeyDown(KeyCode.Space)){
                    ToGameplay();
                }
            }
            break;
            
            case GameState.Gameplay:
            {
                if(Input.GetKeyDown(KeyCode.Escape)){
                    Pause();
                    break;
                }
                PlayerInput.Execute();
                EntityManager.Execute();
                TaskRunner.RunTaskGroup(TaskGroupType.Gameplay);
            }
            break;
            
            case GameState.Pause:
            {
                if(Input.GetKeyDown(KeyCode.Escape)){
                    ToGameplay();
                    break;
                }
            }
            break;
            
            case GameState.GameEnd:
            {
                
            }
            break;
        }
        
    }
    
    private void ToMainMenu(){
        _state = GameState.MainMenu;
        
    }
    
    private void ToGameplay(){
        _state = GameState.Gameplay;
        
    }
    
    private void Pause(){
        _state = GameState.Pause;
        
    }
    
    private void EndGame(){
        _state = GameState.GameEnd;
        
    }
}
