using TMPro;
using System;

public struct EnemyDiedEvent{
    public uint killer;
}

public class EnemyDiedEventHandler : IDisposable {
    public TMP_Text KilledEnemiesText;
    public TMP_Text KilledEnemiesByPlayerText;
    public int      EnemiesKilledByPlayer;
    public int      KilledEnemies;
    
    public EnemyDiedEventHandler(){
        Singleton<Events>.Instance.AddHandler<EnemyDiedEvent>(HandleEvents);
    }
    
    public void Dispose(){
        Singleton<Events>.Instance.RemoveHandler<EnemyDiedEvent>(HandleEvents);
    }
    
    public void HandleEvents(EventData events) {
        var player = Singleton<Player>.Instance;
        for(var i = 0; i < events.Count; ++i){
            ref var evnt = ref events.GetByRef<EnemyDiedEvent>(i);
            
            KilledEnemies++;
            
            if(evnt.killer == player.Id){
                EnemiesKilledByPlayer++;
            }
        }
        
        KilledEnemiesText.text         = KilledEnemies.ToString();
        KilledEnemiesByPlayerText.text = EnemiesKilledByPlayer.ToString();
    }
}