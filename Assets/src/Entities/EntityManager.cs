using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static ArrayUtils;
using static Assertions;

public struct MovedEntity {
    public uint     Id;
    public Vector3  NewPosition;
}

[Serializable]
public struct PackedEntity {
    public Entity        Entity;
    public EntityType    Type;
    public uint          Tag; // Unique identifier for slot
    public bool          Alive;
}

public struct EntityHandle : ISave {
    public uint Id;
    public uint Tag;

    public static readonly EntityHandle Zero = new EntityHandle { Id = 0, Tag = 0};
    
    public static bool operator==(EntityHandle lhs, EntityHandle rhs) {
        return lhs.Id == rhs.Id && lhs.Tag == rhs.Tag;
    }
    
    public static bool operator!=(EntityHandle lhs, EntityHandle rhs) {
        return !(lhs == rhs);
    }

    public override bool Equals(object obj)
    {
        return (EntityHandle)obj == this;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Tag);
    }

    public void Save(ISaveFile sf) {
        sf.Write(Id, nameof(Id));
        sf.Write(Tag, nameof(Tag));
    }

    public void Load(ISaveFile sf) {
        Id  = sf.Read<uint>(nameof(Id));
        Tag = sf.Read<uint>(nameof(Tag));
    }
}

public class EntityManager : MonoBehaviour, ISave {
    public World                                        World;
    public List<Entity>                                 BakedEntities;
    public List<MovedEntity>                            MovedEntities           = new ();
    public Dictionary<EntityType, List<EntityHandle>>   EntitiesByType          = new();
    public Dictionary<int, EntityHandle>                EntityByInstanceId      = new(); // GetEntity by Unity InstanceId
    public PackedEntity[]                               Entities                = new PackedEntity[128];
    public List<uint>                                   DynamicEntities         = new ();
    public uint[]                                       RemoveQueue             = new uint[128];
    public uint[]                                       FreeEntities            = new uint[128];
    [HideInInspector] 
    public uint                                         MaxEntitiesCount        = 1;
    [HideInInspector]
    public uint                                         CurrentTag              = 1;
    public uint                                         FreeEntitiesCount;
    public uint                                         EntitiesToRemoveCount;

    private void Awake() {
        World.Create();
        var entityTypes = Enum.GetValues(typeof(EntityType));

        foreach(var type in entityTypes) {
            EntitiesByType.Add((EntityType)type, new List<EntityHandle>());
        }
    }

    private void Start() {
        Console.RegisterCommand<EntityManager>(nameof(DestroyEntity), this, "destroy_entity");
        Console.RegisterCommand<EntityManager>(nameof(DestroyEntityImmediate), this, "destroy_entity_immediate");
    }

    public virtual void Save(ISaveFile sf) { // @Incomplete Save and Load World?
        sf.Write(MaxEntitiesCount, nameof(MaxEntitiesCount));
        sf.Write(CurrentTag, nameof(CurrentTag));
        for(uint i = 1; i < MaxEntitiesCount; ++i) {
            sf.WritePackedEntity(Entities[i], i, $"EntityNum{i}");
        }
    }

