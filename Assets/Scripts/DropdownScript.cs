using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Data;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using Mono.Data.Sqlite;
#elif UNITY_ANDROID
using Mono.Data.SqliteClient;
#endif

public class DropdownScript : MonoBehaviour
{
    public Dropdown dropdown;
    public Button navigateButton;
    public string selectedDepartment = "Select a department";
    
    public string connectionString;
    private CommonScript commonScript;
    string originalPath;

    List<string> departments = new List<string>(){ "Select a department"};

    private string levels_conn;
    private IDbConnection levels_dbconn;
    private IDbCommand levels_dbcmd;

    private void OnEnable()
    {
        Debug.Log(Application.persistentDataPath);
#if UNITY_EDITOR
        levels_conn = "URI=file:" + Application.dataPath + "/StreamingAssets/UpdatedAcquireDestinationLatLong.sqlite";
#elif UNITY_ANDROID
		//check if file exists in Application.persistentDataPath

       originalPath = Application.persistentDataPath + "/UpdatedAcquireDestinationLatLong.sqlite";
        if (!File.Exists(originalPath))
        {
            //WWW load = new WWW("jar:file://" + Application.dataPath + "!/assets/Database/AcquireDestinationLatLong.sqlite");
            WWW load = new WWW(Application.streamingAssetsPath + "/UpdatedAcquireDestinationLatLong.sqlite");
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

    public void DropdownIndexChanged(int index)
    {
        selectedDepartment = departments[index];
        Debug.Log(selectedDepartment);
     
        if(selectedDepartment.Equals(departments[0]))
        {
            navigateButton.interactable = false;
        }
        else
        {
            navigateButton.interactable = true;
        }       

        commonScript.UpdateDestination(selectedDepartment);
    }

    void Start()
    {
        commonScript = GameObject.FindObjectOfType<CommonScript>();
        navigateButton.interactable = false;
        PopulateList();
    }

    void PopulateList()
    {
        string sqlQuery = "select Department from DepartmentLatLong order by Department;";
        Debug.Log(sqlQuery);

        levels_dbcmd.CommandText  = sqlQuery;
        IDataReader reader = levels_dbcmd.ExecuteReader();
        
        while(reader.Read())
        {
            departments.Add(reader.GetString(0));
        }

        reader.Close();
        reader = null;

        dropdown.AddOptions(departments);        
    }

    public void SceneLoader(int sceneIndex)
    {
        if(navigateButton.interactable) 
        {
            SceneManager.LoadScene(sceneIndex);
        }

        else 
            return;
    }
}
