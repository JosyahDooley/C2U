/*Created by Josyah Dooley*/
/*Created using Unity v2021.3.11f1 for Greyhound v1.46.3.1 & C2M v1.0.4.3*/

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using UnityEditor.SceneManagement;

public class Cod2Unity : EditorWindow
{
    //The status bar string
    private string status = "Ready!";

    //These are all the special keywords variables the make special assignment to object that contain these strings in there name
    private string[] transparencyKeywords = new string[] { "sight", "flare", "lense", "glass", "decal", "fx", "net", "ac130_p" };
    private string[] cutoutKeywords = new string[] { "shrub", "brush" };

    //Create a menu item with our name
    [MenuItem("Window/Cod2Unity")]
    public static void ShowEditor()
    {
        //Get the window if it's created or create a new one
        Cod2Unity c2u = GetWindow<Cod2Unity>();
        //Then set the window title
        c2u.titleContent = new GUIContent("Call Of Duty To Unity");
    }

    //Toolbar selection stuff
    public int toolbarSelection = 0;
    public string[] toolbarStrings = new string[] { "Props, Images and Materials", "Map Details", "Build Settings" };

    //Props variables
    public bool replaceTexture = false;
    public bool replaceModel = false;
    public bool replaceMaterial = false;
    public bool replacePrefabs = false;
    public bool modelImportForce = false;
    public bool showTransparentKeywords = false;
    public bool showCutoutKeywords = false;

    //Build variables
    public bool createNewScene = false;
    public bool replaceSceneFiles = false;
    public TextAsset xmodelsJson;

    //Method for drawing inside the editorwindow
    void OnGUI()
    {
        //Create the toolbar using the specified selection and string list
        toolbarSelection = GUILayout.Toolbar(toolbarSelection, toolbarStrings);

        //If the toolbar selection is Props, Images and Materials
        if (toolbarSelection == 0)
        {
            //Start with the text
            GUI.contentColor = Color.yellow;
            GUILayout.Label("Import from Greyhound Directory>exported_files>(GameName) folder!");
            GUI.contentColor = Color.white;

            //Then the toggles
            replaceTexture = EditorGUILayout.Toggle("Replace existing textures", replaceTexture);
            replaceModel = EditorGUILayout.Toggle("Replace existing models", replaceModel);
            replaceMaterial = EditorGUILayout.Toggle("Replace existing materials", replaceMaterial);
            replacePrefabs = EditorGUILayout.Toggle("Replace existing prefabs", replacePrefabs);
            modelImportForce = EditorGUILayout.Toggle("Force models dirty", modelImportForce);

            //Then the foldouts for the keywords
            showTransparentKeywords = EditorGUILayout.Foldout(showTransparentKeywords, "Transparency Keys");
            if (showTransparentKeywords)
            {
                //Intfield for size changes and constraining it above 0
                int newSize = transparencyKeywords.Length; newSize = EditorGUILayout.IntField("Size", newSize);
                newSize = newSize < 0 ? 0 : newSize;
                //If the value changed, resize the array
                if (newSize != transparencyKeywords.Length) Array.Resize(ref transparencyKeywords, newSize);

                //And loop the keyword array
                for (int i = 0; i < transparencyKeywords.Length; i++)
                {
                    //Then create an offset for the keyword textfield
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(155);
                    transparencyKeywords[i] = GUILayout.TextField(transparencyKeywords[i]);
                    GUILayout.EndHorizontal();
                }
            }//I don't feel like typing it twice
            showCutoutKeywords = EditorGUILayout.Foldout(showCutoutKeywords, "Cutout Keys");
            if (showCutoutKeywords)
            {
                int newSize = cutoutKeywords.Length; newSize = EditorGUILayout.IntField("Size", newSize);
                newSize = newSize < 0 ? 0 : newSize;
                if (newSize != cutoutKeywords.Length) Array.Resize(ref cutoutKeywords, newSize);

                for (int i = 0; i < cutoutKeywords.Length; i++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(155);
                    cutoutKeywords[i] = GUILayout.TextField(cutoutKeywords[i]);
                    GUILayout.EndHorizontal();
                }
            }

            //Then Lastly, Create the import from greyhound button at the bottom
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Import Assets From Greyhound"))
            {
                //And start a coroutine to import props and textures and create the materials using the file path we get from an OpenFolderPanel
                string xmodelPath = EditorUtility.OpenFolderPanel("Select Game Folder In Greyhound>exported_files Directory", "", "");
                if (xmodelPath != null)
                {
                    if (xmodelPath != "") EditorCoroutineUtility.StartCoroutine(ImportProps(xmodelPath), this);
                }
            }
        }
        //Else if the toolbar selection is map details
        else if (toolbarSelection == 1)
        {
            //Start with the text like before
            GUI.contentColor = Color.red;
            GUILayout.Label("Important!!! Make sure all textures and materials are already imported using the previous tab if you haven't already");
            GUI.contentColor = Color.yellow;
            GUILayout.Label("Import from (Where C2M is) exported_maps>(Game)>(sp or mp)>(Map Name) folder!");
            GUI.contentColor = Color.white;

            //Then again, lastly, Create the import from c2m button
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Import Assets From C2M"))
            {
                //And start a coroutine to import the map data using the file path we get from an OpenFolderPanel
                string mapPath = EditorUtility.OpenFolderPanel("Select Map Folder In exported_maps>(Game)>(sp or mp) Directory", "", "");
                if (mapPath != null)
                {
                    if (mapPath != "") EditorCoroutineUtility.StartCoroutine(ImportScene(mapPath), this);
                }
            }
        }
        //Else if the toolbar selection is build settings
        else
        {
            //Do the toggles
            createNewScene = EditorGUILayout.Toggle("Create New Scene", createNewScene);

            //Object fields
            xmodelsJson = EditorGUILayout.ObjectField("_xmodelsJson", xmodelsJson, typeof(TextAsset), false) as TextAsset;

            //And finally, lastly, Create the button to build if a json file is attached
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Build and Place") && xmodelsJson)
            {
                //And simpley coroutine the place method
                EditorCoroutineUtility.StartCoroutine(Place(), this);
            }
        }
        //And draw the status bar as a deactivated textfield
        GUI.enabled = false;
        GUILayout.Box(status, "textfield");
        GUI.enabled = true;
    }

