using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static ArrayUtils;

public class EntitiesDebugger : MonoBehaviour {
    private delegate bool ValidateValue<T>(string str, out T value);
    
    public EntityManager EntityManager;
    public Camera        DebugCamera;
    public Camera        PreviousCamera;
    public float         CameraDefaultSpeed = 5f;
    public float         CameraFastSpeed    = 15f;
    public float         CameraSensitivity  = 120f; // angle per second
    public float         RollSensitivity    = 50f;
    public bool          Enabled = false;
    public bool          SortEntities = false;
    public KeyCode       KeyToEnable = KeyCode.BackQuote;
    public EntityType    SortByType;
    public float         EntitiesListWidth = 0.15f;
    public float         EntityHeight = 0.06f;
    public float         DescriptionWidth = 0.30f;

    public float FieldWidth  = 0.20f;
    public float FieldHeight = 0.045f;
    public float AdditionalFieldOffset = 0.1f;
    public float AdditionalFieldWidth  = 0.2f;
    public float AdditionalFieldHeight = 0.035f;
    public float SingleValueOffset = 0.23f;
    public float SingleValueWidth  = 0.06f;

    public int    ItemsPerScroll = 1;
    public uint   FirstEntity;
    public uint   FirstField;
    public uint[] SortedEntities = new uint[1024];
    public uint   SortedEntitiesCount;
    public uint   SelectedEntity;
    public uint   PreviousEntity;
    public string StringBuffer;

    public float  GoToOffset  = 0.23f;
    public float  GoToWidth   = 0.06f;
    public float  GoToUp      = 5f;
    public float  GoToForward = -5f;

    private void Awake() {
        DebugCamera.gameObject.SetActive(false);
    }

    private void Update() {
        if(Input.GetKeyDown(KeyToEnable)) {
            Enabled = !Enabled;
            if(Enabled) {
                PreviousCamera = Camera.main;
                Camera.main.gameObject.SetActive(false);
                DebugCamera.gameObject.SetActive(true);
            } else {
                PreviousCamera.gameObject.SetActive(true);
                DebugCamera.gameObject.SetActive(false);
            }
        }

        if(Enabled) {
            //Rotate and move camera
            var t = DebugCamera.transform;
            var z = Input.GetAxisRaw("Vertical");
            var x = Input.GetAxisRaw("Horizontal");
            var y = 0f;
            var forward = t.forward;
            var right   = t.right;
            var up  = t.up;
            var speedUpCamera = Input.GetKey(KeyCode.LeftShift);
            var cameraSpeed = speedUpCamera ? CameraFastSpeed : CameraDefaultSpeed;
            var mousex = Input.GetAxisRaw("Mouse X") * CameraSensitivity * Time.deltaTime;
            var mousey = -Input.GetAxisRaw("Mouse Y") * CameraSensitivity * Time.deltaTime;
            var mousez = 0f;
            var currentRotation = t.rotation;

            if(Input.GetKey(KeyCode.Q)) {
                mousez += 1f;
            }

            if(Input.GetKey(KeyCode.E)) {
                mousez -= 1f;
            }

            mousez *= RollSensitivity * Time.deltaTime;
            
            var inputRotation = Quaternion.Euler(mousey, mousex, mousez);
            currentRotation *= inputRotation;

            t.rotation = currentRotation;

            if(Input.GetKey(KeyCode.Space)) {
                y += 1f;
            }

            if(Input.GetKey(KeyCode.LeftControl)) {
                y -= 1f;
            }


            var direction = new Vector3(x, y, z).normalized * cameraSpeed * Time.deltaTime;
            forward *= direction.z;
            right   *= direction.x;
            up      *= direction.y;

            t.position += forward + right + up;
        }
    }

