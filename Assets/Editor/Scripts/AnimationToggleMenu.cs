using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using nadena.dev.modular_avatar.core;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEditor.Animations;
using VRC.SDKBase;

public class MakeAnimationToggleWindow : EditorWindow
{
    private GameObject selectedObject;
    private VRCExpressionsMenu _expressionsMenu;
    private VRCExpressionsMenu _closet;
    private AnimatorController animatorController;

    [MenuItem("Window/EasyMakeCloset")]
    public static void ShowWindow()
    {
        GetWindow<MakeAnimationToggleWindow>("EasyMakeCloset");
    }

    private void OnGUI()
    {
        GUILayout.Label("Select an object to Make Closet", EditorStyles.boldLabel);

        // 오브젝트를 선택할 수 있는 필드
        selectedObject = (GameObject)EditorGUILayout.ObjectField("Target(Clothes)", selectedObject, typeof(GameObject), true);

        if (GUILayout.Button("Make Closet"))
        {
            GameObject avatarObject = FindAvatarDescriptor();
            
            if (selectedObject && avatarObject)
            {
                GameObject clothes = CheckAlreadyExists(avatarObject);
                if (!clothes)
                {
                    GameObject instance = Instantiate(selectedObject, avatarObject.transform);
                    instance.name = selectedObject.name; // 인스턴스 이름을 원본 프리팹 이름과 동일하게 설정

                    // MenuCommand를 생성하여 SetupOutfit 메서드에 전달

                    Selection.activeObject = instance;
                    MenuCommand menuCommand = new MenuCommand(instance);

                    // 리플렉션을 사용하여 내부 메서드 호출
                    InvokeInternalSetupOutfit(menuCommand);
                    Selection.activeObject = null;

                    CreateAnimation(instance);
                    AddComponents(instance);
                }
                else
                {
                    DestroyImmediate(clothes);
                }
            }
            else if (!selectedObject)
            {
                EditorUtility.DisplayDialog("Error", "Please select a GameObject", "OK");
            }
            else if (!avatarObject)
            {
                EditorUtility.DisplayDialog("Error", "Please make avatar instance in level", "OK");
            }
        }
    }

    private GameObject CheckAlreadyExists(GameObject avatarObject)
    {
        Transform foundTransform = avatarObject.transform.Find(selectedObject.name);
        return foundTransform ? foundTransform.gameObject : null;
    }
    
    private void InvokeInternalSetupOutfit(MenuCommand menuCommand)
    {
        // 어셈블리 로드
        Assembly assembly = Assembly.LoadFrom("Library\\ScriptAssemblies\\nadena.dev.modular-avatar.core.editor.dll");
        // EasySetupOutfit 타입 가져오기
        var type = assembly.GetType("nadena.dev.modular_avatar.core.editor.EasySetupOutfit");
        
        // SetupOutfit 메서드 가져오기
        MethodInfo methodInfo = type.GetMethod("SetupOutfit", BindingFlags.Static | BindingFlags.NonPublic);
        if (methodInfo == null)
        {
            Debug.LogError("메서드를 찾을 수 없습니다: SetupOutfit");
            return;
        }

        // 메서드 호출
        methodInfo.Invoke(null, new object[] { menuCommand });
        Console.ReadKey(); 
    }
    
    private GameObject FindAvatarDescriptor()
    {
        VRC_AvatarDescriptor[] avatars = FindObjectsOfType<VRC_AvatarDescriptor>();
        if (avatars.Length > 0)
        {
            return avatars[0].gameObject;
        }
        return null;
    }

    private void CreateAnimation(GameObject targetObject)
    {
        AnimationClip toggleOnClip = new AnimationClip();
        AnimationClip toggleOffClip = new AnimationClip();
        
        SkinnedMeshRenderer[] renderers = targetObject.GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            string animPath = renderer.transform.name;
            
            AddAnimationCurve(toggleOnClip, animPath, true);
            AddAnimationCurve(toggleOffClip, animPath, false);
        }
        
