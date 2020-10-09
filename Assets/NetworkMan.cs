using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using JetBrains.Annotations;

public class NetworkMan : MonoBehaviour
{
    List<GameObject> AllPlayers = new List<GameObject>();
    Player ThisPlayer;
    bool Joined = false;

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

        InvokeRepeating("HeartBeat", 1, 0.0333f);
        InvokeRepeating("UpdatePosition", 2, 1.0333f);
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
            public float x;
            public float y;
            public float z;
        }
        public string id;
        public receivedColor color;
        public Vector3 location;
    }

    [Serializable]
    public class NewPlayer{
        public Player player;
    }

    [Serializable]
    public class DCPlayer
    {
        public Player player;
    }

    [Serializable]
    public class GameState{
        public Player[] players;
    }

    [Serializable]
    public class EveryPlayer
    {
        public Player[] players;
    }

    public Message latestMessage;
    public NewPlayer NPlayer;
    public GameState lastestGameState;
    public EveryPlayer connectedPlayers;
    public DCPlayer DCP;
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
                    Joined = true;
                    break;
                case commands.UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;
                case commands.DISCONNECT:
                    DCP = JsonUtility.FromJson<DCPlayer>(returnData);
                    break;
                case commands.OLD_CLIENT:
                    connectedPlayers = JsonUtility.FromJson<EveryPlayer>(returnData);
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

    bool PlayerInGame(Player player)
    {
        foreach (GameObject p in AllPlayers)
        {
            if (p.GetComponent<NetInfo>().ID == player.id)
            {
                return true;
            }
        }
        return false;
    }

    GameObject FindPlayerInGame(Player player)
    {
        foreach (GameObject p in AllPlayers)
        {
            if (p.GetComponent<NetInfo>().ID == player.id)
            {
                return p;
            }
        }
        return null;
    }

    void SpawnPlayers()
    {
        if (Joined)
        {
            if (!PlayerInGame(NPlayer.player))
            {
                int RandomPoint = UnityEngine.Random.Range(0, SpawnPoints.Length);
                Vector3 RandomDeltaPositon = new Vector3(UnityEngine.Random.Range(.2f, .5f), 0, UnityEngine.Random.Range(.2f, .5f));
                GameObject playerObj = Instantiate(PlayerPrefab, SpawnPoints[RandomPoint].transform.position + RandomDeltaPositon, Quaternion.identity);
                playerObj.GetComponent<NetInfo>().ID = NPlayer.player.id;
                AllPlayers.Add(playerObj);
                ThisPlayer = NPlayer.player;
                Joined = false;
            }
        }

        foreach (Player p in connectedPlayers.players)
        {
            if(!PlayerInGame(p))
            {
                int RandomPoint = UnityEngine.Random.Range(0, SpawnPoints.Length);
                Vector3 RandomDeltaPositon = new Vector3(UnityEngine.Random.Range(.2f, .5f), 0, UnityEngine.Random.Range(.2f, .5f));
                GameObject playerObj = Instantiate(PlayerPrefab, SpawnPoints[RandomPoint].transform.position + RandomDeltaPositon, Quaternion.identity);
                playerObj.GetComponent<NetInfo>().ID = p.id;
                AllPlayers.Add(playerObj);
            }
        }

    }

    void UpdatePlayers()
    {

    }

    void UpdatePosition()
    {
        foreach (Player p in lastestGameState.players)
        {
            foreach (GameObject PO in AllPlayers)
            {
                //Debug.Log(p.id + " and " + PO.GetComponent<NetInfo>().ID);
                if (p.id.Equals(PO.GetComponent<NetInfo>().ID))
                {
                    PO.transform.position = p.location;
                    //Debug.Log(p.location);
                }
            }
        }
    }

    void DestroyPlayers()
    {
        if (PlayerInGame(DCP.player))
        {
            GameObject foundPlayer = FindPlayerInGame(DCP.player);
            AllPlayers.Remove(foundPlayer);
            Destroy(foundPlayer);
        }
    }

    void HeartBeat()
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);


        if (FindPlayerInGame(ThisPlayer) != null)
        {
            Byte[] locationSend = Encoding.ASCII.GetBytes(JsonUtility.ToJson(FindPlayerInGame(ThisPlayer).transform.position));
            udp.Send(locationSend, locationSend.Length);
        }

    }

    void Update(){
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}