    //Import and edit each prop from a given path
    IEnumerator ImportProps(string path)
    {
        //Set the status, repaint the gui and yield
        status = "Moving Texture Files...";
        Repaint();
        yield return null;

        //Start out by splitting the path and snagging the game name out of it
        string[] pathSplit = path.Split('/');
        string gameName = pathSplit[pathSplit.Length - 1];

        //If we don't have a game folder yet inside the resources folder, then create one
        if (!Directory.Exists(Application.dataPath + "/Resources/" + gameName)) Directory.CreateDirectory(Application.dataPath + "/Resources/" + gameName);

        //If the ximages folder exists from the load path
        if (Directory.Exists(path + "/ximages"))
        {
            //The local ximages folder hasn't been created yet, then create it
            if (!Directory.Exists(Application.dataPath + "/Resources/" + gameName + "/ximages")) Directory.CreateDirectory(Application.dataPath + "/Resources/" + gameName + "/ximages");

            //Get a list of all the subfolders
            string[] mFolders = Directory.GetDirectories(path + "/ximages");

            //For each of the subfolder directories
            foreach (string folder in mFolders)
            {
                //Split the path to grab names from and cache the local path that we will use
                string[] folderSplit = folder.Replace("\\", "/").Split('/');
                string folderName = "/Resources/" + gameName + "/ximages/" + folderSplit[folderSplit.Length - 1];

                //Get all the files from this subfolder
                string[] files = Directory.GetFiles(folder);

                //And if this local subfolder hasn't been added yet, add it
                if (!Directory.Exists(Application.dataPath + folderName)) Directory.CreateDirectory(Application.dataPath + folderName);

                //Then for each file path
                foreach (string file in files)
                {
                    //Split the files path and store the name
                    string[] fileSplit = file.Replace("\\", "/").Split('/');
                    string fileName = fileSplit[fileSplit.Length - 1];

                    //If the file isn't already in that folder
                    if (!File.Exists(Application.dataPath + folderName + "/" + fileName))
                    {
                        //Copy the file over
                        FileUtil.CopyFileOrDirectory(file.Replace("\\", "/"), "Assets" + folderName + "/" + fileName);
                    }
                    //Else if it does exist but we should replace it, then replace it
                    else if(replaceTexture) FileUtil.ReplaceFile(file.Replace("\\", "/"), "Assets/Resources/" + gameName + "/ximages/" + fileName);
                }
            }
            //Get an array of all the stray file laying around that aren't in folders
            string[] strayFiles = Directory.GetFiles(path + "/ximages");
            //And for each of those files
            foreach (string file in strayFiles)
            {
                //Split the path and store the name
                string[] fileSplit = file.Replace("\\", "/").Split('/');
                string fileName = fileSplit[fileSplit.Length - 1];

                //If this image doesn't exist yet in the save path
                if (!File.Exists(Application.dataPath + "/Resources/" + gameName + "/ximages/" + fileName))
                {
                    //Copy it over
                    FileUtil.CopyFileOrDirectory(file.Replace("\\", "/"), "Assets/Resources/" + gameName + "/ximages/" + fileName);
                }
                //If one is alread there and we should replace it, then replace it
                else if (replaceTexture) FileUtil.ReplaceFile(file.Replace("\\", "/"), "Assets/Resources/" + gameName + "/ximages/" + fileName);
            }
        }
        //Update the status, repaint the gui and yield
        status = "Importing Files...";
        Repaint();
        yield return null;

        //Refresh the assetdatabase because what the hell
        AssetDatabase.Refresh();

        //Update the status, repaint the gui and yield
        status = "Moving Model Files and Creating Skins...";
        Repaint();
        yield return null;

        //Create a list of skins we can add skins to
        List<Skin> skinList = new List<Skin>();

        //If the we have an xmodels directory
        if (Directory.Exists(path + "/xmodels"))
        {
            //Get an array of all the folders inside of it
            string[] mFolders = Directory.GetDirectories(path + "/xmodels");

            //And for each of those subfolders
            foreach (string folder in mFolders)
            {
                //Split the path so we can extract names out of it and go ahead and cache the models local directory that we will save it to
                string[] folderSplit = folder.Replace("\\", "/").Split('/');
                string folderName = "/Resources/" + gameName + "/xmodels/" + folderSplit[folderSplit.Length - 1];

                //Get an array of all the files inside the folder
                string[] files = Directory.GetFiles(folder);

                //If we don't have a local save directory for it yet, go ahead and create one
                if (!Directory.Exists(Application.dataPath + folderName)) Directory.CreateDirectory(Application.dataPath + folderName);

                //For each of the file paths inside the folder
                foreach (string file in files)
                {
                    //Split the file so we can use names out of it and might aswell snag the files name
                    string[] fileSplit = file.Replace("\\", "/").Split('/');
                    string fileName = fileSplit[fileSplit.Length - 1].Split('.')[0];

                    //If the file is an obj file
                    if (file.EndsWith(".obj"))
                    {
                        //If the file doesn't exist yet
                        if (!File.Exists(Application.dataPath + folderName + "/" + fileSplit[fileSplit.Length - 1]))
                        {
                            //Copy it over
                            FileUtil.CopyFileOrDirectory(file.Replace("\\", "/"), "Assets" + folderName + "/" + fileSplit[fileSplit.Length - 1]);
                        }
                        //If it does exist but we tell it to replace it, the replace it
                        else if(replaceModel) FileUtil.ReplaceFile(file.Replace("\\", "/"), "Assets" + folderName + "/" + fileSplit[fileSplit.Length - 1]);
                    }
                    //Else if it's text file that tells us about the images that this object uses
                    else if (file.EndsWith(".txt") && fileName.EndsWith("_images"))
                    {
                        //Then change the files name to have Mat, instead of _images unity can find it
                        string matName = fileName.Replace("_images", "Mat");

                        //Then create an empty skin
                        Skin s = null;
                        //And for each of the skins in the skinlist, if there is any yet
                        foreach (Skin matSkin in skinList)
                        {
                            //If it has the same name
                            if (matSkin.Name == matName)
                            {
                                //Then it probably is the same, so set the skin variable and break out
                                s = matSkin;
                                break;
                            }
                        }
                        //If the skin variable is null still
                        if (s == null)
                        {
                            //Then read in the skin and set the variable, set the name and add it to the list
                            s = new Skin(new StreamReader(file));
                            s.Name = matName;
                            skinList.Add(s);
                        }
                    }
                }
            }
        }
        //Update the status, repaint the gui and yield
        status = "Importing Models...";
        Repaint();
        yield return null;

        //Force refresh the assetdatabase... just because I want to
        AssetDatabase.Refresh();

        //Update the status and yield
        status = "Creating and Saving Materials";
        yield return null;
        
        //Start asset editing so it will pause updates
        AssetDatabase.StartAssetEditing();

        //We don't have a materials folder yet, go ahead and create it
        if (!Directory.Exists(Application.dataPath + "/Resources/" + gameName + "/xmaterials")) Directory.CreateDirectory(Application.dataPath + "/Resources/" + gameName + "/xmaterials");
        
        //Get an array of all the textures inside the ximages folder
        object[] o = Resources.LoadAll(gameName + "/ximages", typeof(Texture));

        //For each of our skins the proceedurally generated
        foreach (Skin skin in skinList)
        {
            //If the material hasn't been created yet or we should replace it anyways
            if (!File.Exists(Application.dataPath + "/Resources/" + gameName + "/xmaterials/" + skin.Name + ".mat") || replaceMaterial)
            {
                //Create the material using the skin and passing in transparency and cutout keywords and put it in the materials folder
                AssetDatabase.CreateAsset(skin.AsMaterial(o, transparencyKeywords, cutoutKeywords), "Assets/Resources/" + gameName + "/xmaterials/" + skin.Name + ".mat");
            }
        }
        //Stop asset editing and update the changes
        AssetDatabase.StopAssetEditing();

        //Change the status and yield
        status = "Remapping Materials to Models... Don't Panic!";
        yield return null;

        //Get an array of all the assets in the project... yeah yeah, we don't need all of them but I don't feel like doing it another way
        string[] assetPaths = AssetDatabase.GetAllAssetPaths();
        //Loop through all the asset paths
        foreach (string assetPath in assetPaths)
        {
            //If the assetpath path doesn't contain the game name, skip it
            if (!assetPath.Contains(gameName)) continue;

            //If the is an obj file
            if (assetPath.EndsWith(".obj"))
            {
                //Get the modelimporter using the path
                ModelImporter importer = ModelImporter.GetAtPath(assetPath) as ModelImporter;
                //If the models import settings are not what we want them to be yet or we should change them anyways
                if (importer.materialName != ModelImporterMaterialName.BasedOnMaterialName && importer.materialSearch != ModelImporterMaterialSearch.Everywhere || modelImportForce)
                {
                    //Remap the materials based on the material name and search everywhere in the project folder for it, then save and reimport it
                    importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Everywhere);
                    importer.SaveAndReimport();
                }
            }
        }

