using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

[System.Serializable]
public struct SavingObject : ISave {
    public int Value;
    public float Value2;
    public NestedObject NestedObject;

    public void Save(SaveFile file) {
        file.Write(nameof(Value), Value);
        file.Write(nameof(Value2), Value2);
        file.Write(nameof(NestedObject), NestedObject);
    }

    public void Load(SaveFile file) {
        Value = file.ReadInt(nameof(Value));
        // Debug.Log(Value);
        Value2 = file.ReadFloat(nameof(Value2));
        // Debug.Log(Value2);
        NestedObject = file.ReadValueType<NestedObject>(nameof(NestedObject));
    }
}

[System.Serializable]
public struct NestedObject : ISave {
    public float Value1;
    public int Value2;
    public NestedDefaultObject Value3;

    public void Save(SaveFile sf) {
        sf.Write(nameof(Value1), Value1);
        sf.Write(nameof(Value2), Value2);
        sf.Write(nameof(Value3), Value3);
    }

    public void Load(SaveFile sf) {
        Value1 = sf.ReadFloat(nameof(Value1));
        // Debug.Log(Value1);
        Value2 = sf.ReadInt(nameof(Value2));
        // Debug.Log(Value2);
        Value3 = sf.ReadValueType<NestedDefaultObject>(nameof(Value3));
    }
}

[System.Serializable]
public struct NestedDefaultObject : ISave {
    public float Val1;
    public int Val2;

    public static readonly NestedDefaultObject Default = new NestedDefaultObject {
        Val1 = 20f,
        Val2 = 340
    };

    public void Save(SaveFile sf) {
        sf.Write(nameof(Val1), Val1);
        sf.Write(nameof(Val2), Val2);
    }

    public void Load(SaveFile sf) {
        Val1 = sf.ReadFloat(nameof(Val1), Default.Val1);
        // Debug.Log(Val1);
        Val2 = sf.ReadInt(nameof(Val2), Default.Val2);
        // Debug.Log(Val2);
    }
}

[System.Serializable]
public struct BaseTypes : ISave {
    public Vector3 Vector3;
    public Vector3Int Vector3Int;
    public Vector2 Vector2;
    public Vector2Int Vector2Int;
    public Vector4 Vector4;
    public Quaternion Quaternion;
    public Matrix4x4  Matrix;
    public double Double;
    public int    Int;
    public uint   UInt;
    public long   Long;
    public ulong  ULong;
    public short  Short;
    public ushort UShort;
    public byte   Byte;
    public sbyte  SByte;
    public bool   Bool;
    
    public void Save(SaveFile sf) {
        sf.Write(nameof(Vector3), Vector3);
        sf.Write(nameof(Vector3Int), Vector3Int);
        sf.Write(nameof(Vector2), Vector2);
        sf.Write(nameof(Vector2Int), Vector2Int);
        sf.Write(nameof(Vector4), Vector4);
        sf.Write(nameof(Quaternion), Quaternion);
        sf.Write(nameof(Matrix), Matrix);
        sf.Write(nameof(Double), Double);
        sf.Write(nameof(Int), Int);
        sf.Write(nameof(UInt), UInt);
        sf.Write(nameof(Long), Long);
        sf.Write(nameof(ULong), ULong);
        sf.Write(nameof(Short), Short);
        sf.Write(nameof(UShort), UShort);
        sf.Write(nameof(Byte), Byte);
        sf.Write(nameof(SByte), SByte);
        sf.Write(nameof(Bool), Bool);
    }

    public void Load(SaveFile sf) {
        Vector3 = sf.ReadVector3(nameof(Vector3));
        // Vector3Int = sf.ReadVector3Int(nameof(Vector3Int));
        // Vector2 = sf.ReadVector2(nameof(Vector2));
        // Vector2Int = sf.ReadVector2Int(nameof(Vector2Int));
        // Vector4 = sf.ReadVector4(nameof(Vector4));
        // Quaternion = sf.ReadQuaternion(nameof(Quaternion));
        // Matrix = sf.ReadMatrix4x4(nameof(Matrix));

        Double = sf.ReadDouble(nameof(Double));
        Int = sf.ReadInt(nameof(Int));
        UInt = sf.ReadUInt(nameof(UInt));
        Long = sf.ReadLong(nameof(Long));
        ULong = sf.ReadULong(nameof(ULong));
        Short = sf.ReadShort(nameof(Short));
        UShort = sf.ReadUShort(nameof(UShort));
        Byte = sf.ReadByte(nameof(Byte));
        SByte = sf.ReadSByte(nameof(SByte));
        Bool = sf.ReadBool(nameof(Bool));
    }
}

public class SavingTest : MonoBehaviour {
    public SaveFile Save;
    public SavingObject[] Objects = new SavingObject[10000];
    public float[] Floats = new float[10000];
    public int[] Ints = new int[10240];
    public BaseTypes BaseTypes;

    private void Start() {
        var sw = new Stopwatch();
        Save = new SaveFile();
        // sw.Start();
        // Save.NewFile(0.01f);
        // Save.Write(nameof(Objects), Objects.Length, Objects);
        // Save.Write(nameof(Floats), Floats.Length, Floats);
        // Save.Write(nameof(Ints), Ints.Length, Ints);
        // Save.Write(nameof(BaseTypes), BaseTypes);
        // sw.Stop();

        // Debug.Log($"Write Time: {sw.ElapsedMilliseconds}");

        // sw.Restart();
        // Save.SaveToFile(Application.persistentDataPath, "TestSave");
        // sw.Stop();

        // Debug.Log($"File Write Time: {sw.ElapsedMilliseconds}");


        sw.Restart();
        Save.NewFromExistingFile(Application.persistentDataPath, "TestSave");
        sw.Stop();

        Debug.Log($"File Parse Time: {sw.ElapsedMilliseconds}");

        sw.Restart();
        Objects   = Save.ReadObjectArray<SavingObject>(nameof(Objects), () => new SavingObject());
        Floats    = Save.ReadFloatArray(nameof(Floats));
        Ints      = Save.ReadIntArray(nameof(Ints));
        BaseTypes = Save.ReadValueType<BaseTypes>(nameof(BaseTypes));
        sw.Stop();

        Debug.Log($"Reconstruction Time: {sw.ElapsedMilliseconds}");
    }

    private ISave NewSavingObject() {
        return new SavingObject();
    }
}