using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

[System.Serializable]
public struct ObjectToSave : ISave {
    public int Int;
    public float Float;
    public double Double;
    public Vector3 Vector;

    public static ObjectToSave RandomObject() {
        return new ObjectToSave {
            Int = Random.Range(-100, 1000),
            Float = Random.Range(-100f, 100f),
            Double = Random.Range(-100f, 100f),
            Vector = Random.insideUnitSphere * 100f
        };
    }

    public void Save(ISaveFile sf) {
        sf.Write(Int, nameof(Int));
        sf.Write(Float, nameof(Float));
        sf.Write(Double, nameof(Double));
        sf.Write(Vector, nameof(Vector));
    }

    public void Load(ISaveFile sf) {
        Int = sf.Read<int>(nameof(Int));
        Float = sf.Read<float>(nameof(Float));
        Double = sf.Read<double>(nameof(Double));
        Vector = sf.Read<Vector3>(nameof(Vector));
    }
}

public class SavingPerformanceTest : MonoBehaviour {
    public ISaveFile Sf;

    public ObjectToSave[] ObjectsToSave = new ObjectToSave[10000];
    public Stopwatch Sw = new();

    public void Start() {
        Sf = new BinarySaveFile();
        Sf.NewFile(1);

        for(var i = 0; i < ObjectsToSave.Length; ++i) {
            ObjectsToSave[i] = ObjectToSave.RandomObject();
        }

        Sw.Start();

        Sf.WriteObjectArray(ObjectsToSave.Length, ObjectsToSave, nameof(ObjectsToSave));

        Sw.Stop();

        // All tests are made with 10000 objects: Text file / Binary
        Debug.Log($"Writing time: {Sw.ElapsedMilliseconds}"); // 249ms / 80ms

        Sw.Restart();

        Sf.SaveToFile(Application.persistentDataPath, "PerformanceSave");

        Sw.Stop();

        Debug.Log($"Writing to file time: {Sw.ElapsedMilliseconds}"); // 18ms / 1ms

        Sw.Restart();

        Sf.NewFromExistingFile($"{Application.persistentDataPath}/PerformanceSave.sav");

        Sw.Stop();

        Debug.Log($"Reading from file time: {Sw.ElapsedMilliseconds}"); // 208ms / 0ms

        Sw.Restart();

        ObjectsToSave = Sf.ReadValueObjectArray<ObjectToSave>(nameof(ObjectsToSave));

        Sw.Stop();

        Debug.Log($"Recreation time: {Sw.ElapsedMilliseconds}"); // 310ms / 60ms
    }
}