        //Update the status, repaint and yield
        status = "Creating Prefabs...";
        Repaint();
        yield return null;

        //Start asset editing so it wait til we're finished to update the editor
        AssetDatabase.StartAssetEditing();
        //Get an array of all the folders inside the xmodels folder
        string[] modelFolders = AssetDatabase.GetSubFolders("Assets/Resources/" + gameName + "/xmodels");
        
        //If the prefabs folder hasn't been created yet, go ahead and create it
        if (!Directory.Exists(Application.dataPath + "/Resources/" + gameName + "/Prefabs")) Directory.CreateDirectory(Application.dataPath + "/Resources/" + gameName + "/Prefabs");
        
        //For each of the folders inside the xmodels folder
        foreach (string modelFolder in modelFolders)
        {
            //Split the folder path so we can extract names out of it
            string[] folderSplit = modelFolder.Split("/");

            //If this prefab already exists and we're not supposed to replace it, just continue to the next one
            if (File.Exists(Application.dataPath + "/Resources/" + gameName + "/Prefabs/" + folderSplit[folderSplit.Length - 1] + ".prefab") && !replacePrefabs) continue;

            //Create an array of objects containing the assets in the folder
            object[] goAssets = Resources.LoadAll(gameName + "/xmodels/" + folderSplit[folderSplit.Length - 1], typeof(GameObject));

            //If there's more than one object, it must contain LOD's
            if (goAssets.Length > 1)
            {
                //Create a prefab parent in the scene, add the lod group and tell it how many LOD's it will have
                GameObject prefabParent = new GameObject(folderSplit[folderSplit.Length - 1]);
                LODGroup group = prefabParent.AddComponent<LODGroup>();
                LOD[] lods = new LOD[goAssets.Length];

                //For each LOD object
                for (int i = 0; i < goAssets.Length; i++)
                {
                    //Instantiate it in the scene with a zeroed transformation
                    GameObject go = Instantiate((GameObject)goAssets[i], Vector3.zero, Quaternion.identity);
                    //Parent it to the prefab parent
                    go.transform.parent = prefabParent.transform;
                    //Create a renderers array at a size of 1 and set the renderer in it
                    Renderer[] renderers = new Renderer[1];
                    renderers[0] = go.transform.GetChild(0).GetComponent<Renderer>();
                    //Then add the lod to the lod array subdividing distance based on how many assets there are and placing in the renderer array
                    lods[i] = new LOD(1.0f - (1.0f / (float)goAssets.Length) - ((float)i / (float)goAssets.Length), renderers);
                }
                //Set the LOD group using the lod array and recalculate the bounds
                group.SetLODs(lods);
                group.RecalculateBounds();
                
                //Then save the parent object as a prefab in the prefabs folder and destroy the scene object
                PrefabUtility.SaveAsPrefabAsset(prefabParent, "Assets/Resources/" + gameName + "/Prefabs/" + prefabParent.name + ".prefab");
                DestroyImmediate(prefabParent);
            }
            //Else if theres only 1 asset with no LOD's
            else if (goAssets.Length == 1)
            {
                //Instantiate the asset at a zeroed transformation, save it as a prefab in the prefab folder, then destroy the scene object
                GameObject go = Instantiate((GameObject)goAssets[0], Vector3.zero, Quaternion.identity);
                PrefabUtility.SaveAsPrefabAsset(go, "Assets/Resources/" + gameName + "/Prefabs/" + folderSplit[folderSplit.Length - 1] + ".prefab");
                DestroyImmediate(go);
            }
            //If for some reason there's no asset
            else
            {
                //Just print a warning saying we couldn't find it
                Debug.LogWarning("Could not find any objects in " + modelFolder + ". Skipping this folder in the prefab creation process.");
            }
        }
        //Stop asset editing and update changes
        AssetDatabase.StopAssetEditing();

