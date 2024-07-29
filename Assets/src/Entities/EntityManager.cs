using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static ArrayUtils;

public struct MovedEntity {
    public uint     Id;
    public Vector3  NewPosition;
}

[Serializable]
public struct PackedEntity {
    public Entity        Entity;
    public EntityManager Manager;
    public EntityType    Type;
    public uint          Tag; // Unique identifier for slot
    public bool          Alive;
}

public struct EntityHandle {
    public uint   Id;
    public uint   Tag;
}

public class EntityManager : MonoBehaviour, ISave {
    public World             World;
    public List<Entity>      BakedEntities;
    public List<MovedEntity> MovedEntities   = new ();
    public Dictionary<EntityType, List<uint>> EntitiesByType = new();
    public PackedEntity[]    Entities        = new PackedEntity[128];
    public List<uint>        DynamicEntities = new ();
    public uint[]            RemoveQueue     = new uint[128];
    public uint[]            FreeEntities    = new uint[128];
    [HideInInspector] 
    public uint              MaxEntitiesCount = 1;
    public uint              CurrentTag = 0;
    public uint              FreeEntitiesCount;
    public uint              EntitiesToRemoveCount;

    private void Awake() {
        World.Create();
        var entityTypes = Enum.GetValues(typeof(EntityType));

        foreach(var type in entityTypes) {
            EntitiesByType.Add((EntityType)type, new List<uint>());
        }
    }

    public virtual void Save(SaveFile sf) { // @Incomplete Save and Load World?
        sf.Write(nameof(MaxEntitiesCount), MaxEntitiesCount);
        sf.Write(nameof(CurrentTag), CurrentTag);
        for(var i = 0; i < MaxEntitiesCount; ++i) {
            sf.Write($"EntityNum{i}", Entities[i]);
        }
    }

    public virtual void Load(SaveFile sf) {
        // Clear everything
        for(uint i = 0; i < MaxEntitiesCount; ++i) {
            DestroyEntityImmediate(i);
        }
        
        MaxEntitiesCount      = 1;
        FreeEntitiesCount     = 0;
        EntitiesToRemoveCount = 0;
        MovedEntities.Clear();
        DynamicEntities.Clear();

        MaxEntitiesCount = sf.ReadUInt(nameof(MaxEntitiesCount));
        CurrentTag       = sf.ReadUInt(nameof(CurrentTag));
        Entities         = new PackedEntity[MaxEntitiesCount];
        for(var i = 0; i < MaxEntitiesCount; ++i) {
            Entities[i] = sf.ReadPackedEntity($"EntityNum{i}", this);
        }
    }
        
    public void BakeEntities() {
        for(var i = 0; i < BakedEntities.Count; ++i) {
            BakeEntity(BakedEntities[i]);
        }
        
        BakedEntities.Clear();
    }
    