    private void OnGUI() {
        if(Enabled) {
            var screenHeight = Screen.height;
            var screenWidth  = Screen.width;
            var entityWidth  = screenWidth * EntitiesListWidth;
            var entityHeight = screenHeight * EntityHeight;
            var fieldWidth   = screenWidth * FieldWidth;
            var fieldHeight  = screenHeight * FieldHeight;
            var currentEntitiesListHeight = screenHeight * 0.01f;
            var descriptionWidth = screenWidth * DescriptionWidth;
            var descriptionStart = screenWidth - descriptionWidth;
            var additionalFieldOffset = descriptionWidth * AdditionalFieldOffset;
            var additionalFieldWidth  = screenWidth * AdditionalFieldWidth;
            var additionalFieldHeight = screenHeight * AdditionalFieldHeight;
            var mousePosition    = Input.mousePosition;

            ClearSortedEntities();

            GUI.Box(new Rect(0, 0, screenWidth, screenHeight), "Entities Debugger");

            var entitiesListRect = new Rect(0, 0, screenWidth * EntitiesListWidth, screenHeight);

            GUI.BeginGroup(entitiesListRect); //Entities List

            GUI.BeginGroup(new Rect(0, currentEntitiesListHeight, screenWidth * EntitiesListWidth, screenHeight)); // Sorted Entities

            for(uint i = 0; i < EntityManager.MaxEntitiesCount; ++i) {
                if(currentEntitiesListHeight > screenHeight)
                    break;

                if(EntityManager.Entities[i].Alive) {
                    if(!SortEntities) {
                        PushEntity(i);
                    } else {
                        if(EntityManager.Entities[i].Type == SortByType) {
                            PushEntity(i);
                        }
                    }
                }
            }

            if(entitiesListRect.Contains(mousePosition)) {
                var mouseDelta = (Math.Sign(Input.mouseScrollDelta.y) * ItemsPerScroll);
                var firstEntity = FirstEntity - mouseDelta;

                if(firstEntity < 0) {
                    FirstEntity = 0;
                } else if(firstEntity >= SortedEntitiesCount) {
                    FirstEntity = SortedEntitiesCount - 1;
                } else {
                    FirstEntity = (uint)firstEntity;
                }
            }

            for(var i = FirstEntity; i < SortedEntitiesCount; ++i) {
                if(GUI.Button(new Rect(0, currentEntitiesListHeight, entityWidth, entityHeight), 
                              $"{EntityManager.Entities[SortedEntities[i]].Type}")) {
                    PreviousEntity = SelectedEntity;
                    SelectedEntity = SortedEntities[i];
                }

                currentEntitiesListHeight += entityHeight;
            }

            GUI.EndGroup(); //Sorted Entities

            GUI.EndGroup(); //Entities List

            if(SelectedEntity != 0) {
                var descriptionRect =
                    new Rect(descriptionStart, 0, descriptionWidth, screenHeight);
                GUI.BeginGroup(descriptionRect);
                 // Entity Description

                GUI.Box(new Rect(0, 0, descriptionWidth, screenHeight), $"{EntityManager.Entities[SelectedEntity].Type}:{SelectedEntity}");

                var entity = EntityManager.Entities[SelectedEntity].Entity;
                var type = entity.GetType();
                var members = type.GetFields(BindingFlags.Instance  |
                                            BindingFlags.Public    |
                                            BindingFlags.NonPublic)
                                  .Cast<MemberInfo>()
                                  .Concat(type.GetProperties(BindingFlags.Instance |
                                               BindingFlags.Public   |
                                               BindingFlags.NonPublic)).ToArray();
                uint membersCount = (uint)members.Length;
                
                var currentFieldHeight = screenHeight * 0.05f;
                
                if(descriptionRect.Contains(mousePosition)) {
                    var mouseDelta = Math.Sign(Input.mouseScrollDelta.y) * ItemsPerScroll;
                    var firstField = FirstField - mouseDelta;

                    if(firstField < 0) {
                        FirstField = 0;
                    } else if(firstField >= membersCount) {
                        FirstField = membersCount - 1;
                    } else {
                        FirstField = (uint)firstField;
                    }
                }

                for (var i = FirstField; i < membersCount; ++i) {
                    var member = members[i];
                    if (member.GetCustomAttributes<HideInInspector>().Any()) {
                        continue;
                    }
                    
                    if(IsUnityField(member))
                        continue;

                    DisplayMember(member, 
                                  entity, 
                                  ref currentFieldHeight, 
                                  fieldWidth, 
                                  fieldHeight,
                                  additionalFieldOffset,
                                  additionalFieldWidth,
                                  additionalFieldHeight);
                    currentFieldHeight += fieldHeight;

                    if (currentFieldHeight > screenHeight)
                        break;
                }

                GUI.EndGroup(); //Entity Description
            }
        }
    }

    private void PushEntity(uint entity) {
        if(SortedEntitiesCount == SortedEntities.Length) {
            Resize(ref SortedEntities, SortedEntitiesCount << 1);
        }

        SortedEntities[SortedEntitiesCount++] = entity;
    }

    private void ClearSortedEntities() {
        SortedEntitiesCount = 0;
    }

