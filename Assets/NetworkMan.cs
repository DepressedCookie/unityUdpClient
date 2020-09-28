using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using Unity.Mathematics;
using System.Security.Cryptography;


public class NetworkMan : MonoBehaviour
{
    List<GameObject> AllPlayers = new List<GameObject>();
    Player ThisPlayer = new Player();
    int NPlayersCount = 0;

    // The player prefab
    [SerializeField]
    GameObject PlayerPrefab;
 
    // All the spawn points in the world
    GameObject[] SpawnPoints;

    public UdpClient udp;
    // Start is called before the first frame update
    void Start()
    {
        SetupSpawnPoints();

        udp = new UdpClient();

        bool useLocalhost = true;
        string IPADD = "3.137.148.38";

        if (useLocalhost)
            IPADD = "localhost";

        udp.Connect(IPADD, 12345);

        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
      
        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 1);
    }

    void SetupSpawnPoints()
    {
        SpawnPoints = GameObject.FindGameObjectsWithTag("Spawnpoint");
    }

    void OnDestroy(){
        udp.Dispose();
    }


    public enum commands{
        NEW_CLIENT,
        UPDATE,
        DISCONNECT,
        OLD_CLIENT
    };
    
    [Serializable]
    public class Message{
        public commands cmd;
    }
    
    [Serializable]
    public class Player{
        [Serializable]
        public struct receivedColor{
            public float R;
            public float G;
            public float B;
        }
        public string id;
        public receivedColor color;
    }

    [Serializable]
    public class NewPlayer{
        public Player player;
    }


    [Serializable]
    public class GameState{
        public Player[] players;
    }

    public Message latestMessage;
    public NewPlayer NPlayer;
    public GameState lastestGameState;
    public GameState connectedPlayers;
    public Player DCPlayer;
    void OnReceived(IAsyncResult result){
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;
        
        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);
        
        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        Debug.Log("Got this: " + returnData);
        
        latestMessage = JsonUtility.FromJson<Message>(returnData);
        try{
            switch(latestMessage.cmd){
                case commands.NEW_CLIENT:
                    NPlayer = JsonUtility.FromJson<NewPlayer>(returnData);
                    NPlayersCount++;
                    break;
                case commands.UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;
                case commands.DISCONNECT:
                    DCPlayer = JsonUtility.FromJson<Player>(returnData);
                    break;
                case commands.OLD_CLIENT:
                    connectedPlayers = JsonUtility.FromJson<GameState>(returnData);
                    break;
                default:
                    Debug.Log("Error");
                    break;
            }
        }
        catch (Exception e){
            Debug.Log(e.ToString());
        }
        
        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }


    void SpawnPlayers()
    {

        if (NPlayersCount > 0)
        {
            int RandomPoint = UnityEngine.Random.Range(0, SpawnPoints.Length);
            GameObject playerObj = Instantiate(PlayerPrefab, SpawnPoints[RandomPoint].transform.position, Quaternion.identity);
            playerObj.GetComponent<NetInfo>().ID = NPlayer.player.id;
            AllPlayers.Add(playerObj);
            NPlayersCount--;
        }
        else
        {
            if (connectedPlayers.players.Length > AllPlayers.Count)
            {
                foreach (Player p in connectedPlayers.players)
                {
                    int RandomPoint = UnityEngine.Random.Range(0, SpawnPoints.Length);
                    GameObject playerObj = Instantiate(PlayerPrefab, SpawnPoints[RandomPoint].transform.position, Quaternion.identity);
                    playerObj.GetComponent<NetInfo>().ID = p.id;
                    AllPlayers.Add(playerObj);
                }
            }
        }

        //if (lastestGameState.players.Length > AllPlayers.Count)
        //{
        //    foreach (Player p in lastestGameState.players)
        //    {
        //        if (p.id != ThisPlayer.id)
        //        {
        //            int RandomPoint = UnityEngine.Random.Range(0, SpawnPoints.Length);
        //            GameObject playerObj = Instantiate(PlayerPrefab, SpawnPoints[RandomPoint].transform.position, Quaternion.identity);
        //            playerObj.GetComponent<NetInfo>().ID = p.id;
        //            AllPlayers.Add(playerObj);
        //            ThisPlayer = p;
        //        }
        //    }
        //}
        //else
        //{
        //    if (NPlayer != null)
        //    {
        //        Player p = NPlayer.player;
        //        if (p.id.Length > 0 && ThisPlayer != p)
        //        {
        //            Debug.Log("Net: " + p.id + " New: " + NPlayer.player.id);
        //            int RandomPoint = UnityEngine.Random.Range(0, SpawnPoints.Length);
        //            GameObject playerObj = Instantiate(PlayerPrefab, SpawnPoints[RandomPoint].transform.position, Quaternion.identity);
        //            playerObj.GetComponent<NetInfo>().ID = p.id;
        //            AllPlayers.Add(playerObj);
        //            ThisPlayer = p;
        //        }
        //    }
        //}

    }

    void UpdatePlayers()
    {
        foreach (Player p in lastestGameState.players)
        {
            foreach (GameObject PO in AllPlayers)
            {
                //Debug.Log(p.id + " and " + PO.GetComponent<NetInfo>().ID);
                if (p.id.Equals(PO.GetComponent<NetInfo>().ID))
                {
                    PO.GetComponent<NetInfo>().UpdateMat(new Color(p.color.R, p.color.G, p.color.B));
                }
            }
        }
    }

    void DestroyPlayers(){

    }
    
    void HeartBeat(){
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update(){
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}
