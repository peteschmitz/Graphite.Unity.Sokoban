using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Scripting;

public class FileManager
{
#if UNITY_EDITOR
    public static string DataPath => $"{Application.dataPath}/Resources/";
#else
    public static string DataPath = $"{Application.persistentDataPath}/";
#endif
    public const char PostfixSeparator = '_';

    private Dictionary<string, UnityEngine.Object> loadedResource = new Dictionary<string, UnityEngine.Object>();
    private Dictionary<string, List<UnityEngine.Object>> loadedResources = new Dictionary<string, List<UnityEngine.Object>>();
    private Dictionary<string, string[]> loadedDirectories = new Dictionary<string, string[]>();
    private Dictionary<string, object> jsonCache = new Dictionary<string, object>();

    #region properties
    private string Timestamp => DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fff", CultureInfo.InvariantCulture);
    #endregion

    public T GetResource<T>(string resourcePath) where T : UnityEngine.Object
    {
        if (!this.loadedResource.ContainsKey(resourcePath))
        {
            this.loadedResource[resourcePath] = Resources.Load<T>(resourcePath);
        }
        return (T)this.loadedResource[resourcePath];
    }

    public List<T> GetResources<T>(string resourcePath) where T : UnityEngine.Object
    {
        if (!this.loadedResources.ContainsKey(resourcePath))
        {
            this.loadedResources[resourcePath] = Resources.LoadAll<T>(resourcePath).Cast<UnityEngine.Object>().ToList();
        }
        return this.loadedResources[resourcePath]
            .Cast<T>()
            .ToList();
    }

    public GameObject GetPrefab<T>() => this.GetPrefab(typeof(T));
    public GameObject GetPrefab(Type type)
    {
        var prefabPath = type.GetCustomAttribute<PrefabAttribute>(true)?.Path;
        if (prefabPath.IsInvalid())
        {
            return null;
        }
        if (!this.loadedResource.ContainsKey(prefabPath))
        {
            this.loadedResource[prefabPath] = Resources.Load(prefabPath);
        }
        return (GameObject)this.loadedResource[prefabPath];
    }


