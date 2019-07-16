//C# Example
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static GameViewUtils;

public enum ScreenshotBundleType
{
    None,
    GooglePlay,
    ITunes
}

public class ScreenshotBundle
{
    private static readonly ScreenshotBundle[] Bundles = new ScreenshotBundle[]
    {
        new ScreenshotBundle
        {
            bundleType = ScreenshotBundleType.GooglePlay,
            definitions = new ScreenshotDefinition[]
            {
                new ScreenshotDefinition
                {
                    width = 1080,
                    height = 1920,
                    suffix = "phone",
                    category = "phone"
                },
                new ScreenshotDefinition
                {
                    width = 2000,
                    height = 3000,
                    suffix = "tablet",
                    category = "tablet"
                },
                new ScreenshotDefinition
                {
                    width = 3840,
                    height = 2160,
                    suffix = "tv",
                    category = "tv"
                }
            }
        },
        new ScreenshotBundle
        {
            bundleType = ScreenshotBundleType.ITunes,
            definitions = new ScreenshotDefinition[]
            {
                new ScreenshotDefinition
                {
                    width = 1242,
                    height = 2688,
                    suffix = "6-5",
                    category = "phone"
                },
                new ScreenshotDefinition
                {
                    width = 1242,
                    height = 2208,
                    suffix = "5-5",
                    category = "phone"
                },
                new ScreenshotDefinition
                {
                    width = 2048,
                    height = 2732,
                    suffix = "tablet",
                    category = "tablet"
                }
            }
        }
    };

    public ScreenshotBundleType bundleType;
    public ScreenshotDefinition[] definitions;

    public string GetDirectory(ScreenshotDefinition definition)
    {
        if (definition.category.IsValid())
        {
            return $"{this.bundleType.ToString()}/{definition.category}/";
        }
        else
        {
            return $"{this.bundleType.ToString()}/";
        }
    }

    public static ScreenshotBundle ByType(ScreenshotBundleType type)
    {
        return Bundles.FirstOrDefault(x => x.bundleType == type);
    }
}

public class ScreenshotDefinition
{
    public int width;
    public int height;
    public string suffix;
    public string category;

    public string Name => (this.suffix.IsValid() ? $"{this.suffix}_" : "") + $"{this.width}x{this.height}";
}

[ExecuteInEditMode]
public class Screenshot : EditorWindow
{
    private static TimeSpan ShutterDelay = TimeSpan.FromMilliseconds(30);

    int resWidth = Screen.width * 4;
    int resHeight = Screen.height * 4;

    public ScreenshotBundleType bundleType;
    public Camera myCamera;
    int scale = 1;

    string path = "";
    // bool showPreview = true;
    RenderTexture renderTexture;

    bool isTransparent = false;

