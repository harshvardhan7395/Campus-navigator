using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Data;
using System.IO;

#if UNITY_EDITOR
using Mono.Data.Sqlite;
#elif UNITY_ANDROID
using Mono.Data.SqliteClient;
#endif

public class GetDestination : MonoBehaviour
{
    public Text destinationText;
    public Text destinationLatLong;
    private CommonScript commonScript;

    public string connectionString;

    string originalPath;
    private string levels_conn;
    private IDbConnection levels_dbconn;
    private IDbCommand levels_dbcmd;

    private void OnEnable()
    {
#if UNITY_EDITOR
        levels_conn = "URI=file:" + Application.dataPath + "/StreamingAssets/AcquireDestinationLatLong.sqlite";
#elif UNITY_ANDROID
		//check if file exists in Application.persistentDataPath

       originalPath = Application.persistentDataPath + "/AcquireDestinationLatLong.sqlite";
        if (!File.Exists(originalPath))
        {
            //WWW load = new WWW("jar:file://" + Application.dataPath + "!/assets/Database/AcquireDestinationLatLong.sqlite");
            WWW load = new WWW(Application.streamingAssetsPath + "/AcquireDestinationLatLong.sqlite");
            while (!load.isDone)
            { }
            File.WriteAllBytes(originalPath, load.bytes);
        }

        levels_conn = "URI=file:" + originalPath;
#endif

        levels_dbconn = (IDbConnection)new SqliteConnection(levels_conn);
        levels_dbconn.Open();
        levels_dbcmd = levels_dbconn.CreateCommand();
    }

    private void OnDisable()
    {
        levels_dbcmd.Dispose();
        levels_dbcmd = null;

        levels_dbconn.Close();
        levels_dbconn = null;
    }

    void Start()
    {
        UpdateDestination();
    }
   

    public void UpdateDestination()
    {
        destinationText.text = "Selected Department: " + CommonScript.dest;

        string sqlQuery = "select Latitude, Longitude from DepartmentLatLong where Department = '" + CommonScript.dest + "';";
        Debug.Log(sqlQuery);

        levels_dbcmd.CommandText = sqlQuery;

        IDataReader reader = levels_dbcmd.ExecuteReader();
        while (reader.Read())
        {
            destinationLatLong.text = reader.GetFloat(0).ToString() + ", " + reader.GetFloat(1).ToString();
        }

        reader.Close();

    }

}
