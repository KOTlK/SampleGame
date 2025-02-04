using System.Diagnostics;
using Unity.Collections;
using UnityEngine;
using Debug = UnityEngine.Debug;

[System.Serializable]
public struct SavingObject : ISave {
    public int Value;
    public float Value2;
    public NestedObject NestedObject;

    public void Save(ISaveFile file) {
        file.Write(Value, nameof(Value));
        file.Write(Value2, nameof(Value2));
        file.WriteObject(NestedObject, nameof(NestedObject));
    }

    public void Load(ISaveFile file) {
        Value = file.Read<int>(nameof(Value));
        // Debug.Log(Value);
        Value2 = file.Read<float>(nameof(Value2));
        // Debug.Log(Value2);
        NestedObject = file.ReadValueType<NestedObject>(nameof(NestedObject));
    }
}

[System.Serializable]
public struct NestedObject : ISave {
    public float Value1;
    public int Value2;
    public NestedDefaultObject Value3;

    public void Save(ISaveFile sf) {
        sf.Write(Value1, nameof(Value1));
        sf.Write(Value2, nameof(Value2));
        sf.WriteObject(Value3, nameof(Value3));
    }

    public void Load(ISaveFile sf) {
        Value1 = sf.Read<float>(nameof(Value1));
        // Debug.Log(Value1);
        Value2 = sf.Read<int>(nameof(Value2));
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

    public void Save(ISaveFile sf) {
        sf.Write(Val1, nameof(Val1));
        sf.Write(Val2, nameof(Val2));
    }

    public void Load(ISaveFile sf) {
        Val1 = sf.Read(nameof(Val1), Default.Val1);
        // Debug.Log(Val1);
        Val2 = sf.Read(nameof(Val2), Default.Val2);
        // Debug.Log(Val2);
    }
}

public class SavingTest : MonoBehaviour {
    public EntityManager Em;
    public ISaveFile Save;
    public SavingObject[] Objects = new SavingObject[10000];
    public NativeArray<int> NativeInts = new NativeArray<int>(10, Allocator.Persistent);
    public NativeArray<SavingObject> NativeObjects = new NativeArray<SavingObject>(10, Allocator.Persistent);
    public float[] Floats = new float[10000];
    public int[] Ints = new int[10240];
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

    private unsafe void Start() {
        Save = new BinarySaveFile();
    }

    private void OnDestroy() {
        NativeInts.Dispose();
        NativeObjects.Dispose();
    }