        //Update the status, repaint the gui and return out
        status = "Ready!";
        Repaint();
        yield return null;
    }

    IEnumerator ImportScene(string path)
    {
        //Update the status message, Repaint the GUI and yield
        status = "Importing Scene Files...";
        Repaint();
        yield return null;

        //Split the path and grab the map name and game name based on the folder hierarchy
        string[] pathSplit = path.Split('/');
        string mapName = pathSplit[pathSplit.Length - 1];
        string gameName = pathSplit[pathSplit.Length - 3];

        //If the map folder has not been created yet, then create it
        if (!Directory.Exists(Application.dataPath + "/Resources/" + gameName + "/Scenes/" + mapName)) Directory.CreateDirectory(Application.dataPath + "/Resources/" + gameName + "/Scenes/" + mapName);

        //If we don't have a material data json file yet, go ahead and copy it over and refresh the database so we can access it as an asset
        if (!File.Exists(Application.dataPath + "/Resources/" + gameName + "/Scenes/" + mapName + "/" + mapName + "_matdata.json")) FileUtil.CopyFileOrDirectory(path.Replace("\\", " /") + "/" + mapName + "_matdata.json", "Assets/Resources/" + gameName + "/Scenes/" + mapName + "/" + mapName + "_matdata.json");
        AssetDatabase.Refresh();

        //Load the material data as an asset
        TextAsset matDataAsset = Resources.Load(gameName + "/Scenes/" + mapName + "/" + mapName + "_matdata") as TextAsset;
        //Reconfigure the json text so it's readable in our current format and create a skin using our edited text
        string jsonString = matDataAsset.text.Replace("{\"Materials\":", "{\"skins\":[").Replace("[{\"", "[{\"Name\":\"").Replace(":{", ",").Replace("},\"", "},{\"Name\":\"").Replace("Color Map", "Albedo").Replace(" Map", "").Replace("}}}", "}]}").Replace("_images", "").Replace("\\", "");
        Skins s = JsonUtility.FromJson<Skins>(jsonString);

        //Load all the images in our images folder
        object[] o = Resources.LoadAll(gameName + "/ximages", typeof(Texture));
        //Start asset editing so that we can import all at once
        AssetDatabase.StartAssetEditing();
        //Loop through all of the skins
        foreach (Skin skin in s.skins)
        {
            //If some reason dumb reason there's not a materials folder, then create it... Even tho there should be
            if (!Directory.Exists(Application.dataPath + "/Resources/" + gameName + "/xmaterials")) Directory.CreateDirectory(Application.dataPath + "/Resources/" + gameName + "/xmaterials");
            //If the material doesn't already exists or you chose to overwrite the file
            if (!File.Exists(Application.dataPath + "/Resources/" + gameName + "/xmaterials/" + skin.Name + ".mat") || replaceMaterial)
            {
                //Create the material asset using the skin as a material and passing in our keywords and adding the Mat to the end of the name so Unity finds it with when importing the models
                AssetDatabase.CreateAsset(skin.AsMaterial(o, transparencyKeywords, cutoutKeywords), "Assets/Resources/" + gameName + "/xmaterials/" + skin.Name + "Mat.mat");
            }
        }
        //Then stop asset editing
        AssetDatabase.StopAssetEditing();

        //Copy or replace the worldsettings json depending on our toggle
        if (!File.Exists(Application.dataPath + "/Resources/" + gameName + "/Scenes/" + mapName + "/" + mapName + "_worldsettings.json")) FileUtil.CopyFileOrDirectory(path.Replace("\\", "/") + "/" + mapName + "_worldsettings.json", "Assets/Resources/" + gameName + "/Scenes/" + mapName + "/" + mapName + "_worldsettings.json");
        else if(replaceSceneFiles) FileUtil.ReplaceFile(path.Replace("\\", "/") + "/" + mapName + "_worldsettings.json", "Assets/Resources/" + gameName + "/Scenes/" + mapName + "/" + mapName + "_worldsettings.json");
        //Copy or replace the xmodels json depending on our toggle
        if (!File.Exists(Application.dataPath + "/Resources/" + gameName + "/Scenes/" + mapName + "/" + mapName + "_xmodels.json")) FileUtil.CopyFileOrDirectory(path.Replace("\\", " /") + "/" + mapName + "_xmodels.json", "Assets/Resources/" + gameName + "/Scenes/" + mapName + "/" + mapName + "_xmodels.json");
        else if (replaceSceneFiles) FileUtil.ReplaceFile(path.Replace("\\", " /") + "/" + mapName + "_xmodels.json", "Assets/Resources/" + gameName + "/Scenes/" + mapName + "/" + mapName + "_xmodels.json");
        //Copy or replace the obj file depending on our toggle
        if (!File.Exists(Application.dataPath + "/Resources/" + gameName + "/Scenes/" + mapName + "/" + mapName + ".obj")) FileUtil.CopyFileOrDirectory(path.Replace("\\", "/") + "/" + mapName + ".obj", "Assets/Resources/" + gameName + "/Scenes/" + mapName + "/" + mapName + ".obj");
        else if(replaceSceneFiles) FileUtil.ReplaceFile(path.Replace("\\", "/") + "/" + mapName + ".obj", "Assets/Resources/" + gameName + "/Scenes/" + mapName + "/" + mapName + ".obj");

        //Refresh the asset database
        AssetDatabase.Refresh();

        //Get the modelimporter for the basemap
        ModelImporter importer = ModelImporter.GetAtPath("Assets/Resources/" + gameName + "/Scenes/" + mapName + "/" + mapName + ".obj") as ModelImporter;
        //Remap the materials
        importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Everywhere);
        //Then save and reimport it
        importer.SaveAndReimport();

        //Revert the status back to ready and repaint the gui
        status = "Ready!";
        Repaint();

        //Then yield the null return
        yield return null;
    }

    //A coroutined method for placing the map models
    IEnumerator Place()
    {
        //Change the status bar text, repaint and yield
        status = "Gathering Map Data";
        Repaint();
        yield return null;

        //Get the file location for the json, then extract the names and path we need
        string fileLocation = AssetDatabase.GetAssetPath(xmodelsJson);
        string[] locationSplit = fileLocation.Split('/');
        string gameName = locationSplit[locationSplit.Length - 4];
        string mapName = locationSplit[locationSplit.Length - 2];
        string mapFolder = gameName + "/Scenes/" + mapName + "/";

        //If we are supposed to create a new scene and the scene has not already been created
        if(createNewScene && !Application.CanStreamedLevelBeLoaded(mapName))
        {
            //Create a new scene and open it by itself
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            //Then save the scene as the map name
            EditorSceneManager.SaveScene(scene, "Assets/Resources/" + gameName + "/Scenes/" + mapName + "/" + mapName + ".unity");
        }

        //Load the worldsettings data as an asset
        TextAsset settingsJson = Resources.Load(mapFolder + mapName + "_worldsettings") as TextAsset;
        WorldSettings ws = JsonUtility.FromJson<WorldSettings>(settingsJson.text);

        //Find or create a camera
        GameObject camGO = GameObject.Find("Main Camera");
        Camera mainCamera;
        if (camGO == null) mainCamera = new GameObject("Main Camera").AddComponent<Camera>();
        else mainCamera = camGO.GetComponent<Camera>();
        //Set the camera settings
        Color skyColor = ws.SkyColor();
        if (skyColor.a != 0)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = skyColor;
        }

        //Find or create the directional light
        GameObject lightGO = GameObject.Find("Directional Light");
        if (lightGO == null)
        {
            lightGO = new GameObject("Directional Light");
            Light l = lightGO.AddComponent<Light>();
            l.type = LightType.Directional;
        }
        //Set the light settings
        Quaternion sunDirection = ws.SunDirection(); if(sunDirection != Quaternion.identity) lightGO.transform.rotation = sunDirection;
        Light light = lightGO.GetComponent<Light>();
        Color sunColor = ws.SunColor(); if(sunColor.a != 0) light.color = sunColor;
        if(ws.sunlight != 0) light.intensity = ws.sunlight;

        //Adjust the ambient render setting
        if(ws.ambient != 0) RenderSettings.ambientIntensity = ws.ambient;

        //Read all of the text from the json file
        string jsonString = xmodelsJson.text;

        //Create an entities object using the text we read in from the file
        //Very important to add some text so that it knows that it will be adding to the entities list variable... These specific json files require it
        Entities e = JsonUtility.FromJson<Entities>("{\"entities\":" + jsonString + "}");

        //Get all the models that are stored in Resources>modern_warfare_2
        object[] prefabs = Resources.LoadAll(gameName + "/Prefabs", typeof(GameObject));

        //Create a props variable and make it the props object in the scene if there is one
        GameObject props = GameObject.Find("Props");

        //If there's already a props object, destroy it so we can create a new one
        if (props) DestroyImmediate(props);

        //Load the level prefab
        GameObject level = (GameObject)Resources.Load(mapFolder + mapName, typeof(GameObject)) as GameObject;

        //If the level prefab exists
        if (level != null)
        {
            //Instaniate it into the scene and set the transformations. Requires a rotation  on the x axis since our y is up, not forward like theres
            GameObject baseMap = Instantiate(level, Vector3.zero, Quaternion.Euler(-90, 0, 0));
            baseMap.transform.localScale = Vector3.one * 0.03937f;
        }

        //Set the props as a new gameobject
        props = new GameObject("Props");
        //Then loop through all the entities
        foreach (Entity entity in e.entities)
        {
            //If the entities name is null, it's probably not an actual entities so skip it
            if (entity.Name == null) continue;
            
            //Update the status bar
            status = "Placing " + entity.Name;

            //Then find or create a new entity parent to group all instances of this object and parent it to the props object
            GameObject entityParent = GameObject.Find(entity.Name + "_Group");
            if(entityParent == null) entityParent = new GameObject(entity.Name + "_Group");
            entityParent.transform.parent = props.transform;

            //Then loop through the prefabs
            foreach (object prefab in prefabs)
            {
                //Change the type to gameobject and if it has the same name
                GameObject p = (GameObject)prefab;
                if (p.name == entity.Name)
                {
                    //Instantiate it as a prefab, set the transformations and parent it to the group
                    Transform t = ((GameObject)PrefabUtility.InstantiatePrefab(p)).transform;
                    t.position = entity.Position();
                    t.rotation = entity.Rotation();
                    t.localScale = entity.LocaleScale();
                    t.parent = entityParent.transform;

                    //Then rotate it on the x axis since our y is up, not forward like theres
                    t.Rotate(-90, 0, 0, Space.World);
                }
            }
            //Then repaint the gui and unlock the loop for a frame
            Repaint();
            yield return null;
        }

        //And update the status and repaint the gui
        status = "Ready!";
        Repaint();
    }
}

