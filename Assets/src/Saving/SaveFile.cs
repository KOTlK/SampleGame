using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Unity.Collections;
using System;

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
    public char[]        StringBuffer = new char[256];
    public int           StringBufferPointer;
    public string[]      LoadedLines;
    public int           LinesCount;
    public StringBuilder Sb = new();
    public int           CurrentOffset;
    public Dictionary<string, ObjectNode>  SaveHierarchy = new ();
    public int           SavedObjectsCount;
    public int           CurrentObject;
    public Stack<ObjectNode> ObjectStack = new();

    public const int Offset = 4;
    public const string Extension = ".sav";
    public const string Separator = " : ";
    
    public void NewFile(float version) {
        Version = version;
        Write("Version", version);
        Sb.AppendLine();
    }
    
    public void SaveToFile(string path, string name) {
        path += $"/{name}{Extension}";
        Debug.Log(path);
        if(File.Exists(path)) {
            File.Delete(path);
            File.CreateText(path).Close();
            Debug.Log("Removing existing file");
        }
        
        File.WriteAllText(path, Sb.ToString());
        // File.WriteAllText(path, Sb.ToString());
    }

    public void NewFromExistingFile(string path, string name) {
        path += $"/{name}{Extension}";
        if(File.Exists(path)) {
            LoadedLines = File.ReadAllLines(path);
            LinesCount = LoadedLines.Length;
        }

        //Parse file
        var versionLine = LoadedLines[0].TrimStart().TrimEnd();
        var nameObj = versionLine.Split(Separator);
        if(float.TryParse(nameObj[1], out var version)) {
            Version = version;
        }

        Debug.Log(Version);

        var startLine = 1;

        while(startLine < LinesCount) {
            var line = LoadedLines[startLine].TrimStart().TrimEnd();
            var separateLine = line.Split(Separator);
            if(separateLine.Length > 1 && separateLine[1] == "{") {
                startLine++;
                SaveObject(separateLine[0], ParseObject(ref startLine, ObjectNode.Create()));
            } else {
                startLine++;
            }
        }

        Debug.Log(SavedObjectsCount);
    }

    public ObjectNode ParseObject(ref int startLine, ObjectNode node = default) {
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

    public void SaveObject(string name, ObjectNode node) {
        SaveHierarchy.Add(name, node);
        SavedObjectsCount++;
    }

    public void Write(string name, ISave save) {
        BeginObject(name);
        save.Save(this);
        EndObject();
    }

    public void BeginObject(string name) {
        Sb.Append(name);
        AddNameSeparator();
        Sb.Append('{');
        CurrentOffset += Offset;
        NextLine();
    }

    public void NextLine() {
        Sb.AppendLine();
        Sb.Append(' ', CurrentOffset);
    }

    public void EndObject() {
        Sb.AppendLine();
        CurrentOffset -= Offset;
        Sb.Append(' ', CurrentOffset);
        Sb.Append('}');
    }

    public void AddNameSeparator() {
        Sb.Append(Separator);
    }

#region BasicTypesWrite

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

    public void Write(string name, bool value) {
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
        Sb.Append($"{value.GetColumn(0)}, {value.GetColumn(1)}, {value.GetColumn(2)}, {value.GetColumn(3)}");
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
        NextLine();
    }

    public void Write(string name, int itemsCount, int[] array) {
        BeginObject(name);
        Write("Count", itemsCount);

        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, uint[] array) {
        BeginObject(name);
        Write("Count", itemsCount);

        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, long[] array) {
        BeginObject(name);
        Write("Count", itemsCount);

        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, ulong[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, short[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, ushort[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, byte[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, sbyte[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, bool[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, float[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, double[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, Vector3[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, Vector3Int[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, Vector2[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, Vector2Int[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, Vector4[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, Quaternion[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, Matrix4x4[] array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
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
        NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<int> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<uint> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<long> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<ulong> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<short> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<ushort> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<byte> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<sbyte> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<bool> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<float> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<double> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<Vector3> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<Vector3Int> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<Vector2> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<Vector2Int> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<Vector4> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<Quaternion> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }

    public void Write(string name, int itemsCount, NativeArray<Matrix4x4> array) {
        BeginObject(name);
        Write("Count", itemsCount);
        
        for(var i = 0; i < itemsCount; ++i) {
            Write($"{name}ArrayElement{i}", array[i]);
        }
        EndObject();
        NextLine();
    }
#endregion
    
    public ObjectNode GetCurrentNode() {
        return ObjectStack.Peek();
    }

    public void BeginReadObject(string name) {
        if(ObjectStack.Count == 0) {
            ObjectStack.Push(SaveHierarchy[name]);
        } else {
            var currentNode = GetCurrentNode();
            
            if(currentNode.NestedObjects.TryGetValue(name, out var obj)) {
                ObjectStack.Push(obj);
            }
        }
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

    public void EndReadObject() {
        if(ObjectStack.Count == 0) {
            CurrentObject++;
        } else {
            ObjectStack.Pop();

            if(ObjectStack.Count == 0) {
                CurrentObject++;
            }
        }
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
                Debug.Log(nums.Length);
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

    public T[] ReadObjectArray<T>(string name, Func<ISave> createObjectFunc) 
    where T : ISave {
        BeginReadObject(name);
        var count = ReadInt("Count");
        var arr   = new T[count];

        for(var i = 0; i < count; ++i) {
            var obj = createObjectFunc();
            ReadObject($"{name}ArrayElement{i}", obj);
        }

        EndReadObject();

        return arr;
    }

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
}