    private void DisplayMember(MemberInfo member, 
                               Entity entity, 
                               ref float currentHeight, 
                               float width, 
                               float height,
                               float additionalFieldOffset,
                               float additionalFieldWidth,
                               float additionalFieldHeight) {
        var gotoOffset = Screen.width * GoToOffset;
        var gotoWidth  = Screen.width * GoToWidth;
        var singleValueOffset = Screen.width * SingleValueOffset;
        var singleValueWidth  = Screen.width * SingleValueWidth;
        GUI.Label(new Rect(0, currentHeight, width, height), member.Name);

        object memberObject;
        
        if ((member.MemberType & MemberTypes.Field) == MemberTypes.Field) {
            memberObject = ((FieldInfo)member).GetValue(entity);
        }
        else if ((member.MemberType & MemberTypes.Property) == MemberTypes.Property) {
            memberObject = ((PropertyInfo)member).GetValue(entity);
        }
        else {
            memberObject = null;
        }

        var memberType = memberObject.GetType();
        var memberTypeName = memberType.ToString();

        if (memberType.IsSubclassOf(typeof(Entity))) {
            DrawGoTo((MonoBehaviour)memberObject,
                     gotoOffset,
                     currentHeight,
                     gotoWidth,
                     additionalFieldHeight);
        } else if (memberType.IsSubclassOf(typeof(MonoBehaviour))) {
            DrawGoTo((MonoBehaviour)memberObject,
                     gotoOffset,
                     currentHeight,
                     gotoWidth,
                     additionalFieldHeight);
        }

        switch (memberTypeName) {
            case "UnityEngine.Transform": {
                var t = (Transform)memberObject;
                var offset = additionalFieldOffset;
                var position = t.position;
                var rotation = t.rotation.eulerAngles;
                var scale    = t.localScale;

                //Draw position
                currentHeight += additionalFieldHeight;

                GUI.Label(new Rect(offset, currentHeight, width, height), "Position");
                currentHeight += additionalFieldHeight;
                offset += additionalFieldOffset;

                position = DrawVector3Input(position,
                                            ref StringBuffer,
                                            ref currentHeight,
                                            offset,
                                            singleValueOffset,
                                            additionalFieldHeight);
                
                offset -= additionalFieldOffset;

                //Draw rotation
                currentHeight += additionalFieldHeight;

                GUI.Label(new Rect(offset, currentHeight, width, height), "Rotation");
                currentHeight += additionalFieldHeight;
                offset += additionalFieldOffset;

                rotation = DrawVector3Input(rotation,
                                            ref StringBuffer,
                                            ref currentHeight,
                                            offset,
                                            singleValueOffset,
                                            additionalFieldHeight);

                offset -= additionalFieldOffset;

                //Draw scale
                currentHeight += additionalFieldHeight;

                GUI.Label(new Rect(offset, currentHeight, width, height), "LocalScale");
                currentHeight += additionalFieldHeight;
                offset += additionalFieldOffset;

                scale = DrawVector3Input(scale,
                                         ref StringBuffer,
                                         ref currentHeight,
                                         offset,
                                         singleValueOffset,
                                         additionalFieldHeight);

                t.position = position;
                t.rotation = Quaternion.Euler(rotation);
                t.localScale = scale;
            }
            break;    
            case "System.Single": {
                var value = DrawInputValue((float)memberObject,
                                                  ValidateSingle,
                                                  ref StringBuffer,
                                                  singleValueOffset,
                                                  currentHeight,
                                                  singleValueWidth,
                                                  additionalFieldHeight);

                SetMemberValue(member, entity, value);
            }
            break;
            case "System.Int32": {
                var value = DrawInputValue((int)memberObject,
                                                ValidateInt,
                                                ref StringBuffer,
                                                singleValueOffset,
                                                currentHeight,
                                                singleValueWidth,
                                                additionalFieldHeight);

                SetMemberValue(member, entity, value);
            }
            break;
            case "System.UInt32": {
                var value = DrawInputValue((uint)memberObject,
                                                 ValidateUInt,
                                                 ref StringBuffer,
                                                 singleValueOffset,
                                                 currentHeight,
                                                 singleValueWidth,
                                                 additionalFieldHeight);

                SetMemberValue(member, entity, value);
            }
            break;
            case "System.Int64": {
                var value = DrawInputValue((long)memberObject,
                                                 ValidateLong,
                                                 ref StringBuffer,
                                                 singleValueOffset,
                                                 currentHeight,
                                                 singleValueWidth,
                                                 additionalFieldHeight);

                SetMemberValue(member, entity, value);
            }
            break;
            case "System.UInt64": {
                var value = DrawInputValue((ulong)memberObject,
                                                  ValidateULong,
                                                  ref StringBuffer,
                                                  singleValueOffset,
                                                  currentHeight,
                                                  singleValueWidth,
                                                  additionalFieldHeight);

                SetMemberValue(member, entity, value);
            }
            break;
            case "System.Double": {
                var value = DrawInputValue((double)memberObject,
                                                   ValidateDouble,
                                                   ref StringBuffer,
                                                   singleValueOffset,
                                                   currentHeight,
                                                   singleValueWidth,
                                                   additionalFieldHeight);

                SetMemberValue(member, entity, value);
            }
            break;
            case "System.Boolean": {
                var current = (bool)memberObject;
                var value = GUI.Toggle(new Rect(singleValueOffset,
                                                currentHeight,
                                                singleValueWidth,
                                                additionalFieldHeight), current, "");
                SetMemberValue(member, entity, value);
            }
            break;
            case "System.String": {
                GUI.Label(new Rect(singleValueOffset,
                                   currentHeight,
                                   singleValueWidth,
                                   additionalFieldHeight), (string)memberObject);
            }
            break;
            default: {
                // Debug.Log(memberTypeName);
            }
            break;
        }
    }