//Serialize class to store the world settings read from the worldsettings json file
[System.Serializable] public class WorldSettings
{
    //Pretty self explanatory
    public string suncolor;
    public float sunlight;
    public string sundirection;
    public float ambient;
    public string skycolor;

    //Retrieve the suncolor string as a color
    public Color SunColor()
    {
        if (suncolor != null)
        {
            //Split the string at the space
            string[] split = suncolor.Split(" ");
            //Parse the rgb values and return the color
            return new Color(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]), 1);
        }
        return new Color(0, 0, 0, 0);
    }
    //Retrieve the skycolor string as a color
    public Color SkyColor()
    {
        if (skycolor != null)
        {
            //Split the string at the space
            string[] split = skycolor.Split(" ");
            //Parse the rgb values and return the color
            return new Color(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]), 1);
        }
        return new Color(0, 0, 0, 0);
    }
    //Retrieve the sundirection as a vector3
    public Quaternion SunDirection()
    {
        if (sundirection != null)
        {
            //Split the string at the space
            string[] split = sundirection.Split(" ");
            //Parse each value and return the vector
            return Quaternion.Euler(new Vector3(-float.Parse(split[0]), float.Parse(split[1]), -float.Parse(split[2])));
        }
        return Quaternion.identity;
    }
}

//Serizable entity class that holds information about each entity instance
[System.Serializable]
public class Entity
{
    //All the variables to match the json keys
    public string Name;
    public float RotX, RotY, RotZ;
    public float PosX, PosY, PosZ;
    public float Scale;