    // Add menu item named "My Window" to the Window menu
    [MenuItem("Tools/Saad Khawaja/Instant High-Res Screenshot")]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        EditorWindow editorWindow = EditorWindow.GetWindow(typeof(Screenshot));
        editorWindow.autoRepaintOnSceneChange = true;
        editorWindow.Show();
        editorWindow.titleContent = new GUIContent("Screenshot");
    }

    float lastTime;


    async void OnGUI()
    {
        EditorGUILayout.LabelField("Bundle", EditorStyles.boldLabel);
        bundleType = (ScreenshotBundleType)EditorGUILayout.EnumPopup("", bundleType);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Resolution", EditorStyles.boldLabel);
        resWidth = EditorGUILayout.IntField("Width", resWidth);
        resHeight = EditorGUILayout.IntField("Height", resHeight);

        EditorGUILayout.Space();

        scale = EditorGUILayout.IntSlider("Scale", scale, 1, 15);

        EditorGUILayout.HelpBox("The default mode of screenshot is crop - so choose a proper width and height. The scale is a factor " +
            "to multiply or enlarge the renders without loosing quality.", MessageType.None);


        EditorGUILayout.Space();


        GUILayout.Label("Save Path", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.TextField(path, GUILayout.ExpandWidth(false));
        if (GUILayout.Button("Browse", GUILayout.ExpandWidth(false)))
            path = EditorUtility.SaveFolderPanel("Path to Save Images", path, Application.dataPath);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("Choose the folder in which to save the screenshots ", MessageType.None);
        EditorGUILayout.Space();



        //isTransparent = EditorGUILayout.Toggle(isTransparent,"Transparent Background");



        GUILayout.Label("Select Camera", EditorStyles.boldLabel);


        myCamera = EditorGUILayout.ObjectField(myCamera, typeof(Camera), true, null) as Camera;


        if (myCamera == null)
        {
            myCamera = Camera.main;
        }

        isTransparent = EditorGUILayout.Toggle("Transparent Background", isTransparent);


        EditorGUILayout.HelpBox("Choose the camera of which to capture the render. You can make the background transparent using the transparency option.", MessageType.None);

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Default Options", EditorStyles.boldLabel);


        if (GUILayout.Button("Set To Screen Size"))
        {
            resHeight = (int)Handles.GetMainGameViewSize().y;
            resWidth = (int)Handles.GetMainGameViewSize().x;

        }


        if (GUILayout.Button("Default Size"))
        {
            resHeight = 1440;
            resWidth = 2560;
            scale = 1;
        }



        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Screenshot will be taken at " + resWidth * scale + " x " + resHeight * scale + " px", EditorStyles.boldLabel);

        if (GUILayout.Button("Take Screenshot", GUILayout.MinHeight(60)))
        {
            if (path == "")
            {
                path = EditorUtility.SaveFolderPanel("Path to Save Images", path, Application.dataPath);
                Debug.Log("Path Set");
                TakeHiResShot();
            }
            else
            {
                TakeHiResShot();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Open Last Screenshot", GUILayout.MaxWidth(160), GUILayout.MinHeight(40)))
        {
            if (lastScreenshot != "")
            {
                Application.OpenURL("file://" + lastScreenshot);
                Debug.Log("Opening File " + lastScreenshot);
            }
        }

        if (GUILayout.Button("Open Folder", GUILayout.MaxWidth(100), GUILayout.MinHeight(40)))
        {

            Application.OpenURL("file://" + path);
        }

        if (GUILayout.Button("More Assets", GUILayout.MaxWidth(100), GUILayout.MinHeight(40)))
        {
            Application.OpenURL("https://www.assetstore.unity3d.com/en/#!/publisher/5951");
        }

        EditorGUILayout.EndHorizontal();


        if (takeHiResShot)
        {
            takeHiResShot = false;
            // takeHiResShot = false;
            await this.PerformShot();
        }

        // EditorGUILayout.HelpBox("In case of any error, make sure you have Unity Pro as the plugin requires Unity Pro to work.", MessageType.Info);
    }

    public static void SetGameViewSize(int index)
    {
        var gvWndType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        var selectedSizeIndexProp = gvWndType.GetProperty("selectedSizeIndex",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var gvWnd = EditorWindow.GetWindow(gvWndType);
        selectedSizeIndexProp.SetValue(gvWnd, index, null);
    }

    private async Task PerformShot()
    {
        if (this.bundleType == ScreenshotBundleType.None)
        {
            int width = resWidth * scale;
            int height = resHeight * scale;
            this.Capture(width, height, ScreenShotName(width, height));
        }
        else
        {
            var screenshotBundle = ScreenshotBundle.ByType(this.bundleType);
            foreach (var screenshotDefinition in screenshotBundle.definitions)
            {
                int width = screenshotDefinition.width * scale;
                int height = screenshotDefinition.height * scale;

                if (GameViewUtils.FindSize(GameViewUtils.GetCurrentGroupType(), width, height) == -1)
                {
                    GameViewUtils.AddCustomSize(GameViewSizeType.FixedResolution, GameViewUtils.GetCurrentGroupType(), width, height, screenshotDefinition.suffix);
                }
                GameViewUtils.SetSize(GameViewUtils.FindSize(GameViewUtils.GetCurrentGroupType(), width, height));

                await Task.Delay(ShutterDelay);

                var directory = screenshotBundle.GetDirectory(screenshotDefinition);
                this.PrepareDirectory(path + "/", directory);
                var fullPath = $"{path}/{directory}";

                var fileName = string.Format("{0}{1}_{2}.png",
                    fullPath,
                    screenshotDefinition.Name,
                    System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
                this.Capture(width, height, fileName);
            }
        }
    }

    private void PrepareDirectory(string currentDirectory, string relativePath)
    {
        var targetDirectories = relativePath.IsInvalid() ? new string[0] : relativePath.Split('/');
        // var currentDirectory = $"{DataPath}";
        for (var i = 0; i < targetDirectories.Length - 1; ++i) // exclude last element (file path)
        {
            var targetDirectory = targetDirectories[i];
            if (targetDirectory.IsInvalid())
            {
                throw new ArgumentException("Couldn't prepare directory for save: a portion of the path is empty.");
            }
            currentDirectory += $"{targetDirectory}/";
            if (!Directory.Exists(currentDirectory))
            {
                var info = Directory.CreateDirectory(currentDirectory);
                if (!info.Exists)
                {
                    throw new ArgumentException($"Couldn't prepare directory for save: directory {targetDirectory} failed.");
                }
            }
        }
    }

    private void Capture(int width, int height, string fileName)
    {
        RenderTexture rt = new RenderTexture(width, height, 24);
        myCamera.targetTexture = rt;

        TextureFormat tFormat;
        if (isTransparent)
            tFormat = TextureFormat.ARGB32;
        else
            tFormat = TextureFormat.RGB24;


        Texture2D screenShot = new Texture2D(width, height, tFormat, false);
        myCamera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        myCamera.targetTexture = null;
        RenderTexture.active = null;
        byte[] bytes = screenShot.EncodeToPNG();

        System.IO.File.WriteAllBytes(fileName, bytes);
        Debug.Log(string.Format("Took screenshot to: {0}", fileName));
        // Application.OpenURL(fileName);
    }


    private bool takeHiResShot = false;
    public string lastScreenshot = "";


    public string ScreenShotName(int width, int height)
    {

        string strPath = "";

        strPath = string.Format("{0}/screen_{1}x{2}_{3}.png",
                             path,
                             width, height,
                                       System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
        lastScreenshot = strPath;

        return strPath;
    }



    public void TakeHiResShot()
    {
        Debug.Log("Taking Screenshot");
        takeHiResShot = true;
    }

}

// https://answers.unity.com/questions/956123/add-and-select-game-view-resolution.html
public static class GameViewUtils
{
    static object gameViewSizesInstance;
    static MethodInfo getGroup;

    static GameViewUtils()
    {
        // gameViewSizesInstance  = ScriptableSingleton<GameViewSizes>.instance;
        var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var instanceProp = singleType.GetProperty("instance");
        getGroup = sizesType.GetMethod("GetGroup");
        gameViewSizesInstance = instanceProp.GetValue(null, null);
    }

    public enum GameViewSizeType
    {
        AspectRatio, FixedResolution
    }

    // [MenuItem("Test/AddSize")]
    // public static void AddTestSize()
    // {
    //     AddCustomSize(GameViewSizeType.AspectRatio, GameViewSizeGroupType.Standalone, 123, 456, "Test size");
    // }

    // [MenuItem("Test/SizeTextQuery")]
    // public static void SizeTextQueryTest()
    // {
    //     Debug.Log(SizeExists(GameViewSizeGroupType.Standalone, "Test size"));
    // }

    // [MenuItem("Test/Query16:9Test")]
    // public static void WidescreenQueryTest()
    // {
    //     Debug.Log(SizeExists(GameViewSizeGroupType.Standalone, "16:9"));
    // }

    // [MenuItem("Test/Set16:9")]
    // public static void SetWidescreenTest()
    // {
    //     SetSize(FindSize(GameViewSizeGroupType.Standalone, "16:9"));
    // }

    // [MenuItem("Test/SetTestSize")]
    // public static void SetTestSize()
    // {
    //     int idx = FindSize(GameViewSizeGroupType.Standalone, 123, 456);
    //     if (idx != -1)
    //         SetSize(idx);
    // }

    public static void SetSize(int index)
    {
        var gvWndType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        var selectedSizeIndexProp = gvWndType.GetProperty("selectedSizeIndex",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var gvWnd = EditorWindow.GetWindow(gvWndType);
        selectedSizeIndexProp.SetValue(gvWnd, index, null);
    }

    // [MenuItem("Test/SizeDimensionsQuery")]
    // public static void SizeDimensionsQueryTest()
    // {
    //     Debug.Log(SizeExists(GameViewSizeGroupType.Standalone, 123, 456));
    // }

    public static void AddCustomSize(GameViewSizeType viewSizeType, GameViewSizeGroupType sizeGroupType, int width, int height, string text)
    {
        // GameViewSizes group = gameViewSizesInstance.GetGroup(sizeGroupTyge);
        // group.AddCustomSize(new GameViewSize(viewSizeType, width, height, text);

        var group = GetGroup(sizeGroupType);
        var addCustomSize = getGroup.ReturnType.GetMethod("AddCustomSize"); // or group.GetType().
        var gvsType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
        var constructors = gvsType.GetConstructors();
        var ctor = constructors[0];
        // var ctor = gvsType.GetConstructor(new Type[] { typeof(GameViewSizeType), typeof(Int32), typeof(Int32), typeof(String) });
        var newSize = ctor.Invoke(new object[] { (int)viewSizeType, width, height, text });
        addCustomSize.Invoke(group, new object[] { newSize });
    }

    public static bool SizeExists(GameViewSizeGroupType sizeGroupType, string text)
    {
        return FindSize(sizeGroupType, text) != -1;
    }

    public static int FindSize(GameViewSizeGroupType sizeGroupType, string text)
    {
        // GameViewSizes group = gameViewSizesInstance.GetGroup(sizeGroupType);
        // string[] texts = group.GetDisplayTexts();
        // for loop...

        var group = GetGroup(sizeGroupType);
        var getDisplayTexts = group.GetType().GetMethod("GetDisplayTexts");
        var displayTexts = getDisplayTexts.Invoke(group, null) as string[];
        for (int i = 0; i < displayTexts.Length; i++)
        {
            string display = displayTexts[i];
            // the text we get is "Name (W:H)" if the size has a name, or just "W:H" e.g. 16:9
            // so if we're querying a custom size text we substring to only get the name
            // You could see the outputs by just logging
            // Debug.Log(display);
            int pren = display.IndexOf('(');
            if (pren != -1)
                display = display.Substring(0, pren - 1); // -1 to remove the space that's before the prens. This is very implementation-depdenent
            if (display == text)
                return i;
        }
        return -1;
    }

    public static bool SizeExists(GameViewSizeGroupType sizeGroupType, int width, int height)
    {
        return FindSize(sizeGroupType, width, height) != -1;
    }

    public static int FindSize(GameViewSizeGroupType sizeGroupType, int width, int height)
    {
        // goal:
        // GameViewSizes group = gameViewSizesInstance.GetGroup(sizeGroupType);
        // int sizesCount = group.GetBuiltinCount() + group.GetCustomCount();
        // iterate through the sizes via group.GetGameViewSize(int index)

        var group = GetGroup(sizeGroupType);
        var groupType = group.GetType();
        var getBuiltinCount = groupType.GetMethod("GetBuiltinCount");
        var getCustomCount = groupType.GetMethod("GetCustomCount");
        int sizesCount = (int)getBuiltinCount.Invoke(group, null) + (int)getCustomCount.Invoke(group, null);
        var getGameViewSize = groupType.GetMethod("GetGameViewSize");
        var gvsType = getGameViewSize.ReturnType;
        var widthProp = gvsType.GetProperty("width");
        var heightProp = gvsType.GetProperty("height");
        var indexValue = new object[1];
        for (int i = 0; i < sizesCount; i++)
        {
            indexValue[0] = i;
            var size = getGameViewSize.Invoke(group, indexValue);
            int sizeWidth = (int)widthProp.GetValue(size, null);
            int sizeHeight = (int)heightProp.GetValue(size, null);
            if (sizeWidth == width && sizeHeight == height)
                return i;
        }
        return -1;
    }

    static object GetGroup(GameViewSizeGroupType type)
    {
        return getGroup.Invoke(gameViewSizesInstance, new object[] { (int)type });
    }

    // [MenuItem("Test/LogCurrentGroupType")]
    // public static void LogCurrentGroupType()
    // {
    //     Debug.Log(GetCurrentGroupType());
    // }
    public static GameViewSizeGroupType GetCurrentGroupType()
    {
        var getCurrentGroupTypeProp = gameViewSizesInstance.GetType().GetProperty("currentGroupType");
        return (GameViewSizeGroupType)(int)getCurrentGroupTypeProp.GetValue(gameViewSizesInstance, null);
    }
}