    public string Save<T>(T data, bool timestampPostfix = false, string postFix = "", bool allowCache = true)
    {
        //var path = typeof(T).GetCustomAttribute<RelativePathAttribute>();
        //var relativePath = $"{path.RelativeFileName}/{path.FileName}";
        return this.SaveJson(data, typeof(T).GetCustomAttribute<RelativePathAttribute>().RelativeFileName, timestampPostfix, postFix, allowCache);
    }
    public string SaveJson(object dataObject, string relativePath, bool timestampPostfix = false, string postFix = "", bool allowCache = true)
    {
        if (relativePath.IsInvalid())
        {
            Debug.LogError("Couldn't save, empty path");
            return null;
        }
        if (relativePath.Contains(PostfixSeparator))
        {
            Debug.LogError($"Couldn't save {relativePath}, character {PostfixSeparator} is reserved.");
            return null;
        }

        try
        {
            this.PrepareDirectory(relativePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"Couldn't save {relativePath}, exception while preparing directory {e.GetType().Name}: {e.Message}.");
            return null;
        }

        var content = string.Empty;
        if (dataObject.GetType().IsAssignableFrom(typeof(string)))
        {
            content = (string)dataObject;
        }
        else
        {
            try
            {
                content = this.AsJson(dataObject);
            }
            catch (Exception e)
            {
                Debug.LogError($"Couldn't save {relativePath}, json exception {e.GetType().Name}: {e.Message}.");
                return null;
            }
        }
        if (content.IsInvalid())
        {
            Debug.LogError($"Couldn't save {relativePath}, serialized content is empty.");
            return null;
        }

        // http://answers.unity.com/answers/739357/view.html
        var absolutePath = "";
        string successFileName = null;
        try
        {
            var completePostfix = timestampPostfix ? $"{PostfixSeparator}{this.Timestamp}" : "";
            if (postFix.IsValid())
            {
                completePostfix += $"{PostfixSeparator}{postFix}";
            }
            absolutePath = $"{DataPath}{relativePath}{completePostfix}.json";
            using (var stream = new FileStream(absolutePath, FileMode.Create))
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(content);
                }
            }
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            // var fileName = relativePath;
            // if (fileName.Contains('/'))
            // {
            //     var subIndex = fileName.LastIndexOf('/') + 1;
            //     fileName = fileName.Substring(subIndex, fileName.Length - subIndex);
            // }
            successFileName = $"{relativePath}{completePostfix}.json";
        }
        catch (Exception e)
        {
            Debug.LogError($"Couldn't save {relativePath}, IO exception {e.GetType().Name}: {e.Message}.");
            return null;
        }

        return successFileName;
    }

    private string PrepareDirectory(string relativePath)
    {
        var targetDirectories = relativePath.IsInvalid() ? new string[0] : relativePath.Split('/');
        var currentDirectory = $"{DataPath}";
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
        return currentDirectory;
    }


    /// <summary>
    /// List of relative file paths to FileManager.DataPath
    /// </summary>
    /// <param name="relativeDirectoryPath"></param>
    /// <returns></returns>
    public string[] GetDirectoryFilePaths<T>(string fileExtension = "json", string appendedDirectory = "", bool allowCache = false) where T : new()
    {
        var pathAttribute = typeof(T).GetCustomAttribute<RelativePathAttribute>();
        return this.GetDirectoryFilePaths(pathAttribute.Directory, fileExtension, pathAttribute.IsResource, appendedDirectory, allowCache);
    }

    public string[] GetDirectoryFilePaths(string relativeDirectoryPath, string fileExtension = "json", bool isResource = false, string appendedDirectory = "", bool allowCache = false)
    {
        if (appendedDirectory.IsValid())
        {
            relativeDirectoryPath = $"{relativeDirectoryPath}{appendedDirectory}";
        }

        var fileNames = new string[0];
        if (relativeDirectoryPath.IsInvalid())
        {
            return fileNames;
        }

        if (isResource)
        {
            List<TextAsset> files = null;
            if (!this.loadedResources.ContainsKey(relativeDirectoryPath))
            {
                this.loadedResources[relativeDirectoryPath] = Resources.LoadAll(relativeDirectoryPath, typeof(TextAsset)).ToList();
            }
            files = this.loadedResources[relativeDirectoryPath]?
                .Cast<TextAsset>()
                .ToList();
            fileNames = files.AsNotNull().Select(x => x.name).ToArray();
            if (!fileNames.HasItems())
            {
                this.loadedResources.Remove(relativeDirectoryPath);
                Debug.LogError($"FileManager: Couldn't GetDirectoryFilePaths for relative path '{relativeDirectoryPath}'");
            }
            else
            {
                // json cache all items...
                foreach (var file in files)
                {
                    if (!String.IsNullOrEmpty(file?.name) && !this.jsonCache.ContainsKey(file.name))
                    {
                        this.jsonCache[file.name] = file;
                    }
                }
            }
        }
        else
        {
            if (allowCache && loadedDirectories.ContainsKey(relativeDirectoryPath))
            {
                fileNames = loadedDirectories[relativeDirectoryPath].ToArray();
            }
            else
            {
                this.PrepareDirectory(relativeDirectoryPath);
                fileNames = Directory.GetFiles($"{DataPath}{relativeDirectoryPath}");
            }
        }

        var preparedFiles = fileNames
            .Where(x => x.IsValid() && (!isResource && fileExtension.IsValid() ? x.HasFileExtension(fileExtension) : true))
            .Select(x => x.Replace($"{DataPath}", "").RemoveFileExtension())
            .ToArray();

        if (!isResource && !this.loadedDirectories.ContainsKey(relativeDirectoryPath) && preparedFiles.HasItems())
        {
            this.loadedDirectories.Add(relativeDirectoryPath, preparedFiles);
        }

        return preparedFiles;
    }

    public async Task<T> GetStreamedFile<T>(string relativePath) where T : new()
    {
        Debug.Log($"FileManager.GetStreamFile-> Attempt load on {relativePath}");
        // var www =  UnityWebRequest.Get(absolutePath);
        try
        {
            var absolutePath = Path.Combine(Application.streamingAssetsPath, relativePath);
            // #if UNITY_ANDROID
            var request = new SokobanRequestHandler<T>(UnityWebRequest.Get(absolutePath));
            var response = await request.send();
            // #endif
            Debug.Log($"FileManager.GetStreamFile-> Result on {relativePath}: {response.status.ToString()}");
            return response.result;
        }
        catch (Exception e)
        {
            Debug.Log($"FileManager.GetStreamFile-> Error: {e.Message}");
        }
        return default(T);
    }

    public T Get<T>(bool allowCache = true) where T : new() => this.Get<T>(typeof(T).GetCustomAttribute<RelativePathAttribute>().RelativeFileName, allowCache);
    public T Get<T>(string relativePath, bool allowCache = true) where T : new()
    {
        if (!relativePath.HasFileExtension())
        {
            relativePath += ".json";
        }
        if (allowCache && this.jsonCache.ContainsKey(relativePath))
        {
            return (T)this.jsonCache[relativePath];
        }
        else
        {
            return this.LoadJson<T>(relativePath);
        }
    }

    public List<T> GetDirectory<T>() where T : new() => this.GetDirectory<T>(typeof(T).GetCustomAttribute<RelativePathAttribute>().Directory);
    public List<T> GetDirectory<T>(string relativeDirectoryPath, string fileExtension = "json") where T : new()
    {
        return this.GetDirectoryFilePaths(relativeDirectoryPath, fileExtension)
            .Select(x => this.LoadJson<T>(x))
            .ToList();
    }

    public T LoadJson<T>() where T : new()
    {
        var pathAttribute = typeof(T).GetCustomAttribute<RelativePathAttribute>();
        return this.LoadJson<T>(pathAttribute.RelativeFileName);
    }

    public T LoadJson<T>(string relativePath) where T : new()
    {
        var pathAttribute = typeof(T).GetCustomAttribute<RelativePathAttribute>();
        var isResource = pathAttribute?.IsResource == true;
        TextAsset resourceAsset = null;
        try
        {
            if (relativePath.IsInvalid())
            {
                throw new ArgumentException($"Relative path is empty (Attempted to load type {typeof(T).Name}).");
            }

            var content = string.Empty;
            if (!isResource)
            {
                if (!relativePath.HasFileExtension())
                {
                    relativePath += ".json";
                }
                var absolutePath = $"{DataPath}{relativePath}";
                var directoryPath = this.PrepareDirectory(relativePath);
                if (!Directory.Exists(directoryPath))
                {
                    throw new IOException($"Couldn't prepare relative directory {relativePath}.");
                }
                if (!File.Exists(absolutePath))
                {
                    return this.CacheNew<T>(relativePath);
                }

                using (var stream = new FileStream(absolutePath, FileMode.Open))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        content = reader.ReadToEnd();
                    }
                }
            }
            else
            {
                resourceAsset = this.jsonCache.ContainsKey(relativePath) ? this.jsonCache[relativePath] as TextAsset : null;
                if (resourceAsset == null)
                {
                    resourceAsset = Resources.Load<TextAsset>(relativePath);
                    if (resourceAsset == null)
                    {
                        Debug.Log($"FileManager: Couldn't LoadJson for relative path '{relativePath}'");
                    }
                }
                content = resourceAsset?.text;
            }
            // Debug.Log($"Loaded content (from path {DataPath}{relativePath}): " + content);
            var loadedObject = JsonConvert.DeserializeObject<T>(content);
            if (loadedObject != null && content.IsValid())
            {
                // prefer caching the resource asset
                if (resourceAsset != null)
                {
                    this.jsonCache[relativePath] = resourceAsset;
                }
                else
                {
                    this.jsonCache[relativePath] = loadedObject;
                }
                return loadedObject;
            }
            throw new JsonException($"Deserialized content was null/empty (On type {typeof(T).Name}).");
        }
        catch (Exception e)
        {
            Debug.LogError($"Couldn't load {relativePath}, IO exception {e.GetType().Name}: {e.Message}.");
            return this.CacheNew<T>(relativePath);
        }
    }

    private T CacheNew<T>(string relativePath) where T : new()
    {
        var newObject = new T();
        this.jsonCache[relativePath] = newObject;
        return newObject;
    }

    public string AsJson(object jsonObject)
    {
        return JsonConvert.SerializeObject(
            jsonObject,
            Formatting.Indented,
            new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
    }

    //public List<T> LoadData<T>(string relativePath)
    //{
    //    if (relativePath.IsInvalid())
    //    {
    //        return new List<T>();
    //    }
    //    if (relativePath.HasFileExtension())
    //    {

    //    }
    //    else
    //    {
    //        var data = this.GetFileData()
    //            .AsNotNull()
    //            .Select(x => this.GetDirectoryFilePaths(x))
    //            .ToList()

    //    }
    //}

}
