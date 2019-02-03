using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
class HighscoreEntry
{
    public float score;
    public string name;

    public HighscoreEntry(float _score, string _name)
    {
        score = _score;
        name = _name;
    }
}

[System.Serializable]
class HighscoreCollectionServer
{
    public HighscoreEntryServer[] scores;
}

[System.Serializable]
class HighscoreEntryServer
{
    public float score;
    public string name;
    public int time;

    public HighscoreEntryServer(float _score, string _name, int _time)
    {
        score = _score;
        name = _name;
        time = _time;
    }
}

public class DescComparer<T> : IComparer<T>
{
    public int Compare(T x, T y)
    {
        return Comparer<T>.Default.Compare(y, x);
    }
}

public static class Highscore
{
    static SortedList<float, string> localHighscores = new SortedList< float, string>(new DescComparer<float>());
    static SortedList<float, string> onlineHighscores = new SortedList<float, string>(new DescComparer<float>());

    public static void loadOnlineHighscores()
    {
        // Create a form object for sending high score data to the server
        WWWForm form = new WWWForm();

        form.AddField("method", "getScores");
        
        // Create a download object
        WWW download = new WWW("https://arbowling.klauswagner.com/service/", form);

        //yield return download;
        // Wait until the download is done
        float timeOut = Time.time + 5;
        pause(0.1f);
        while (!download.isDone && timeOut > Time.time) //wait max 5 seconds
        {
            pause(0.5f);
            //timeWaited += 0.1f;
        }
        if (download.isDone && download.error == null)
        {
            HighscoreCollectionServer highscores = JsonUtility.FromJson<HighscoreCollectionServer>("{ \"scores\": " + download.text + "}");
            if (highscores.scores != null)
            {
                for (int i = 0; i < highscores.scores.Length; ++i)
                {
                    onlineHighscores.Add(highscores.scores[i].score, highscores.scores[i].name);
                }
            }

        }
        else
        {
            Debug.Log("ERROR: " + download.error);
        }

    }

    static IEnumerator pause(float time)
    {
        yield return new WaitForSeconds(time);
    }

    public static void loadLocalHighscores()
    {
        string serializedHighscores = PlayerPrefs.GetString("localHighscores");

        if(serializedHighscores == null || serializedHighscores.Length == 0)
        {
            return;
        }

        HighscoreEntry[] highscoreArray = JsonHelper.getJsonArray<HighscoreEntry>(serializedHighscores);

        localHighscores.Clear();

        for (int i = 0; i < highscoreArray.Length; ++i)
        {
            localHighscores.Add(highscoreArray[i].score, highscoreArray[i].name);
        }
    }

    public static void writeLocalHighscores()
    {
        HighscoreEntry[] highscoreArray = new HighscoreEntry[localHighscores.Count];
        int ctr = 0;

        foreach (KeyValuePair<float, string> kvp in localHighscores)
        {
            highscoreArray[ctr] = new HighscoreEntry(kvp.Key, kvp.Value);
            ctr++;
        }

        string serializedHighscores = JsonHelper.arrayToJson<HighscoreEntry>(highscoreArray);

        PlayerPrefs.SetString("localHighscores", serializedHighscores);
    }

    public static void writeOnlineHighscores(string name, float score)
    {
        // Create a form object for sending high score data to the server
        WWWForm form = new WWWForm();

        form.AddField("method", "storeScore");
        // The name of the player submitting the scores
        form.AddField("name", name);
        // The score
        form.AddField("score", score.ToString());
        
        // Create a download object
        WWW upload = new WWW("https://arbowling.klauswagner.com/service/", form);

        // Wait until the upload (/download) is done
        //yield return upload;
    }

    public static void addHighscore(string name, float score)
    {
        Highscore.localHighscores.Add(score, name);
        Highscore.onlineHighscores.Add(score, name);

        Highscore.writeLocalHighscores();
        Highscore.writeOnlineHighscores(name, score);
    }

    public static SortedList<float, string> getOnlineHighscores()
    {
        return Highscore.onlineHighscores;
    }

    public static SortedList<float, string> getLocalHighscores()
    {
        return Highscore.localHighscores;
    }
}