    private void Update() {
        var sw = new Stopwatch();

        if (Input.GetKeyDown(KeyCode.F5)) {
            sw.Start();
            Save.NewFile(2);
            // Save.Write<int>(-2000000000);
            // Save.Write<uint>(4000000000);
            // Save.Write<short>(-32000);
            // Save.Write<ushort>(65000);
            // Save.Write<long>(-999999999999999999);
            // Save.Write<ulong>(9999999999999999999);
            // Save.Write(true);
            // Save.Write(false);
            // Save.Write(new Vector3Int(1, 2, 3));
            // Save.Write(new Vector2Int(3, 4));
            // Save.Write(new Vector2(3, 4));
            // Save.Write(new Vector3(5, 6, 32));
            // Save.Write(new Vector4(5, 6, 7, 8));
            // Save.Write("Hello World!");
            // Save.Write("Привет Мир!");
            // Save.Write(123);
            // // Save.WriteObject(Em);
            // Save.Write(Quaternion.Euler(45, 34, 22));
            // Save.Write(Quaternion.Euler(22.2f, 33.8f, 2.2f));
            // Save.Write(22.33f);
            // Save.Write(42.23f);
            // Save.Write(4222.234343d);
            // Save.Write(12332.422123d);
            // Save.Write(new Matrix4x4(new Vector4(1, 2, 3, 4), 
            //                          new Vector4(5, 6, 7, 8),
            //                          new Vector4(9, 10, 11, 12),
            //                          new Vector4(13, 14, 15, 16)));
            // Save.Write(33356);

            Save.WriteObjectArray(Objects.Length, Objects, nameof(Objects));
            Save.WriteArray(Floats.Length, Floats, nameof(Floats));
            Save.WriteArray(Ints.Length, Ints, nameof(Ints));
            Save.WriteObject(Em, nameof(Em));
            Save.Write(Vector3, nameof(Vector3));
            Save.Write(Vector3Int, nameof(Vector3Int));
            Save.Write(Vector2, nameof(Vector2));
            Save.Write(Vector2Int, nameof(Vector2Int));
            Save.Write(Vector4, nameof(Vector4));
            Save.Write(Quaternion, nameof(Quaternion));
            Save.Write(Matrix, nameof(Matrix));
            Save.Write(Double, nameof(Double));
            Save.Write(Int, nameof(Int));
            Save.Write(UInt, nameof(UInt));
            Save.Write(Long, nameof(Long));
            Save.Write(ULong, nameof(ULong));
            Save.Write(Short, nameof(Short));
            Save.Write(UShort, nameof(UShort));
            Save.Write(Byte, nameof(Byte));
            Save.Write(SByte, nameof(SByte));
            Save.Write(Bool, nameof(Bool));
            sw.Stop();

            Debug.Log($"Write Time: {sw.ElapsedMilliseconds}");

            sw.Restart();
            Save.SaveToFile(Application.persistentDataPath, "TestSave");
            sw.Stop();

            Debug.Log($"File Write Time: {sw.ElapsedMilliseconds}");
        }

        if(Input.GetKeyDown(KeyCode.F9)) {
            sw.Restart();
            Save.NewFromExistingFile($"{Application.persistentDataPath}/TestSave.sav");
            sw.Stop();

            Debug.Log($"File Parse Time: {sw.ElapsedMilliseconds}");

            // var sf  = (BinarySaveFile)Save;
            // var str = "";

            // for(var ii = 0; ii < sf.LoadedBytes.Length; ++ii) {
            //     str += string.Format("{0:x}, ", sf.LoadedBytes[ii]);
            // }

            // Debug.Log(str);

            sw.Restart();
            // var a = Save.Read<int>();
            // var b = Save.Read<uint>();
            // var c = Save.Read<short>();
            // var d = Save.Read<ushort>();
            // var e = Save.Read<long>();
            // var f = Save.Read<ulong>();
            // var g = Save.Read<bool>();
            // var h = Save.Read<bool>();
            // var i = Save.Read<Vector3Int>();
            // var j = Save.Read<Vector2Int>();
            // var k = Save.Read<Vector2>();
            // var l = Save.Read<Vector3>();
            // var m = Save.Read<Vector4>();
            // var n = Save.Read<string>();
            // var o = Save.Read<string>();
            // var p = Save.Read<int>();
            // // Save.ReadObject(Em);
            // var r = Save.Read<Quaternion>();
            // var s = Save.Read<Quaternion>();
            // var t = Save.Read<float>();
            // var u = Save.Read<float>();
            // var v = Save.Read<double>();
            // var w = Save.Read<double>();
            // var x = Save.Read<Matrix4x4>();
            // var y = Save.Read<int>();
            // Debug.Log(a);
            // Debug.Log(b);
            // Debug.Log(c);
            // Debug.Log(d);
            // Debug.Log(e);
            // Debug.Log(f);
            // Debug.Log(g);
            // Debug.Log(h);
            // Debug.Log(i);
            // Debug.Log(j);
            // Debug.Log(k);
            // Debug.Log(l);
            // Debug.Log(m);
            // Debug.Log(n);
            // Debug.Log(o);
            // Debug.Log(p);
            // Debug.Log(r.eulerAngles);
            // Debug.Log(s.eulerAngles);
            // Debug.Log(t);
            // Debug.Log(u);
            // Debug.Log(v);
            // Debug.Log(w);

            // for(var ii = 0; ii < 4; ++ii) {
            //     Debug.Log(x.GetColumn(ii));
            // }
            // Debug.Log(y);

            Objects    = Save.ReadValueObjectArray<SavingObject>(nameof(Objects));
            Floats     = Save.ReadArray<float>(nameof(Floats));
            Ints       = Save.ReadArray<int>(nameof(Ints));
            Save.ReadObject(Em, nameof(Em));
            Vector3    = Save.Read<Vector3>(nameof(Vector3));
            Vector3Int = Save.Read<Vector3Int>(nameof(Vector3Int));
            Vector2    = Save.Read<Vector2>(nameof(Vector2));
            Vector2Int = Save.Read<Vector2Int>(nameof(Vector2Int));
            Vector4    = Save.Read<Vector4>(nameof(Vector4));
            Quaternion = Save.Read<Quaternion>(nameof(Quaternion));
            Matrix     = Save.Read<Matrix4x4>(nameof(Matrix));

            Double = Save.Read<double>(nameof(Double));
            Int    = Save.Read<int>(nameof(Int));
            UInt   = Save.Read<uint>(nameof(UInt));
            Long   = Save.Read<long>(nameof(Long));
            ULong  = Save.Read<ulong>(nameof(ULong));
            Short  = Save.Read<short>(nameof(Short));
            UShort = Save.Read<ushort>(nameof(UShort));
            Byte   = Save.Read<byte>(nameof(Byte));
            SByte  = Save.Read<sbyte>(nameof(SByte));
            Bool   = Save.Read<bool>(nameof(Bool));
            
            sw.Stop();

            Debug.Log($"Reconstruction Time: {sw.ElapsedMilliseconds}");
        }
    }
}