        string folderPath = "Assets/EasyCloset";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "EasyCloset");
        }

        folderPath += $"/{targetObject.name}";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/EasyCloset", targetObject.name);
        }
        AssetDatabase.CreateAsset(toggleOnClip, $"{folderPath}/{targetObject.name}_ToggleOn.anim");
        AssetDatabase.CreateAsset(toggleOffClip, $"{folderPath}/{targetObject.name}_ToggleOff.anim");
        
        animatorController = AnimatorController.CreateAnimatorControllerAtPath($"{folderPath}/{targetObject.name}_ToggleAnimatorController.controller");
        
        animatorController.AddParameter($"{targetObject.name}Toggle", AnimatorControllerParameterType.Bool);
        
        AnimatorState toggleOnState = animatorController.AddMotion(toggleOnClip);
        AnimatorState toggleOffState = animatorController.AddMotion(toggleOffClip);
        
        animatorController.layers[0].stateMachine.defaultState = toggleOnState;
        
        AnimatorStateMachine stateMachine = animatorController.layers[0].stateMachine;
        AnimatorStateTransition toOffTransition = toggleOnState.AddTransition(toggleOffState);
        toOffTransition.AddCondition(AnimatorConditionMode.IfNot, 0, $"{targetObject.name}Toggle");
        toOffTransition.hasExitTime = false;
        
        AnimatorStateTransition toOnTransition = toggleOffState.AddTransition(toggleOnState);
        toOnTransition.AddCondition(AnimatorConditionMode.If, 0, $"{targetObject.name}Toggle");
        toOnTransition.hasExitTime = false;
        
        Animator animator = targetObject.GetComponent<Animator>();
        if (!animator)
        {
            animator = targetObject.AddComponent<Animator>();
        }
        animator.runtimeAnimatorController = animatorController;
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void AddAnimationCurve(AnimationClip clip, string path, bool isActive)
    {
        // AnimationCurve를 사용하여 키프레임을 설정
        AnimationCurve curve = AnimationCurve.Constant(0, 0, isActive ? 1f : 0f);
        clip.SetCurve(path, typeof(GameObject), "m_IsActive", curve);
    }

    private void AddComponents(GameObject targetObject)
    {
        // 1. Menu 생성
        CreateVRCExpressionsMenu(targetObject);

        // 2. MA Menu Installer 컴포넌트 추가
        ModularAvatarMenuInstaller installer = targetObject.GetComponent<ModularAvatarMenuInstaller>();
        if (!installer)
        {
            installer = targetObject.AddComponent<ModularAvatarMenuInstaller>();
        }
        else
        {
            //Menu Installer가 있으면 이미 해당 오브젝트는 진행했다 가정
            return;
        }
        
        // rootMenu를 찾아서 installTargetMenu로 설정
        VRCAvatarDescriptor avatarDescriptor = targetObject.GetComponentInParent<VRCAvatarDescriptor>();
        if (avatarDescriptor)
        {
            installer.installTargetMenu = avatarDescriptor.expressionsMenu;
        }
        
        VRCExpressionsMenu rootMenu = installer.installTargetMenu;
        if (!findClosetMenuControl(rootMenu))
        {
            var control = new VRCExpressionsMenu.Control
            {
                name = "Closet",
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = _closet
            };
            rootMenu.controls.Add(control);
        }
        
        installer.installTargetMenu = _closet;
        installer.menuToAppend = _expressionsMenu;
        installer.menuToAppend.controls[0].parameter.name = animatorController.parameters[0].name;
        
        // 3. MA Parameters 컴포넌트 추가
        ModularAvatarParameters parameters = targetObject.GetComponent<ModularAvatarParameters>();
        if (!parameters)
        {
            parameters = targetObject.AddComponent<ModularAvatarParameters>();
        }

        // 4. bool 타입의 파라미터 생성 -> animation cotroller에 등록된 파라미터
        parameters.parameters.Add(new ParameterConfig
        {
            internalParameter = false,
            nameOrPrefix = animatorController.parameters[0].name,
            isPrefix = false,
            remapTo = "",
            syncType = ParameterSyncType.Bool,
            saved = true,
        });
        

        // 5. MA Merge Animator 컴포넌트 추가
        ModularAvatarMergeAnimator mergeAnimator = targetObject.GetComponent<ModularAvatarMergeAnimator>();
        if (!mergeAnimator)
        {
            mergeAnimator = targetObject.AddComponent<ModularAvatarMergeAnimator>();
        }

        // 기본 설정 추가
        mergeAnimator.animator = targetObject.GetComponent<Animator>().runtimeAnimatorController;
        mergeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
        mergeAnimator.deleteAttachedAnimator = true;
        mergeAnimator.pathMode = MergeAnimatorPathMode.Relative;
        mergeAnimator.matchAvatarWriteDefaults = false;
        mergeAnimator.relativePathRoot = new AvatarObjectReference();
        mergeAnimator.layerPriority = 0;
    }

    bool findClosetMenuControl(VRCExpressionsMenu rootMenu)
    {
        if (!rootMenu) return false;
        foreach (var control in rootMenu.controls)
        {
            if (control.name == "Closet" && control.subMenu == _closet)
                return true;
            else if (control.name == "Closet")
            {
                rootMenu.controls.Remove(control);
            }
        }

        return false; 
    }

    private void CreateVRCExpressionsMenu(GameObject targetObject)
    {
        //Assets/MakeToggle 확인
        string folderPath = "Assets/EasyCloset";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "EasyCloset");
        }

        //Assets/MakeToggle/ClosetMenu.asset 확인
        string closetPath = folderPath + "/ClosetMenu.asset";
        _closet = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(closetPath);
        if (!_closet)
        {
            _closet = CreateInstance<VRCExpressionsMenu>();
            AssetDatabase.CreateAsset(_closet, closetPath);
            AssetDatabase.SaveAssets();
        }
        
        //Assets/MakeToggle/selectObject.name 폴더 확인
        folderPath += $"/{targetObject.name}";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("EasyCloset", targetObject.name);
        }
        
        //Asset/MakeToggle/selectObject.name/NewExpressionMenu.asset 확인
        string path = folderPath + "/NewExpressionsMenu.asset";
        _expressionsMenu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(path);
        if (_expressionsMenu)
        {
            return;
        }
        
        _expressionsMenu = CreateInstance<VRCExpressionsMenu>();
        var control = new VRCExpressionsMenu.Control
        {
            name = targetObject.name,
            type = VRCExpressionsMenu.Control.ControlType.Toggle,
            parameter = new VRCExpressionsMenu.Control.Parameter
            {
                name = targetObject.name + "Toggle"
            }
        };
        _expressionsMenu.controls.Add(control);
        
        AssetDatabase.CreateAsset(_expressionsMenu, path);
        AssetDatabase.SaveAssets();
    }
}
