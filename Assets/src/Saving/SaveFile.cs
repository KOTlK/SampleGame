using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Unity.Collections;
using System;

using static Assertions;

public class SaveFile {
    public struct Field {
        public string Name;
        public string Value;

        public Field(string name, string value) {
            Name = name;
            Value = value;
        }
    }

    public struct ObjectNode {
        public List<Field> Fields;
        public Dictionary<string, ObjectNode> NestedObjects;

        public static ObjectNode Create() {
            return new ObjectNode() {
                Fields = new(),
                NestedObjects = new()
            };
        }

        public void PushField(Field field) {
            Fields.Add(field);
        }

        public void PushObject(string name, ObjectNode node) {
            NestedObjects.Add(name, node);
        }
    }

    public float         Version;
    public string[]      LoadedLines;
    public int           LinesCount;
    public StringBuilder Sb = new();
    public int           CurrentOffset;
    public ObjectNode    Root;
    public Stack<ObjectNode> ObjectStack = new();

    public const int Offset = 4;
    public const string Extension = ".sav";
    public const string Separator = " : ";
    
    public void NewFile(float version) {
        Version = version;
        Write(nameof(Version), version);
        Sb.AppendLine();
    }
    
    public void SaveToFile(string path, string name) {
        path += $"/{name}{Extension}";
        if(File.Exists(path)) {
            File.Delete(path);
            File.CreateText(path).Close();
        }
        
        File.WriteAllText(path, Sb.ToString());
        // File.WriteAllText(path, Sb.ToString());
    }

    public void NewFromExistingFile(string path) {
        Assert(path.EndsWith(Extension), $"File should ends with {Extension}");
        Root = new ObjectNode
        {
            Fields = new(),
            NestedObjects = new()
        };

        if(File.Exists(path)) {
            LoadedLines = File.ReadAllLines(path);
            LinesCount = LoadedLines.Length;
        }

        //Parse file
        var versionLine = LoadedLines[0].TrimStart().TrimEnd();
        var nameObj = versionLine.Split(Separator);
        if(nameObj.Length == 2) {
            Assert(nameObj[0] == nameof(Version), "Can't read version, make sure the file is formated right");
            if(float.TryParse(nameObj[1], out var version)) {
                Version = version;
            } else {
                Debug.LogError("Can't parse version");
            }
        } else {
            Debug.LogError("Can't parse version");
        }

        Debug.Log(Version);

        var startLine = 1;

        while(startLine < LinesCount) {
            var line = LoadedLines[startLine].TrimStart().TrimEnd();
            var separateLine = line.Split(Separator);
            if(separateLine.Length > 1 && separateLine[1] == "{") {
                startLine++;
                SaveObject(separateLine[0], ParseObject(ref startLine, ObjectNode.Create()));
            } else if(separateLine.Length == 2) {
                Root.Fields.Add(new Field(separateLine[0], separateLine[1]));
                startLine++;
            } else {
                startLine++;
            }
        }

        Debug.Log(Root.NestedObjects.Count);
        Debug.Log(Root.Fields.Count);
    }

    public void Write(string name, ISave save) {
        BeginObject(name);
        save.Save(this);
        EndObject();
    }

    public void Write(string name, PackedEntity e, uint id) {
        BeginObject(name);
        Write("Id", id);
        Write(nameof(e.Tag), e.Tag);
        Write(nameof(e.Type), e.Type);
        WriteBool(nameof(e.Alive), e.Alive);
        if(e.Alive) {
            Write(nameof(e.Entity), e.Entity);
        }
        EndObject();
    }

    private void Write(string name, Entity e) {
        BeginObject(name);
        WriteBool(nameof(e.RecreateOnLoad), e.RecreateOnLoad);
        Write(nameof(e.Id), e.Id);
        Write(nameof(e.Type), e.Type);
        Write(nameof(e.Flags), e.Flags);
        Write(nameof(e.PrefabName), e.PrefabName);
        Write("Position", e.transform.position);
        Write("Orientation", e.transform.rotation);
        Write("Scale", e.transform.localScale);
        e.Save(this);
        EndObject();
    }