    public void BakeEntity(Entity entity) {
        uint tag = GetTag();
        uint id = 0;
        if(FreeEntitiesCount > 0){
            id = FreeEntities[--FreeEntitiesCount];
        }else{
            id = MaxEntitiesCount++;
        }
        
        if(MaxEntitiesCount == Entities.Length) {
            Resize(ref Entities, MaxEntitiesCount << 1);
        }
        
        Entities[id].Entity  = entity;
        Entities[id].Alive   = true;
        Entities[id].Type    = entity.Type;
        Entities[id].Manager = this;
        Entities[id].Tag     = tag;
        
        entity.Id          = id;
        entity.Em          = this;
        entity.World       = World;

        EntitiesByType[entity.Type].Add(id);
        
        if((entity.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
            DynamicEntities.Add(id);
        }

        if((entity.Flags & EntityFlags.InsideHashTable) == EntityFlags.InsideHashTable) {
            if((entity.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
                World.AddDynamicEntity(id, entity.transform.position);
            } else if ((entity.Flags & EntityFlags.Static) == EntityFlags.Static) {
                World.AddStaticEntity(id, entity.transform.position);
            }
        }
        
        entity.OnCreate();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityHandle CreateEntity(Entity prefab, Vector3 position) {
        return CreateEntity(prefab, position, Quaternion.identity, Vector3.one, null);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityHandle CreateEntity(Entity prefab, 
                               Vector3 position, 
                               Quaternion orientation, 
                               Vector3 scale) {
        return CreateEntity(prefab, position, orientation, scale, null);
    }

    public EntityHandle CreateEntity(string prefabPath) {
        return CreateEntity(prefabPath, Vector3.zero, Quaternion.identity, Vector3.one, null);
    }

    public EntityHandle CreateEntity(string prefabPath,
                               Vector3 position, 
                               Quaternion orientation,
                               Vector3 scale,
                               Transform parent) {
        var prefab = Resources.Load<Entity>(prefabPath); // @Unfinished temporary, until resource system is ready
        Destroy(prefab);
        return CreateEntity(prefab, position, orientation, scale, parent);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityHandle CreateEntity(Entity prefab, 
                                     Vector3 position, 
                                     Quaternion orientation, 
                                     Vector3 scale, 
                                     Transform parent) {
        uint id = 0;
        uint tag = GetTag();
        
        if(FreeEntitiesCount > 0) {
            id = FreeEntities[--FreeEntitiesCount];
        }else{
            id = MaxEntitiesCount++;
        }
        
        var obj = Instantiate(prefab, position, orientation, parent);
        
        if(MaxEntitiesCount == Entities.Length) {
            Resize(ref Entities, MaxEntitiesCount << 1);
        }
        
        Entities[id].Entity  = obj;
        Entities[id].Alive   = true;
        Entities[id].Type    = obj.Type;
        Entities[id].Manager = this;
        Entities[id].Tag     = tag;
        
        obj.Id     = id;
        obj.Em          = this;
        obj.World       = World;

        EntitiesByType[obj.Type].Add(id);
        
        if((obj.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
            DynamicEntities.Add(id);
        }

        if((obj.Flags & EntityFlags.InsideHashTable) == EntityFlags.InsideHashTable) {
            if((obj.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
                World.AddDynamicEntity(id, position);
            } else if ((obj.Flags & EntityFlags.Static) == EntityFlags.Static) {
                World.AddStaticEntity(id, position);
            }
        }
        
        obj.OnCreate();
        
        return new EntityHandle {
            Id = id,
            Tag = tag,
        };
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyEntity(uint id) {
        if(EntitiesToRemoveCount == RemoveQueue.Length) {
            Resize(ref RemoveQueue, EntitiesToRemoveCount << 1);
        }
        
        Entities[id].Alive = false;
        RemoveQueue[EntitiesToRemoveCount++] = id;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyEntityImmediate(uint id) {
        var entity = Entities[id].Entity;
        
        if(entity != null){
            if(FreeEntitiesCount == FreeEntities.Length) {
                Resize(ref FreeEntities, FreeEntitiesCount << 1);
            }

            EntitiesByType[entity.Type].Remove(id);
            
            if((entity.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
                DynamicEntities.Remove(id);
            }

            if((entity.Flags & EntityFlags.InsideHashTable) == EntityFlags.InsideHashTable) {
                if((entity.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
                    World.RemoveDynamicEntity(entity.Id);
                } else if((entity.Flags & EntityFlags.Static) == EntityFlags.Static) {
                    World.RemoveStaticEntity(entity.Id);
                }
            }
            
            Entities[id].Entity = null;
            entity.Destroy();
            FreeEntities[FreeEntitiesCount++] = id;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyAllEntities() {
        for(uint i = 0; i < MaxEntitiesCount; ++i) {
            DestroyEntityImmediate(i);
        }
        
        MaxEntitiesCount      = 1;
        FreeEntitiesCount     = 0;
        EntitiesToRemoveCount = 0;
        MovedEntities.Clear();
        DynamicEntities.Clear();
        World.Dispose();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Execute() {
        if(MovedEntities.Count > 0) {
            for(var i = 0; i < MovedEntities.Count; ++i) {
                World.UpdateDynamicEntityPosition(MovedEntities[i].Id, MovedEntities[i].NewPosition);
            }
            MovedEntities.Clear();
        }

        for(var i = 0; i < EntitiesToRemoveCount; ++i) {
            DestroyEntityImmediate(RemoveQueue[i]);
        }
        EntitiesToRemoveCount = 0;

        World.Execute();
        
        for(var i = 0; i < DynamicEntities.Count; ++i) {
            Entities[DynamicEntities[i]].Entity.Execute();
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetEntity(EntityHandle handle, out Entity e) {
        if(IsValid(handle)) {
            e = Entities[handle.Id].Entity;
            return true;
        } else {
            e = null;
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetEntity<T>(EntityHandle handle, out T e) 
    where T : Entity {
        if(IsValid(handle)) {
            e = (T)Entities[handle.Id].Entity;
            return true;
        } else {
            e = null;
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityHandle GetHandle(uint id) {
        return new EntityHandle {
            Id = id,
            Tag = Entities[id].Tag
        };
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAlive(uint id) {
        return Entities[id].Alive;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid(EntityHandle handle) {
        return handle.Tag == Entities[handle.Id].Tag;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<Entity> GetAllEntitiesWithType(EntityType type) {
        for(var i = 0; i < MaxEntitiesCount; ++i) {
            if(Entities[i].Type == type && Entities[i].Alive) {
                yield return Entities[i].Entity;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> GetAllEntitiesWithType<T>(EntityType type) 
    where T : Entity {
        for(var i = 0; i < MaxEntitiesCount; ++i) {
            if(Entities[i].Type == type && Entities[i].Alive) {
                yield return (T)Entities[i].Entity;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint GetTag() {
        if(CurrentTag == uint.MaxValue) {
            CurrentTag = 0;
        }
        
        return CurrentTag;
    }
}