    //The scale factor to shrink it down for unity
    public float unityScalingFactor = 0.1f;

    //Return the left hand position value adjusted with our scaling factor
    public Vector3 Position() { return new Vector3(-PosX, PosZ, -PosY) * unityScalingFactor; }

    //Return an inverted rotation to match the left hand matrix
    public Quaternion Rotation() { return Quaternion.Inverse(Quaternion.Euler(new Vector3(-RotX, RotY, RotZ))); }

    //Return the adjusted scale to match our scaling factor
    public Vector3 LocaleScale() { return (new Vector3(Scale, -Scale, Scale) * 0.3937f) * unityScalingFactor; }
}
//Just a list of entities that matches the the json key
[System.Serializable] public class Entities { public List<Entity> entities; }

//Serialized skin class that holds the material information
[System.Serializable]
public class Skin
{
    //All the variables we need to assign the information we need to the material. Also matches our converted json file materials on the basemap
    public string Name;
    public string Albedo;
    public string Specular;
    public string Normal;
    public string Detail;

    //Just an empty construction incase for some reason i want to create an empty one at some point. Useless for now
    public Skin() { }

    //Create a skin using a streamreader
    public Skin(StreamReader sr)
    {
        //Read the line. While the line isn't empty
        string line = sr.ReadLine();
        while (line != null)
        {
            //Split the line at the comma
            string[] split = line.Split(',');

            //And compare the text in the 2nd element and assign it to the proper variables
            if (split[0] == "colorMap") Albedo = split[1];
            else if (split[0] == "specularMap") Specular = split[1];
            else if (split[0] == "normalMap") Normal = split[1];
            else if (split[0] == "detailMap") Detail = split[1];

            //Then move on to the next line
            line = sr.ReadLine();
        }
        //And close the streamreader
        sr.Close();
    }

