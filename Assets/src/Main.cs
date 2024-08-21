using UnityEngine;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum GameState {
    MainMenu,
    Gameplay,
    Pause,
    GameEnd
}

public class Main : MonoBehaviour {
    public TMP_Text       KilledEnemiesText;
    public TMP_Text       KilledEnemiesByPlayerText;
    public TMP_Text       DebugText;
    public ResourceLink   PlayerWeapon;
    public Player         Player;
    public EntityManager  EntityManager;
    public TaskRunner     TaskRunner;
    public EnemySpawner   SpawnerTask;
    public Events         Events;
    public ResourceSystem ResourceSystem;
    public SaveSystem SaveSystem;
    
    private GameState _state;
    
    private void Awake() {
        TaskRunner = new TaskRunner();
        Events     = new Events();
        ResourceSystem = new ResourceSystem();
        SaveSystem     = new SaveSystem();
        
        Singleton<EntityManager>.Create(EntityManager);
        Singleton<TaskRunner>.Create(TaskRunner);
        Singleton<Events>.Create(Events);
        Singleton<ResourceSystem>.Create(ResourceSystem);
        Singleton<SaveSystem>.Create(SaveSystem);
        Singleton<Player>.Create(Player);
        
        var edeh = new EnemyDiedEventHandler();
        edeh.KilledEnemiesText         = KilledEnemiesText;
        edeh.KilledEnemiesByPlayerText = KilledEnemiesByPlayerText;
    }
    
    private void Start() {
        EntityManager.BakeEntities();
        ToMainMenu();
        TaskRunner.StartTask(TaskGroupType.Gameplay, SpawnerTask);
    }
    
    private void OnDestroy() {
        Events.Dispose();
        SaveSystem.Dispose();
    }
    
    private void Update() {
        Clock.Update();
        TaskRunner.RunTaskGroup(TaskGroupType.ExecuteAlways);
        switch(_state) {
            case GameState.MainMenu:
            {
                if(Input.GetKeyDown(KeyCode.Tab)) {
                    ToGameplay();
                    Player.GiveWeapon(EntityManager.CreateEntity(PlayerWeapon, Player.WeaponSlot.position, Player.WeaponSlot.rotation));
                }
            }
            break;
            
            case GameState.Gameplay:
            {
                if(Input.GetKeyDown(KeyCode.Escape)) {
                    Pause();
                    break;
                }

                if(Input.GetKeyDown(KeyCode.F5)) {
                    var sf = SaveSystem.BeginSave();
                    //Save game
                    sf.WriteObject(nameof(EntityManager), EntityManager);

                    SaveSystem.EndSave(Application.persistentDataPath, "GameSave");
                    break;
                }

                if(Input.GetKeyDown(KeyCode.F9)) {
                    var sf = SaveSystem.BeginLoading($"{Application.persistentDataPath}/GameSave.sav");
                    //load game

                    sf.ReadObject(nameof(EntityManager), EntityManager);

                    SaveSystem.EndLoading();
                    break;
                }

                Events.HandleEvents<EnemyDiedEvent>();
                EntityManager.Execute();
                TaskRunner.RunTaskGroup(TaskGroupType.Gameplay);
            }
            break;
            
            case GameState.Pause:
            {
                if(Input.GetKeyDown(KeyCode.Escape)) {
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
    
    private void ToMainMenu() {
        _state = GameState.MainMenu;
    }
    
    private void ToGameplay() {
        _state = GameState.Gameplay;
    }
    
    private void Pause() {
        _state = GameState.Pause;
        
    }
    
    private void EndGame() {
        _state = GameState.GameEnd;
        
    }

    [ConsoleCommand("quit")]
    public static void Quit() {
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }
}
