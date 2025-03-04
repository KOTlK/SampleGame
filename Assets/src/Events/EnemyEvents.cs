using TMPro;
using System;

public struct EnemyDiedEvent : IEvent {
    public uint killer;
}

public class EnemyDiedEventHandler : IDisposable {
    public TMP_Text KilledEnemiesText;
    public TMP_Text KilledEnemiesByPlayerText;
    public int      EnemiesKilledByPlayer;
    public int      KilledEnemies;
    
    public EnemyDiedEventHandler() {
        Events.SubscribeToPrivate<Enemy, EnemyDiedEvent>(HandleEvents);
    }
    
    public void Dispose() {
        Events.UnsubscribeFromPrivate<Enemy, EnemyDiedEvent>(HandleEvents);
    }
    
    public void HandleEvents(IEvent evnt) {
        var player = Singleton<Player>.Instance;

        var data = (EnemyDiedEvent)evnt;
        
        KilledEnemies++;
        
        if(data.killer == player.Handle.Id){
            EnemiesKilledByPlayer++;
        }
        
        KilledEnemiesText.text         = KilledEnemies.ToString();
        KilledEnemiesByPlayerText.text = EnemiesKilledByPlayer.ToString();
    }
}