    //Convert the the skin to an actual material by passing in all the textures we have and arrays of keywords for material types
    public Material AsMaterial(object[] o, string[] transparencyKeywords, string[] cutoutKeywords)
    {
        //A material variable we can assign
        Material mat;

        //If it has a specular image
        if(Specular != null)
        {
            //Create a material with a specular setup shader
            mat = new Material(Shader.Find("Standard (Specular setup)"));
            mat.SetTexture("_SpecGlossMap", GetTexture(Specular, o));
        }
        //If not, create a material with just a standard shader
        else mat = new Material(Shader.Find("Standard"));

        //If it has an albedo texture, get and set the maintex
        if (Albedo != null) mat.SetTexture("_MainTex", GetTexture(Albedo, o));

        //If it has a normal texture, get and set the bumpmap
        if (Normal != null) mat.SetTexture("_BumpMap", GetTexture(Normal, o));

        //Loop thru all the transparency keywords
        foreach (string key in transparencyKeywords)
        {
            //If the name of the material contains this keyword
            if (Name.Contains(key))
            {
                //Set the material rendermode to transparent
                mat.SetFloat("_Mode", 3);

                //Then return the material
                return mat;
            }
        }
        //Loop thru all the cutout keywords
        foreach (string key in cutoutKeywords)
        {
            //If the name of the material contains this keyword
            if (Name.Contains(key))
            {
                //Set the material rendermode to cutout and break out of the loop
                mat.SetFloat("_Mode", 1);

                //Then return the material
                return mat;
            }
        }
        //If it doesn't contain any of the keyword filters, return the material as is
        return mat;
    }

    //Get texture from an object array using a sample name
    public Texture GetTexture(string n, object[] o)
    {
        //For each of the objects
        foreach(object tex in o)
        {
            //Cache it as a texture
            Texture t = (Texture) tex;

            //If it's name is the same as our sample
            if (t.name == n) return t;
        }
        //Create a log saying we couldn't find it and return null
        Debug.Log("Could not find " + n + " texture");
        return null;
    }
}
//Just a list of skins
[System.Serializable] public class Skins { public List<Skin> skins; }
