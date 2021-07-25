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

public class GPSController : MonoBehaviour
{
    string message = "Initialising GPS...";
    float thisLat;
    float thisLong;
    float destLat;
    float destLong;
    float diff;
    float angle;
    public static float distance = 0;
    public static float bearing = 0;

    public GameObject compassNeedle ;
    public Text destinationReached;
    public Text DestinationDisplay;

    public string connectionString;
    string originalPath;
    private string levels_conn;
    private IDbConnection levels_dbconn;
    private IDbCommand levels_dbcmd;

    DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
   
    private void OnEnable()
    {
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


        Debug.Log("Selected department: " + CommonScript.dest);

        string sqlQuery = "select Latitude, Longitude from DepartmentLatLong where Department = '" + CommonScript.dest + "';";
        Debug.Log(sqlQuery);

        levels_dbcmd.CommandText = sqlQuery;
        IDataReader reader = levels_dbcmd.ExecuteReader();
        while (reader.Read())
        {
            destLat = reader.GetFloat(0);
            destLong = reader.GetFloat(1);
        }
        Debug.Log(destLat + " " + destLong);
        
        reader.Close();
    }

    private void OnDisable()
    {
        levels_dbcmd.Dispose();
        levels_dbcmd = null;

        levels_dbconn.Close();
        levels_dbconn = null;
    }

    void OnGUI()
    {
        GUI.skin.label.fontSize = 60;
    //    GUI.Label(new Rect(30, 30, 1000, 1000), message);
    }

    IEnumerator StartGPS()
    {
        message = "Starting...";

        if(!Input.location.isEnabledByUser)
        {
            message = "Location services disabled...";
            yield break;
        }

        //Start service before querying location
        Input.location.Start(5, 0);         // Start(accuracy, displacement)

        int maxWait = 5;
        while(Input.location.status == LocationServiceStatus.Initializing && maxWait>0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if(maxWait<1)
        {
            message = "Timed Out";
            yield break;
        }

        if(Input.location.status == LocationServiceStatus.Failed)
        {
            message = "Unable to determine location..";
            yield break;
        }

        else
        {
            Input.compass.enabled = true;
 
           /*  message = "Lat: " + Input.location.lastData.latitude + 
                      "\nLong: " +  Input.location.lastData.longitude + 
                      "\nAlt: " +  Input.location.lastData.altitude +
                      "\nHorizontal Accuracy: " +  Input.location.lastData.horizontalAccuracy + 
                      "\nVertical Accuracy: " +  Input.location.lastData.verticalAccuracy + 
                      "\nHeading: " + Input.compass.trueHeading;*/
       }
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(StartGPS());
        
    }

    // Update is called once per frame
    void Update()
    {
        DateTime lastUpdate = epoch.AddSeconds(Input.location.lastData.timestamp);
        DateTime rightNow = DateTime.Now;

        thisLat = Input.location.lastData.latitude;
        thisLong = Input.location.lastData.longitude;

        float distance = Haversine(thisLat, thisLong, destLat, destLong);

        bearing=Bearing(thisLat,thisLong,destLat,destLong);

        if(Input.compass.trueHeading < bearing)
        {
            angle = bearing - Input.compass.trueHeading;
        }

        else
        {
            angle = 360 - (Input.compass.trueHeading - bearing);
        }

       // message ="Navigating to : Amphitheatre\nDistance: " + distance +"m";
                    /*  "Bearing: " + bearing +
                  "\ntrueHeading: " + Input.compass.trueHeading +
                  "\nAngle: " + angle +
                  "\nCurrent Lat: " + thisLat +
                  "\nCurrent Long: " + thisLong +
                  "\nUpdate Time: "+ lastUpdate.ToString("HH:mm:ss") +
                  "\nNow: " + rightNow.ToString("HH:mm:ss");

                     heading=Input.compass.trueHeading;
                bearing = (bearing-Input.compass.trueHeading +360.0f)%360.0f ;

                diff = 360.0f-Input.compass.trueHeading;
                diff = 360.0f - (diff + bearing);*/
        
        DestinationDisplay.text = "Navigating to : " + CommonScript.dest + "\nDistance Remaining: " + Mathf.Round(distance) + " meters";
        compassNeedle.transform.localRotation= Quaternion.Euler(0,0,-angle);        
       
       // if(thisLat>=18.52316 && thisLat<=18.52344 && thisLong>=73.83880 && thisLong<=73.83885)
        if(distance<=10.0)
        {
            compassNeedle.SetActive(false);
            destinationReached.text = CommonScript.dest;
            //destinationReached.SetActive(true);
        }   
        else
        {
            destinationReached.text = "";
        }    //destinationReached.SetActive(false);    
    }

    float Bearing(float lat1,float long1,float lat2,float long2)
    {
         float lRad1 = lat1 * Mathf.Deg2Rad;
        float lRad2 = lat2 * Mathf.Deg2Rad;

        float dLong = (long2 - long1) * Mathf.Deg2Rad;

        float x =  Mathf.Cos(lRad2) * Mathf.Sin(dLong);
        float y = (Mathf.Cos(lRad1) * Mathf.Sin(lRad2)) - (Mathf.Sin(lRad1) * Mathf.Cos(lRad2) * Mathf.Cos(dLong));

        float beta = Mathf.Atan2(x,y);

        float angle = beta * Mathf.Rad2Deg;

        return ((angle + 360.0f)% 360.0f);
    }

     float Haversine(float lat1, float long1, float lat2, float long2)
    {
        float earthRad = 6371000;
        float lRad1 = lat1 * Mathf.Deg2Rad;
        float lRad2 = lat2 * Mathf.Deg2Rad;
        float dLat = (lat2 - lat1) * Mathf.Deg2Rad;
        float dLong = (long2 - long1) * Mathf.Deg2Rad;
        float a = Mathf.Sin(dLat/2.0f) * Mathf.Sin(dLat/2.0f) +
                  Mathf.Cos(lRad1) * Mathf.Cos(lRad2) *
                  Mathf.Sin(dLong/2.0f) * Mathf.Sin(dLong/2.0f);

        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a),Mathf.Sqrt(1-a));

        return earthRad * c;
    }
}