    public void Write(string name, Enum e) { // @Incomplete add all basic enum types
        var type = Enum.GetUnderlyingType(e.GetType()).ToString();

        switch(type) {
            case "System.Int32" : {
                Write(name, (int)Convert.ChangeType(e, typeof(int)));
            }
            break;
            default : {
                Debug.LogError($"Can't convert from {type} to any proper type");
            }
            break;
        }
    }

    public T ReadEnum<T>(string name) // @Incomplete add all basic enum types
    where T : Enum {
        var type = Enum.GetUnderlyingType(typeof(T)).ToString();

        switch(type) {
            case "System.Int32" : {
                var val = ReadInt(name);
                return (T)Enum.ToObject(typeof(T), val);
            }
            break;
        }

        return default;
    }

    public PackedEntity ReadPackedEntity(string name, EntityManager em) {
        BeginReadObject(name);
        var ent = new PackedEntity();
        var id  = ReadUInt("Id");
        ent.Tag = ReadUInt(nameof(ent.Tag));
        ent.Type = ReadEnum<EntityType>(nameof(ent.Type));
        ent.Alive = ReadBool(nameof(ent.Alive));
        if(ent.Alive) {
            ent.Entity = ReadEntity(nameof(ent.Entity), em, ent.Tag);
        } else {
            em.PushEmptyEntity(id);
        }
        EndReadObject();

        return ent;
    }
    
    private Entity ReadEntity(string name, EntityManager em, uint tag) {
        BeginReadObject(name);
        Entity entity = null;
        var recreate = ReadBool(nameof(entity.RecreateOnLoad));

        if(recreate) {
            var id     = ReadUInt(nameof(entity.Id));
            var type   = ReadEnum<EntityType>(nameof(entity.Type));
            var flags  = ReadEnum<EntityFlags>(nameof(entity.Flags));
            var link   = ReadValueType<ResourceLink>(nameof(entity.PrefabName));
            var position = ReadVector3("Position");
            var orientation = ReadQuaternion("Orientation");
            var scale = ReadVector3("Scale");
            entity = em.RecreateEntity(link, tag, position, orientation, scale, type, flags);
            entity.Load(this);
            Assert(id == entity.Id, $"Entity Id's are not identical while reading entity. Recreated Id: {entity.Id}, Saved Id: {id}");
        }
        
        EndReadObject();

        return entity;
    }

    public void ReadObject(string name, ISave obj) {
        BeginReadObject(name);
        obj.Load(this);
        EndReadObject();
    }

    public T ReadValueType<T>(string name) 
    where T : ISave {
        var ret = default(T);

        BeginReadObject(name);
        ret.Load(this);
        EndReadObject();

        return ret;
    }

    public T[] ReadObjectArray<T>(string name, Func<T> createObjectFunc) 
    where T : ISave {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr   = new T[count];

        for(var i = 0; i < count; ++i) {
            var obj = createObjectFunc();
            ReadObject($"{name}ArrayElement{i}", obj);
            arr[i] = obj;
        }

        EndReadObject();

        return arr;
    }

