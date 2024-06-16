using Unity.Collections;
using UnityEngine;
using Unity.Profiling;
using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Random = UnityEngine.Random;

public unsafe class SpatialHashingTest : MonoBehaviour {
    private struct TestingEntity {
        public Vector3 Position;
        public int     Index;
    }

    public BurstedUnboundedSpatialTable HashTable;
    public int             EntitiesCount;
    public int             SearchesCount;
    public float           Radius = 10f;
    public float           HashTableSpacing = 3f;
    public Vector3         MaxSpawn = new Vector3(50, 50, 50);
    public Vector3         MinSpawn = new Vector3(-50, -50, -50);
    private NativeArray<TestingEntity> _entities;
    private NativeArray<Vector3> _positions;
    private NativeArray<float>   _rads;
    private NativeArray<int> _counts;

    private readonly ProfilerMarker Rehash = new ProfilerMarker("Rehash");
    private readonly ProfilerMarker Search = new ProfilerMarker("Search");
    private readonly ProfilerMarker MovingEntities = new ProfilerMarker(nameof(MovingEntities));


    private void Start() {
        HashTable = new BurstedUnboundedSpatialTable(EntitiesCount, HashTableSpacing);
        _entities = new NativeArray<TestingEntity>(EntitiesCount, Allocator.Persistent);
        _positions = new NativeArray<Vector3>(SearchesCount, Allocator.Persistent);
        _rads = new NativeArray<float>(SearchesCount, Allocator.Persistent);
        _counts = new NativeArray<int>(SearchesCount, Allocator.Persistent);
        
        for(var i = 0; i < EntitiesCount; ++i) {
            var position = new Vector3(
                Random.Range(MinSpawn.x + 10f, MaxSpawn.x - 10f),
                Random.Range(MinSpawn.y + 10f, MaxSpawn.y - 10f),
                Random.Range(MinSpawn.z + 10f, MaxSpawn.z - 10f)
            );
            _entities[i] = new TestingEntity {
                Position = position,
                Index = i
            };
            HashTable.AddEntity(i, position);
        }

        for(var i = 0; i < SearchesCount; ++i) {
            _positions[i] = new Vector3(
                Random.Range(MinSpawn.x, MaxSpawn.x),
                Random.Range(MinSpawn.y, MaxSpawn.y),
                Random.Range(MinSpawn.z, MaxSpawn.z)
            );
        }

        for (var i = 0; i < SearchesCount; ++i) {
            _rads[i] = Radius;
        }

        _buffers = (NativeArray<int>*)UnsafeUtility.MallocTracked(sizeof(NativeArray<int>) * 
            SearchesCount, 
            UnsafeUtility.AlignOf<NativeArray<int>>(),
            Allocator.Persistent, 0);

        for(var i = 0; i < SearchesCount; ++i) {
            _buffers[i] = new NativeArray<int>(128, Allocator.Persistent);
        }
    }

    private void OnDestroy() {
        HashTable.Dispose();
        _buffer.Dispose();

        for(var i = 0; i < SearchesCount; ++i) {
            _buffers[i].Dispose();
        }

        UnsafeUtility.FreeTracked(_buffers, Allocator.Persistent);
    }

    private NativeArray<int> _buffer = new (128, Allocator.Persistent);
    private NativeArray<int> *_buffers;


    private void Update() {
        MovingEntities.Begin();
        for(var i = 0; i < EntitiesCount; ++i) {
            _entities[i] = new TestingEntity {
                Position = _entities[i].Position + Vector3.up * Time.deltaTime,
                Index = _entities[i].Index
            };
            HashTable.UpdatePosition(_entities[i].Index, _entities[i].Position);
        }
        MovingEntities.End();
        Rehash.Begin(); //0.5ms
        HashTable.Rehash();
        Rehash.End();

        Search.Begin(); // 5.5 - 6ms / multithreaded: 0.61ms
        HashTable.Query(_positions, _rads, _buffers, _counts);
        for (var i = 0; i < SearchesCount; ++i) {
            var count = _counts[i];
        
            for (var j = 0; j < count; ++j) {
                Debug.DrawLine(_positions[i], _entities[_buffers[i][j]].Position);
            }
        }
        // for(var i = 0; i < SearchesCount; ++i) { 
        //     var count = HashTable.Query(_positions[i], _buffer, Radius);
        //     for (var j = 0; j < count; ++j) {
        //         Debug.DrawLine(_positions[i], _entities[_buffer[j]].Position);
        //     }
        // }
        Search.End();
    }
}