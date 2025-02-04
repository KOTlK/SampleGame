using System;

public class SaveSystem : IDisposable {
    public enum SaveType {
        Text,
        Binary
    }

    public uint Version = 2;
    public event Action<ISaveFile> LoadingOver = delegate { };

    public SaveType  Type = SaveType.Binary;
    public ISaveFile Sf;

    public SaveSystem() {
        ChangeSaveFileType(SaveType.Binary);
    }

    public SaveSystem(SaveType type) {
        ChangeSaveFileType(type);
    }

    public void Dispose() {
        if(Sf != null) {
            Sf.Dispose();
        }
    }

    public void ChangeSaveFileType(SaveType type) {
        if(Sf != null) {
            Sf.Dispose();
        }

        switch (Type) {
            case SaveType.Text : {
                Sf = new TextSaveFile();
            }
            break;
            case SaveType.Binary : {
                Sf = new BinarySaveFile();
            }
            break;
        }
    }

    public ISaveFile BeginSave() {
        Sf.NewFile(Version);
        return Sf;
    }

    public void EndSave(string path, string name) {
        Sf.SaveToFile(path, name);
    }

    public ISaveFile BeginLoading(string path) {
        Sf.NewFromExistingFile(path);

        return Sf;
    }

    public void EndLoading() {
        LoadingOver(Sf);
    }
}