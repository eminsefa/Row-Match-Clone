using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class LevelData
{
    public string FilePath;
    public int LevelNumber;
    public int GridWidth;
    public int GridHeight;
    public int MoveCount;
    public int HighScore;
    public int Unlocked;
    public List<string> Grid = new List<string>();
}

//Some prefer not to use static classes. But I think it is not risky to use here
public static class LevelDataHandler
{
    private const int LevelCount = 25;
    private static bool _levelDownloaded;
    private static bool _levelListUpdated;
    public static bool HighScored { get; set; }
    public static int NextLevelToUnlock { get; private set; }
    private static List<LevelData> _orderedLevelList;

    public static List<LevelData> ReadLevelData()
    {
        //Calling this method every time main scene launches
        //High score and unlock data could be saved somewhere else and this method could be called once
        //But this way it is dynamic and seemed performant enough
        var persistentDataPath = Application.persistentDataPath + "/Levels/";
        var levelDataList = new List<LevelData>();
        _levelListUpdated = false;

        //Add resources data files to persistent data path on first launch. So all files stay in same directory
        if (PlayerPrefs.GetInt("FirstLaunch", 1) == 1)
        {
            //Could not make resources method work on non text asset files
            //Could not find a way to convert to text assets via code
            //Converted first 10 documents to txt then added to resources
            PlayerPrefs.SetInt("FirstLaunch", 0);

            if (!Directory.Exists(persistentDataPath)) Directory.CreateDirectory(persistentDataPath);
            else //Clear data on clear player pref
            {
                foreach (var f in Directory.GetFiles(persistentDataPath)) File.Delete(f);
            }

            var levelsDefault = Resources.LoadAll<TextAsset>("Levels");
            var assetFileInfo = levelsDefault
                .Where(x => x.name.Contains("RM") && !x.name.Contains("meta")).ToArray(); //Ignore meta files
            foreach (var f in assetFileInfo)
            {
                var result = f.text;
                File.WriteAllText(persistentDataPath + f.name, result);
            }
        }

        //Read existing files
        var info = new DirectoryInfo(persistentDataPath);
        var fileInfo = info.GetFiles().Where(x => x.Name.Contains("RM")).ToArray(); //Double check for only level files
        foreach (var f in fileInfo)
        {
            var path = Application.persistentDataPath + "/Levels/" + f.Name;
            var result = File.ReadAllLines(path);

            //Get numbers from strings. Added unlocked and high score variables here
            var levelNumber = result[0].Replace("level_number: ", "");
            if (_levelDownloaded && int.Parse(levelNumber) > _orderedLevelList.Count)
                continue; //This is where code passes data which recently downloaded

            var gridWidth = result[1].Replace("grid_width: ", "");
            var gridHeight = result[2].Replace("grid_height: ", "");
            var moveCount = result[3].Replace("move_count: ", "");
            var grid = result[4].Replace("grid: ", "");
            var unlocked = "0";
            if (result.Length > 5) unlocked = result[5].Replace("unlocked: ", "");
            var highScore = "0";
            if (result.Length > 6) highScore = result[6].Replace("high_score: ", "");

            //Convert data and create level data for each file
            var levelData = new LevelData()
            {
                FilePath = path,
                LevelNumber = int.Parse(levelNumber),
                GridWidth = int.Parse(gridWidth),
                GridHeight = int.Parse(gridHeight),
                MoveCount = int.Parse(moveCount),
                HighScore = int.Parse(highScore),
                Unlocked = int.Parse(unlocked),
                Grid = grid.Split(new char[] { ',' }).ToList()
            };
            levelDataList.Add(levelData);
        }

        _orderedLevelList =
            levelDataList.OrderBy(x => x.LevelNumber).ToList(); //Order by level number for possible order bugs

        //Find the next locked level
        for (int i = 0; i < _orderedLevelList.Count; i++)
        {
            if (_orderedLevelList[i].Unlocked == 0)
            {
                NextLevelToUnlock = i + 1;
                break;
            }

            NextLevelToUnlock = 0; //If all levels are unlocked
        }

        _levelListUpdated = true; //This is only for read to compile before download
        return _orderedLevelList;
    }

    public static void LevelEndedWithHighScore(int level, int score)
    {
        var currentPath = _orderedLevelList[level - 1].FilePath;
        var result = File.ReadAllLines(currentPath).ToList();
        if (result.Count > 6) result[6] = "high_score: " + score;
        else result.Add("high_score: " + score);
        File.WriteAllLines(currentPath, result);

        //Unlock next level
        if (_orderedLevelList.Count > level &&
            _orderedLevelList[level].Unlocked != 1) //Return if next level or all levels are already unlocked 
        {
            HighScored = true;
            var nextPath = _orderedLevelList[level].FilePath;
            var resultNext = File.ReadAllLines(nextPath).ToList();
            if (resultNext.Count <= 5) resultNext.Add("unlocked: " + 1);
            File.WriteAllLines(nextPath, resultNext);
        }
        else HighScored = false;
    }

    public static IEnumerator DownloadLevels()
    {
        //This is called once on application start, retries every 5 second until device connects internet
        while (!_levelListUpdated) //To Download levels after first initialize
        {
            yield return null;
        }

        if (_orderedLevelList.Count == LevelCount) yield break; //If all levels downloaded
        _levelDownloaded = true;

        var persistentDataPath = Application.persistentDataPath + "/Levels/";
        var mainURL = DownloadHelper.GetMainURL;
        var levelNames = DownloadHelper.GetLevelNames(LevelCount);

        //-----Poor Fix -----
        //If there is no connection, does not check for each file
        //If connection is lost in for loop web request does not work and coroutine ends
        //So if the connection is lost during writing coroutine starts back from here
        //It is probably normal but I could not find a better solution
        var www = UnityWebRequest.Get(mainURL);
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            yield return new WaitForSeconds(5f);
            SceneHandler.Instance
                .StartCoroutine(DownloadLevels()); //Restarted coroutine instead of waiting for some network errors 
            yield break;
        }
        //-----Poor Fix -----

        for (int i = _orderedLevelList.Count; i < levelNames.Count; i++) //Pass already downloaded levels
        {
            var path = persistentDataPath + levelNames[i];

            if (Directory.Exists(path)) continue; //If data is downloaded and coroutine restarted while writing
            //This might be a bad idea because it blocks the possibility to overwrite levels

            www = UnityWebRequest.Get(mainURL + levelNames[i]);
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError) //If connection is lost during download
            {
                yield return new WaitForSeconds(5f);
                SceneHandler.Instance.StartCoroutine(DownloadLevels());
                yield break;
            }

            File.WriteAllText(path, www.downloadHandler.text);
        }
    }
}