    private bool ValidateInt(string str, out int value) {
        return int.TryParse(str, out value);
    }
    
    private bool ValidateUInt(string str, out uint value) {
        return uint.TryParse(str, out value);
    }

    private bool ValidateLong(string str, out long value) {
        return long.TryParse(str, out value);
    }
    
    private bool ValidateULong(string str, out ulong value) {
        return ulong.TryParse(str, out value);
    }

    private bool ValidateSingle(string str, out float value) {
        if(str.EndsWith('.') || str.EndsWith(',')) {
            str.Remove(str.Length - 1);
            str += '1';
        }
        return float.TryParse(str, out value);
    }

    private bool ValidateDouble(string str, out double value) {
        if(str.EndsWith('.') || str.EndsWith(',')) {
            str.Remove(str.Length - 1);
            str += '1';
        }
        return double.TryParse(str, out value);
    }

    private T DrawInputValue<T>(T input,
                                ValidateValue<T> validationFunc,
                                ref string inputOutput,
                                float x,
                                float y,
                                float width,
                                float height) {
        var result = input;
        inputOutput = result.ToString();
        inputOutput = GUI.TextField(new Rect(x,
                                            y,
                                            width,
                                            height), inputOutput);
        var valueValid = validationFunc(inputOutput, out var value);
        if (valueValid) {
            result = value;
        }

        return result;
    }

    private void SetMemberValue(MemberInfo member,
                                object obj,
                                object value) {
        if ((member.MemberType & MemberTypes.Field) == MemberTypes.Field) {
            ((FieldInfo)member).SetValue(obj, value);
        } else if ((member.MemberType & MemberTypes.Property) == MemberTypes.Property) {
            var setMethod = ((PropertyInfo)member).GetSetMethod();
            if (setMethod != null) {
                ((PropertyInfo)member).SetValue(obj, value);
            }
        }    
    }

    private Vector3 DrawVector3Input(Vector3 input,
                                     ref string inputOutput,
                                     ref float currentHeight,
                                     float x,
                                     float width,
                                     float height) {
        var result = input;
        inputOutput = result.x.ToString();
        inputOutput = GUI.TextField(new Rect(x,
                                             currentHeight,
                                             width,
                                             height), inputOutput);

        var valueValid = ValidateSingle(inputOutput, out var vx);
        if (valueValid) {
            result.x = vx;
        }

        currentHeight += height;

        inputOutput = result.y.ToString();
        inputOutput = GUI.TextField(new Rect(x,
                                             currentHeight,
                                             width,
                                             height), inputOutput);

        valueValid = ValidateSingle(inputOutput, out var vy);
        if (valueValid) {
            result.y = vy;
        }

        currentHeight += height;

        inputOutput = result.z.ToString();
        inputOutput = GUI.TextField(new Rect(x,
                                             currentHeight,
                                             width,
                                             height), inputOutput);

        valueValid = ValidateSingle(inputOutput, out var vz);
        if (valueValid) {
            result.z = vz;
        }

        return result;
    }

    private void DrawGoTo(MonoBehaviour e,
                          float x,
                          float y,
                          float width,
                          float height) {
        if(GUI.Button(new Rect(x,
                               y,
                               width,
                               height), "Go To")) {
            var t = e.transform;
            var p = t.position;
            p.y += GoToUp;
            p.z += GoToForward;

            DebugCamera.transform.position = p;
            DebugCamera.transform.LookAt(t.position);
        }

    }

    private static bool IsUnityField(MemberInfo field) {
        return field.Name switch {
            "destroyCancellationToken" => true,
            "useGUILayout" => true,
            "runInEditMode" => true,
            "allowPrefabModeInPlayMode" => true,
            "isActiveAndEnabled" => true,
            "gameObject" => true,
            "tag" => true,
            "rigidbody" => true,
            "rigidbody2D" => true,
            "camera" => true,
            "light" => true,
            "animation" => true,
            "constantForce" => true,
            "renderer" => true,
            "audio" => true,
            "networkView" => true,
            "collider" => true,
            "collider2D" => true,
            "hingeJoint" => true,
            "particleSystem" => true,
            "hideFlags" => true,
            _ => false
        };
    }
}