    public T[] ReadUnmanagedObjectArray<T>(string name) 
    where T : unmanaged, ISave {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr   = new T[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadValueType<T>($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public T[] ReadValueObjectArray<T>(string name) // @Untested
    where T : struct, ISave {
    BeginReadObject(name);
    var count = ReadInt("Count");
    var arr = new T[count];

    for(var i = 0; i < count; ++i) {
        arr[i] = ReadValueType<T>($"{name}ArrayElement{i}");
    }
    
    EndReadObject();

    return arr;
}

    public NativeArray<T> ReadNativeObjectArray<T>(string name, Allocator allocator) // @Untested
    where T : unmanaged, ISave {
    BeginReadObject(name);
    var count = ReadInt("Count");
    var arr = new NativeArray<T>(count, allocator);

    for(var i = 0; i < count; ++i) {
        arr[i] = ReadValueType<T>($"{name}ArrayElement{i}");
    }
    
    EndReadObject();

    return arr;
}

// Write
#region BasicTypesWrite

    public void Write(string name, string value) {
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append(value);
        NextLine();
    }

    public void Write(string name, int value) {
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append(value.ToString());
        NextLine();
    }

    public void Write(string name, uint value) {
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append(value.ToString());
        NextLine();
    }

    public void Write(string name, long value) {
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append(value.ToString());
        NextLine();
    }

    public void Write(string name, ulong value) {
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append(value.ToString());
        NextLine();
    }

    public void Write(string name, short value) {
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append(value.ToString());
        NextLine();
    }

    public void Write(string name, ushort value) {
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append(value.ToString());
        NextLine();
    }

    public void Write(string name, byte value) {
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append(value.ToString());
        NextLine();
    }

    public void Write(string name, sbyte value) {
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append(value.ToString());
        NextLine();
    }

    public void WriteBool(string name, bool value) {
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append(value.ToString());
        NextLine();
    }

    public void Write(string name, float value) {
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append(value.ToString());
        NextLine();
    }

    public void Write(string name, double value) {
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append(value.ToString());
        NextLine();
    }

    public void Write(string name, Vector3 value) {
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append($"({value.x}, {value.y}, {value.z})");
        NextLine();
    }

    public void Write(string name, Vector3Int value) {
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append($"({value.x}, {value.y}, {value.z})");
        NextLine();
    }

    public void Write(string name, Vector2 value) {
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append($"({value.x}, {value.y})");
        NextLine();
    }

    public void Write(string name, Vector2Int value) {
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append($"({value.x}, {value.y})");
        NextLine();
    }

    public void Write(string name, Vector4 value) {
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append($"({value.x}, {value.y}, {value.z}, {value.w})");
        NextLine();
    }

    public void Write(string name, Quaternion value) {
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append($"({value.x}, {value.y}, {value.z}, {value.w})");
        NextLine();
    }

    public void Write(string name, Matrix4x4 value) {
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append($"{value.GetRow(0)}, {value.GetRow(1)}, {value.GetRow(2)}, {value.GetRow(3)}");
        NextLine();
    }

#endregion
#region BasicArraysWrite
    public void Write<T>(string name, int itemsCount, T[] array) 
    where T : ISave {
        BeginObject(name);
        Write("Count", itemsCount);

        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
            NextLine();
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, int[] array) {
        BeginObject(name);
        Write("Count", itemsCount);

        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, uint[] array) {
        BeginObject(name);
        Write("Count", itemsCount);

        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, long[] array) {
        BeginObject(name);
        Write("Count", itemsCount);

        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, ulong[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, short[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, ushort[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, byte[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, sbyte[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, bool[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            WriteBool($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, float[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, double[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, Vector3[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, Vector3Int[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, Vector2[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, Vector2Int[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, Vector4[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, Quaternion[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, Matrix4x4[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }
#endregion
    //@Untested Everything inside this region is generated by ChatGPT
#region NativeArraysWrite
    public void Write<T>(string name, int itemsCount, NativeArray<T> array) 
    where T : struct, ISave {
        BeginObject(name);
        Write("Count", itemsCount);

        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
            NextLine();
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<int> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<uint> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<long> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<ulong> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<short> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<ushort> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<byte> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<sbyte> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<bool> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            WriteBool($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<float> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<double> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<Vector3> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<Vector3Int> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<Vector2> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<Vector2Int> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<Vector4> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<Quaternion> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<Matrix4x4> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        // NextLine();
    }
#endregion
// Read
#region ReadBasicTypes
    public string ReadString(string name, string defaultValue = "") {
        foreach(var field in GetCurrentNode().Fields) {
            if(field.Name == name) {
                return field.Value;
            }
        }

        return defaultValue;
    }

    public int ReadInt(string name, int defaultValue = 0) {
        foreach(var field in GetCurrentNode().Fields) {
            if(field.Name == name) {
                if(int.TryParse(field.Value, out var value)) {
                    return value;
                }
            }
        }

        return defaultValue;
    }

    public uint ReadUInt(string name, uint defaultValue = 0) {
        foreach(var field in GetCurrentNode().Fields) {
            if(field.Name == name) {
                if(uint.TryParse(field.Value, out var value)) {
                    return value;
                }
            }
        }

        return defaultValue;
    }

    public long ReadLong(string name, long defaultValue = 0) {
        foreach(var field in GetCurrentNode().Fields) {
            if(field.Name == name) {
                if(long.TryParse(field.Value, out var value)) {
                    return value;
                }
            }
        }

        return defaultValue;
    }

    public ulong ReadULong(string name, ulong defaultValue = 0) {
        foreach(var field in GetCurrentNode().Fields) {
            if(field.Name == name) {
                if(ulong.TryParse(field.Value, out var value)) {
                    return value;
                }
            }
        }

        return defaultValue;
    }

    public short ReadShort(string name, short defaultValue = 0) {
        foreach(var field in GetCurrentNode().Fields) {
            if(field.Name == name) {
                if(short.TryParse(field.Value, out var value)) {
                    return value;
                }
            }
        }

        return defaultValue;
    }

    public ushort ReadUShort(string name, ushort defaultValue = 0) {
        foreach(var field in GetCurrentNode().Fields) {
            if(field.Name == name) {
                if(ushort.TryParse(field.Value, out var value)) {
                    return value;
                }
            }
        }

        return defaultValue;
    }

    public byte ReadByte(string name, byte defaultValue = 0) {
        foreach(var field in GetCurrentNode().Fields) {
            if(field.Name == name) {
                if(byte.TryParse(field.Value, out var value)) {
                    return value;
                }
            }
        }

        return defaultValue;
    }

    public sbyte ReadSByte(string name, sbyte defaultValue = 0) {
        foreach(var field in GetCurrentNode().Fields) {
            if(field.Name == name) {
                if(sbyte.TryParse(field.Value, out var value)) {
                    return value;
                }
            }
        }

        return defaultValue;
    }

    public bool ReadBool(string name, bool defaultValue = false) {
        foreach(var field in GetCurrentNode().Fields) {
            if(field.Name == name) {
                if(bool.TryParse(field.Value, out var value)) {
                    return value;
                }
            }
        }

        return defaultValue;
    }

    public float ReadFloat(string name, float defaultValue = 0f) {
        foreach(var field in GetCurrentNode().Fields) {
            if(field.Name == name) {
                if(float.TryParse(field.Value, out var value)) {
                    return value;
                }
            }
        }

        return defaultValue;
    }

    public double ReadDouble(string name, double defaultValue = 0d) {
        foreach(var field in GetCurrentNode().Fields) {
            if(field.Name == name) {
                if(double.TryParse(field.Value, out var value)) {
                    return value;
                }
            }
        }

        return defaultValue;
    }

    public Vector3 ReadVector3(string name, Vector3 defaultValue = new()) {
        foreach(var field in GetCurrentNode().Fields) {
            if(field.Name == name) {
                var ret = new Vector3();
                var value = field.Value;
                var nums = value.TrimStart('(').TrimEnd(')').Split(',');
                for(var i = 0; i < 3; ++i) {
                    if(float.TryParse(nums[i], out var val)) {
                        ret[i] = val;
                    }
                }

                return ret;
            }
        }

        return defaultValue;
    }

    public Vector3Int ReadVector3Int(string name, Vector3Int defaultValue = new()) {
        foreach(var field in GetCurrentNode().Fields) {
            if(field.Name == name) {
                var ret = new Vector3Int();
                var value = field.Value;
                var nums = value.TrimStart('(').TrimEnd(')').Split(',');
                for(var i = 0; i < 3; ++i) {
                    if(int.TryParse(nums[i], out var val)) {
                        ret[i] = val;
                    }
                }

                return ret;
            }
        }

        return defaultValue;
    }

    public Vector2 ReadVector2(string name, Vector2 defaultValue = new()) {
        foreach(var field in GetCurrentNode().Fields) {
            if(field.Name == name) {
                var ret = new Vector2();
                var value = field.Value;
                var nums = value.TrimStart('(').TrimEnd(')').Split(',');
                for(var i = 0; i < 2; ++i) {
                    if(float.TryParse(nums[i], out var val)) {
                        ret[i] = val;
                    }
                }

                return ret;
            }
        }

        return defaultValue;
    }

    public Vector2Int ReadVector2Int(string name, Vector2Int defaultValue = new()) {
        foreach(var field in GetCurrentNode().Fields) {
            if(field.Name == name) {
                var ret = new Vector2Int();
                var value = field.Value;
                var nums = value.TrimStart('(').TrimEnd(')').Split(',');
                for(var i = 0; i < 2; ++i) {
                    if(int.TryParse(nums[i], out var val)) {
                        ret[i] = val;
                    }
                }

                return ret;
            }
        }

        return defaultValue;
    }

    public Vector4 ReadVector4(string name, Vector4 defaultValue = new()) {
        foreach(var field in GetCurrentNode().Fields) {
            if(field.Name == name) {
                var ret = new Vector4();
                var value = field.Value;
                var nums = value.TrimStart('(').TrimEnd(')').Split(',');
                for(var i = 0; i < 4; ++i) {
                    if(float.TryParse(nums[i], out var val)) {
                        ret[i] = val;
                    }
                }

                return ret;
            }
        }

        return defaultValue;
    }

    public Quaternion ReadQuaternion(string name, Quaternion defaultValue = new()) {
        foreach(var field in GetCurrentNode().Fields) {
            if(field.Name == name) {
                var ret = new Quaternion();
                var value = field.Value;
                var nums = value.TrimStart('(').TrimEnd(')').Split(',');
                for(var i = 0; i < 4; ++i) {
                    if(float.TryParse(nums[i], out var val)) {
                        ret[i] = val;
                    }
                }

                return ret;
            }
        }

        return defaultValue;
    }

    public Matrix4x4 ReadMatrix4x4(string name, Matrix4x4 defaultValue = new()) {
        foreach(var field in GetCurrentNode().Fields) {
            if(field.Name == name) {
                var ret = new Matrix4x4();
                var value   = field.Value;
                var currentRow = 0;
                var currentVector = new Vector4();
                var currentComp   = 0;
                var currString = "";

                for(var i = 0; i < value.Length; ++i) {
                    if(value[i] == '(') {
                        currentVector = new Vector4();
                        currentComp = 0;
                        currString = "";
                    } else if (value[i] == ')' && i == value.Length - 1) {
                        if(float.TryParse(currString, out var val)) {
                            currentVector[currentComp] = val;
                        }
                        ret.SetRow(currentRow, currentVector);
                    } else if(value[i] == ')' && value[i + 1] == ',') {
                        if(float.TryParse(currString, out var val)) {
                            currentVector[currentComp] = val;
                        } 

                        ret.SetRow(currentRow++, currentVector);
                    } else if (value[i] == ',' && value[i - 1] != ')') {
                        if(float.TryParse(currString, out var val)) {
                            currentVector[currentComp] = val;
                        } 
                        
                        currentComp++;
                        currString = "";
                    } else if(value[i] != '(' && value[i] != ')' && value[i] != ' ' && value[i] != ',') {
                        currString += value[i];
                    }
                }

                return ret;
            }
        }

        return defaultValue;
    }

#endregion
#region ReadBasicArrays
    public int[] ReadIntArray(string name) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr   = new int[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadInt($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public uint[] ReadUIntArray(string name) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr   = new uint[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadUInt($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public short[] ReadShortArray(string name) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr   = new short[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadShort($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public ushort[] ReadUShortArray(string name) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr   = new ushort[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadUShort($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public long[] ReadLongArray(string name) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr   = new long[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadLong($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public ulong[] ReadULongArray(string name) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr   = new ulong[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadULong($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public byte[] ReadByteArray(string name) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr   = new byte[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadByte($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public sbyte[] ReadSByteArray(string name) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr   = new sbyte[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadSByte($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public bool[] ReadBoolArray(string name) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr   = new bool[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadBool($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public float[] ReadFloatArray(string name) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr   = new float[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadFloat($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public double[] ReadDoubleArray(string name) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr   = new double[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadDouble($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public Vector3[] ReadVector3Array(string name) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr   = new Vector3[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadVector3($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public Vector3Int[] ReadVector3IntArray(string name) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr   = new Vector3Int[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadVector3Int($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public Vector2[] ReadVector2Array(string name) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr   = new Vector2[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadVector2($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }    

    public Vector2Int[] ReadVector2IntArray(string name) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr   = new Vector2Int[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadVector2Int($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public Vector4[] ReadVector4Array(string name) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr   = new Vector4[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadVector4($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public Quaternion[] ReadQuaternionArray(string name) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr   = new Quaternion[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadQuaternion($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public Matrix4x4[] ReadMatrix4x4Array(string name) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr   = new Matrix4x4[count];

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadMatrix4x4($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }
#endregion
// @Untested Everything inside ReadBasicNativeArrays region is generated by ChatGPT
#region ReadBasicNativeArrays
    public NativeArray<int> ReadNativeIntArray(string name, Allocator allocator) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr   = new NativeArray<int>(count, allocator);

        for(var i = 0; i < count; ++i) {
            arr[i] = ReadInt($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public NativeArray<uint> ReadNativeUIntArray(string name, Allocator allocator) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr = new NativeArray<uint>(count, allocator);

        for (var i = 0; i < count; ++i) {
            arr[i] = ReadUInt($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public NativeArray<short> ReadNativeShortArray(string name, Allocator allocator) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr = new NativeArray<short>(count, allocator);

        for (var i = 0; i < count; ++i) {
            arr[i] = ReadShort($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public NativeArray<ushort> ReadNativeUShortArray(string name, Allocator allocator) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr = new NativeArray<ushort>(count, allocator);

        for (var i = 0; i < count; ++i) {
            arr[i] = ReadUShort($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public NativeArray<long> ReadNativeLongArray(string name, Allocator allocator) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr = new NativeArray<long>(count, allocator);

        for (var i = 0; i < count; ++i) {
            arr[i] = ReadLong($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public NativeArray<ulong> ReadNativeULongArray(string name, Allocator allocator) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr = new NativeArray<ulong>(count, allocator);

        for (var i = 0; i < count; ++i) {
            arr[i] = ReadULong($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public NativeArray<byte> ReadNativeByteArray(string name, Allocator allocator) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr = new NativeArray<byte>(count, allocator);

        for (var i = 0; i < count; ++i) {
            arr[i] = ReadByte($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public NativeArray<sbyte> ReadNativeSByteArray(string name, Allocator allocator) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr = new NativeArray<sbyte>(count, allocator);

        for (var i = 0; i < count; ++i) {
            arr[i] = ReadSByte($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public NativeArray<bool> ReadNativeBoolArray(string name, Allocator allocator) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr = new NativeArray<bool>(count, allocator);

        for (var i = 0; i < count; ++i) {
            arr[i] = ReadBool($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public NativeArray<float> ReadNativeFloatArray(string name, Allocator allocator) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr = new NativeArray<float>(count, allocator);

        for (var i = 0; i < count; ++i) {
            arr[i] = ReadFloat($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public NativeArray<double> ReadNativeDoubleArray(string name, Allocator allocator) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr = new NativeArray<double>(count, allocator);

        for (var i = 0; i < count; ++i) {
            arr[i] = ReadDouble($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public NativeArray<Vector3> ReadNativeVector3Array(string name, Allocator allocator) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr = new NativeArray<Vector3>(count, allocator);

        for (var i = 0; i < count; ++i) {
            arr[i] = ReadVector3($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public NativeArray<Vector3Int> ReadNativeVector3IntArray(string name, Allocator allocator) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr = new NativeArray<Vector3Int>(count, allocator);

        for (var i = 0; i < count; ++i) {
            arr[i] = ReadVector3Int($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public NativeArray<Vector2> ReadNativeVector2Array(string name, Allocator allocator) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr = new NativeArray<Vector2>(count, allocator);

        for (var i = 0; i < count; ++i) {
            arr[i] = ReadVector2($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public NativeArray<Vector2Int> ReadNativeVector2IntArray(string name, Allocator allocator) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr = new NativeArray<Vector2Int>(count, allocator);

        for (var i = 0; i < count; ++i) {
            arr[i] = ReadVector2Int($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public NativeArray<Vector4> ReadNativeVector4Array(string name, Allocator allocator) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr = new NativeArray<Vector4>(count, allocator);

        for (var i = 0; i < count; ++i) {
            arr[i] = ReadVector4($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public NativeArray<Quaternion> ReadNativeQuaternionArray(string name, Allocator allocator) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr = new NativeArray<Quaternion>(count, allocator);

        for (var i = 0; i < count; ++i) {
            arr[i] = ReadQuaternion($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }

    public NativeArray<Matrix4x4> ReadNativeMatrix4x4Array(string name, Allocator allocator) {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr = new NativeArray<Matrix4x4>(count, allocator);

        for (var i = 0; i < count; ++i) {
            arr[i] = ReadMatrix4x4($"{name}ArrayElement{i}");
        }

        EndReadObject();

        return arr;
    }
#endregion

    private ObjectNode GetCurrentNode() {
        return ObjectStack.Peek();
    }

    private void BeginReadObject(string name) {
        if(ObjectStack.Count == 0) {
            ObjectStack.Push(Root);
            var currentNode = GetCurrentNode();
            
            if(currentNode.NestedObjects.TryGetValue(name, out var obj)) {
                ObjectStack.Push(obj);
            }
        } else {
            var currentNode = GetCurrentNode();
            
            if(currentNode.NestedObjects.TryGetValue(name, out var obj)) {
                ObjectStack.Push(obj);
            }
        }
    }

    

    

    private void EndReadObject() {
        if(ObjectStack.Count == 0) {
            ObjectStack.Push(Root);
        } else {
            ObjectStack.Pop();

            if(ObjectStack.Count == 0) {
                ObjectStack.Push(Root);
            }
        }
    }

    private void BeginObject(string name) {
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append('{');
        CurrentOffset += Offset;
        NextLine();
    }

    private void NextLine() {
        Sb.AppendLine();
        Sb.Append(' ', CurrentOffset);
    }

    private void EndObject() {
        Sb.AppendLine();
        CurrentOffset -= Offset;
        Sb.Append(' ', CurrentOffset);
        Sb.Append('}');
        NextLine();
    }

    private void AddNameSeparator() {
        Sb.Append(Separator);
    }

    private ObjectNode ParseObject(ref int startLine, ObjectNode node = default) {
        while(true) {
            var line = LoadedLines[startLine].TrimStart().TrimEnd();

            if(string.IsNullOrEmpty(line)) {
                startLine++;
                continue;
            }

            if(line[0] == '}') {
                startLine++;
                break;
            }

            var separateLine = line.Split(Separator);

            if(separateLine[1] == "{") {
                startLine++;
                node.PushObject(separateLine[0], ParseObject(ref startLine, ObjectNode.Create()));
            } else {
                node.PushField(new Field(separateLine[0], separateLine[1]));
                startLine++;
            }
        }

        return node;
    }

    private void SaveObject(string name, ObjectNode node) {
        Root.PushObject(name, node);
    }

}