    public virtual void Load(ISaveFile sf) {
        // Clear everything
        for(uint i = 0; i < MaxEntitiesCount; ++i) {
            DestroyEntityImmediate(i);
        }
        
        MaxEntitiesCount      = 1;
        FreeEntitiesCount     = 0;
        EntitiesToRemoveCount = 0;
        MovedEntities.Clear();
        DynamicEntities.Clear();

        var entitiesCount = sf.Read<uint>(nameof(MaxEntitiesCount));
        CurrentTag        = sf.Read<uint>(nameof(CurrentTag));
        Entities          = new PackedEntity[entitiesCount];
        for(var i = 1; i < entitiesCount; ++i) {
            Entities[i] = sf.ReadPackedEntity(this, $"EntityNum{i}");
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
        uint id;
        if(FreeEntitiesCount > 0){
            id = FreeEntities[--FreeEntitiesCount];
        }else{
            id = MaxEntitiesCount++;
        }

        var handle = new EntityHandle {
            Id = id,
            Tag = tag
        };
        
        if(MaxEntitiesCount == Entities.Length) {
            Resize(ref Entities, MaxEntitiesCount << 1);
        }
        
        Entities[id].Entity  = entity;
        Entities[id].Alive   = true;
        Entities[id].Type    = entity.Type;
        Entities[id].Tag     = tag;
        
        entity.Handle      = handle;
        entity.Em          = this;
        entity.World       = World;

        EntitiesByType[entity.Type].Add(handle);
        
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
        
        entity.OnBaking();
        entity.RegisterInstanceId(this);
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

    public EntityHandle CreateEntity(ResourceLink link,
                                     Vector3 position, 
                                     Quaternion orientation) {
        var prefab = Singleton<ResourceSystem>.Instance.Load<Entity>(link);
        var handle = CreateEntity(prefab, position, orientation, null);
        if(GetEntity(handle, out var e)) {
            e.transform.localScale = prefab.transform.localScale;
        }
        return handle;
    }

    public EntityHandle CreateEntity(ResourceLink link,
                                     Vector3 position, 
                                     Quaternion orientation,
                                     Transform parent) {
        var prefab = Singleton<ResourceSystem>.Instance.Load<Entity>(link);
        var handle = CreateEntity(prefab, position, orientation, parent);
        if(GetEntity(handle, out var e)) {
            e.transform.localScale = prefab.transform.localScale;
        }
        return handle;
    }

    public EntityHandle CreateEntity(Entity prefab,
                                     Vector3 position,
                                     Quaternion orientation, 
                                     Vector3 scale,
                                     Transform parent) {
        var handle = CreateEntity(prefab, position, orientation, parent);
        if(GetEntity(handle, out var e)) {
            e.transform.localScale = scale;
        }
        return handle;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityHandle CreateEntity(Entity prefab, 
                                     Vector3 position, 
                                     Quaternion orientation, 
                                     Transform parent) {
        uint id;
        uint tag = GetTag();
        
        if(FreeEntitiesCount > 0) {
            id = FreeEntities[--FreeEntitiesCount];
        }else{
            id = MaxEntitiesCount++;
        }

        var handle = new EntityHandle {
            Id = id,
            Tag = tag
        };
        
        var obj = Instantiate(prefab, position, orientation, parent);
        
        if(MaxEntitiesCount == Entities.Length) {
            Resize(ref Entities, MaxEntitiesCount << 1);
        }
        
        Entities[id].Entity  = obj;
        Entities[id].Alive   = true;
        Entities[id].Type    = obj.Type;
        Entities[id].Tag     = tag;
        
        obj.Handle      = handle;
        obj.Em          = this;
        obj.World       = World;

        EntitiesByType[obj.Type].Add(handle);
        
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
        obj.RegisterInstanceId(this);
        
        return handle;
    }

    public void PushEmptyEntity(uint id) {
        FreeEntities[FreeEntitiesCount++] = id;
        Entities[id].Alive   = false;
        MaxEntitiesCount++;
    }

    public Entity RecreateEntity(ResourceLink link, 
                                 uint        tag, 
                                 Vector3     position, 
                                 Quaternion  orientation,
                                 Vector3     scale,
                                 EntityType  type,
                                 EntityFlags flags) {
        var resource = Singleton<ResourceSystem>.Instance.Load<Entity>(link);
        var e        = Instantiate(resource, position, orientation);
        e.transform.localScale = scale;

        uint id = MaxEntitiesCount++;
        
        if(MaxEntitiesCount == Entities.Length) {
            Resize(ref Entities, MaxEntitiesCount << 1);
        }
        var handle = new EntityHandle {
            Id = id,
            Tag = tag
        };
        
        Entities[id].Entity  = e;
        Entities[id].Alive   = true;
        Entities[id].Type    = type;
        Entities[id].Tag     = tag;
        
        e.Handle = handle;
        e.Em     = this;
        e.World  = World;
        e.Type   = type;
        e.Flags  = flags;

        EntitiesByType[e.Type].Add(handle);
        
        if((e.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
            DynamicEntities.Add(id);
        }

        if((e.Flags & EntityFlags.InsideHashTable) == EntityFlags.InsideHashTable) {
            if((e.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
                World.AddDynamicEntity(id, position);
            } else if ((e.Flags & EntityFlags.Static) == EntityFlags.Static) {
                World.AddStaticEntity(id, position);
            }
        }
        
        e.OnCreate();
        e.RegisterInstanceId(this);
        
        return e;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyEntity(EntityHandle handle) {
        if(EntitiesToRemoveCount == RemoveQueue.Length) {
            Resize(ref RemoveQueue, EntitiesToRemoveCount << 1);
        }

        if(IsValid(handle)) {
            Entities[handle.Id].Alive = false;
            RemoveQueue[EntitiesToRemoveCount++] = handle.Id;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyEntityImmediate(uint id) {
        var entity = Entities[id].Entity;
        
        if(entity != null){
            if(FreeEntitiesCount == FreeEntities.Length) {
                Resize(ref FreeEntities, FreeEntitiesCount << 1);
            }

            EntitiesByType[entity.Type].Remove(GetHandle(id));
            
            if((entity.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
                DynamicEntities.Remove(id);
            }

            if((entity.Flags & EntityFlags.InsideHashTable) == EntityFlags.InsideHashTable) {
                if((entity.Flags & EntityFlags.Dynamic) == EntityFlags.Dynamic) {
                    World.RemoveDynamicEntity(entity.Handle.Id);
                } else if((entity.Flags & EntityFlags.Static) == EntityFlags.Static) {
                    World.RemoveStaticEntity(entity.Handle.Id);
                }
            }
            
            Entities[id].Entity = null;
            entity.UnRegisterInstanceId(this);
#pragma warning disable 0618
            entity.Destroy();
#pragma warning restore 0618
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
    public bool GetEntityByInstanceId<T>(int instanceId, out T entity) 
    where T : Entity {
        if(EntityByInstanceId.ContainsKey(instanceId)) {
            Assert(Entities[EntityByInstanceId[instanceId].Id].Entity is T);
            entity = (T)Entities[EntityByInstanceId[instanceId].Id].Entity;
            return true;
        }else {
            entity = null;
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid(EntityHandle handle) {
        return handle != EntityHandle.Zero && handle.Tag == Entities[handle.Id].Tag;
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
            CurrentTag = 1;
        }
        
        return CurrentTag++;
    }
}