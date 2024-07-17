using UnityEngine;
using TMPro;
using System.Collections;
using static Assertions;

public enum GameState {
    MainMenu,
    Gameplay,
    Pause,
    GameEnd
}

public class Main : MonoBehaviour {
    public TMP_Text      KilledEnemiesText;
    public TMP_Text      KilledEnemiesByPlayerText;
    public TMP_Text      DebugText;
    public Player        Player;
    public PlayerInput   PlayerInput;
    public EntityManager EntityManager;
    public TaskRunner    TaskRunner;
    public EnemySpawner  SpawnerTask;
    public Events        Events;
    public Projectile    ProjectilePrefab;
    
    private GameState _state;
    
    private void Awake() {
        TaskRunner = new TaskRunner();
        Events     = new Events();
        
        Singleton<EntityManager>.Create(EntityManager);
        Singleton<TaskRunner>.Create(TaskRunner);
        Singleton<Events>.Create(Events);
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
    }
    
    private void Update() {
        Clock.Update();
        TaskRunner.RunTaskGroup(TaskGroupType.ExecuteAlways);
        switch(_state) {
            case GameState.MainMenu:
            {
                if(Input.GetKeyDown(KeyCode.Tab)) {
                    ToGameplay();
                }
            }
            break;
            
            case GameState.Gameplay:
            {
                if(Input.GetKeyDown(KeyCode.Escape)) {
                    Pause();
                    break;
                }
                PlayerInput.Execute();
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
}
