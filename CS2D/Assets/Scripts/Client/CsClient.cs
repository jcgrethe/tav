using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static AnimatorStates;
using static SendUtil;
using static ExecuteCommand;
using static MessageCsType;
using Random = UnityEngine.Random;

public class CsClient : MonoBehaviour
{

    private Channel channel;
    private AudioSource audioSource;
    private float clientTime = 0f;
    public int pps = 100;
    public int requiredSnapshots = 3;
    private int packetNumber = 0;
    private int serverPort = 9000;
    public GameObject ClientPrefab;
    private GameObject client;
    public Material material;
    private GameObject conciliateGameObject;
    private Dictionary<String, GameObject> clients;
    private CharacterController characterController;
    private CharacterController conciliateCharacterController;
    List<Snapshot> interpolationBuffer = new List<Snapshot>();
    List<Command> commandServer = new List<Command>();
    private bool join = false;
    private bool waitJoin = true;
    public Transform cameraHolder;
    private float mouseSensitivity = 6f;
    public float upLimit = -50;
    public float downLimit = 50;
    public String serverIP;
    private GameObject mainCamera;
    public GameObject cameraPrefab;
    private bool shooting;
    private bool crouch;
    private int life = 100;
    private bool isDead = false;
    private Animator animator;
    private bool startInterp = false;

    public float coolDown = .3f;
    private float shootingCoolDown = 0;
    private bool onShootingCoolDown = false;
    
    public float reloadCoolDown = 2f;
    private float accumReloadingCoolDown = 0;
    private bool onReloadingCoolDown = false;
    public int initialAmmo = 1;
    private int bullets = 30;
    private GameManager gameManager;
    public InGameUi inGameUi;
    
    // Start is called before the first frame update
    void Start()
    {
        var gameManagerObject = GameObject.FindGameObjectWithTag("GameManager");
        if (gameManagerObject != null)
        {
            gameManager = gameManagerObject.GetComponent<GameManager>();
            serverIP = gameManager.ip;
        }
        audioSource = GetComponent<AudioSource>();
        JoinPlayer();
    }

    private void OnDestroy() {
        channel.Disconnect();
    }

    public void Awake()
    {
        channel = new Channel(9001);

        clients = new Dictionary<string, GameObject>();
    }

    // Update is called once per frame

    private void JoinPlayer()
    {
        client = Instantiate(ClientPrefab, new Vector3(343.2f, 1209.8f, 650 ), Quaternion.identity);
        var id = RandomId();
        client.name = id;
        client.GetComponent<PlayerId>().Id = id;
        clients.Add(id, client);
        //client.GetComponent<MeshRenderer>().material = material;
        characterController = client.GetComponent<CharacterController>();
        animator = client.GetComponent<Animator>();
        
        GameObject main = Instantiate(cameraPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        main.transform.parent = client.transform;
        main.transform.localPosition = new Vector3(1.077f, 1.481f, -2.427f);
        cameraHolder = main.GetComponent<Camera>().transform;
        inGameUi.setCamera(cameraHolder.GetComponent<Camera>());
        //mainCamera = main;
        var packet4 = Packet.Obtain();
        packet4.buffer.PutEnum(messagetype.newPlayer, quantityOfMessages);
        packet4.buffer.PutString(id);
        var player = new PlayerEntity(client, id);
        player.Serialize(packet4.buffer);
        packet4.buffer.Flush();

        Send(serverIP, serverPort, channel, packet4);
        
        conciliateGameObject = Instantiate(ClientPrefab, new Vector3(0, 0f, 0), Quaternion.identity);
        conciliateGameObject.name = id;
        conciliateGameObject.GetComponent<PlayerId>().Id = id;
        conciliateCharacterController = conciliateGameObject.GetComponent<CharacterController>();
        Destroy(conciliateGameObject.GetComponent<Animator>());
        conciliateCharacterController.transform.GetChild(1).gameObject.active = false;
        conciliateCharacterController.transform.GetChild(0).gameObject.active = false;


    }


    void Update() 
    {
        if (onShootingCoolDown)
        {
            shootingCoolDown += Time.deltaTime;
            if (shootingCoolDown > coolDown)
            {
                onShootingCoolDown = false;
            }
        }
        
        if (onReloadingCoolDown)
        {
            accumReloadingCoolDown += Time.deltaTime;
            if (accumReloadingCoolDown > reloadCoolDown)
            {
                bullets = initialAmmo;
                inGameUi.setAmmo(bullets.ToString());
                onReloadingCoolDown = false;
                animator.SetBool("isReloading", false);
            }
        }
        
        
        while(commandServer.Count != 0)
        {
            if (commandServer[0].timestamp < Time.time)
            {
                commandServer.RemoveAt(0);
            }
            else
            {
                break;
            }
        }
        UpdateClient();
        InterpolateAndConciliate();
        if (life <= 0  && !isDead)
        {
            Debug.Log("DEAD");
            animator.SetBool("isDead", true);
            isDead = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            shooting = true;
        }
        if(Input.GetMouseButtonUp(0))
        {
            shooting = false;
        }
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            crouch = true;
        }
        if(Input.GetKeyUp(KeyCode.LeftControl))
        {
            crouch = false;
        }
    }

    public void FixedUpdate()
    {
        if (!isDead)
        {
            SendInput();
        }
    }

    private void UpdateClient() 
    {
        Packet packet;
        while ((packet = channel.GetPacket()) != null)
        {
            switch (packet.buffer.GetEnum<messagetype>(quantityOfMessages))
            {
                case messagetype.ackInput:
                    UpdateInterpolationBuffer(packet);
                    break;
                case messagetype.ackJoin:
                    AwaitJoinGame(packet);
                    break;
                case messagetype.updateWorld:
                    UpdateWord(packet);
                    break;
            }
        }
    }

    private void SendInput()
    {
        if(!join) return;
        ReadInput();
        if (commandServer.Count != 0)
        {
            var packet2 = Packet.Obtain();
            packet2.buffer.PutEnum(messagetype.input, quantityOfMessages);
            packet2.buffer.PutString(client.name);
            packet2.buffer.PutUInt(commandServer.Count);
            foreach (var currentCommand in commandServer)
            {
                currentCommand.Serialize(packet2.buffer);
            }
            packet2.buffer.Flush();

            Send(serverIP, serverPort, channel, packet2);
        }
        
    }
    
    private void AwaitJoinGame(Packet packet)
    {
        var quan = packet.buffer.GetBits(0, 50);
        for (int i = 0; i < quan; i++)
        {
            var enemyClient = Instantiate(ClientPrefab, new Vector3(3, 0.5f, 0), Quaternion.identity);
            enemyClient.name =  packet.buffer.GetString();
            enemyClient.tag = "Enemy";
            enemyClient.GetComponent<PlayerId>().Id = enemyClient.name;
            //enemyClient.GetComponent<MeshRenderer>().material = material;
            clients.Add(enemyClient.name, enemyClient);  
        }

        join = true;
        
    }

    private void UpdateInterpolationBuffer(Packet packet)
    {
        var toDel = packet.buffer.GetUInt();
        while (commandServer.Count != 0)
        {
            if (commandServer[0].commandNumber <= toDel)
            {
                commandServer.RemoveAt(0);
            }
            else
            {
                break;
            }
        }
    }


    private void UpdateWord(Packet packet)
    {

        var buffer = packet.buffer;
        var snapshot = new Snapshot(-1);
        snapshot.Deserialize(buffer);
        if(snapshot.life != 0  && isDead) isDead = false; 
        life = snapshot.life;
        inGameUi.setLife(life.ToString());
        inGameUi.setKills(snapshot.kills.ToString());
        //Debug.Log(snapshot.life);
        int size = interpolationBuffer.Count;
        if((size == 0 || snapshot.packetNumber > interpolationBuffer[size - 1].packetNumber) && size < requiredSnapshots + 1 ) {
            interpolationBuffer.Add(snapshot);
            startInterp = true;
        }
    }

    private void InterpolateAndConciliate()
    {
        if(interpolationBuffer.Count >= requiredSnapshots) clientTime += Time.deltaTime;
        Conciliate();
        while ( Interpolate())
        {
        }


        

    }
    
    private bool Interpolate() 
    {
        if (!join) return false;
        if (interpolationBuffer.Count < requiredSnapshots) return false;
        var previousTime = ((interpolationBuffer[0]).packetNumber) * (1f/(pps));
        var nextTime =  (interpolationBuffer[1].packetNumber) * (1f/(pps));
        var t =  (clientTime - previousTime) / (nextTime - previousTime); 
        var interpolatedSnapshot = Snapshot.CreateInterpolated(interpolationBuffer[0], interpolationBuffer[1], t, clients, client.name);
        interpolatedSnapshot.Apply();
        
        //Debug.Log("PREV TIME" + previousTime);
        //Debug.Log("CLIENT TIME" + clientTime);
        //Debug.Log("NEXt TIME" + nextTime);

        if (clientTime > nextTime) {
            interpolationBuffer.RemoveAt(0);
            return true;
        }
        else
        {
            //Debug.Log("INTERP");
            //Debug.Log("BUFFER" + interpolationBuffer.Count);
        }

        return false;
    }

    private void Conciliate()
    {
        if(interpolationBuffer.Count < 1) return;
        var auxClient = interpolationBuffer[interpolationBuffer.Count - 1].playerEntities[client.name];
        conciliateGameObject.transform.position = auxClient.position;
        conciliateGameObject.transform.rotation = auxClient.rotation;
        foreach (var auxCommand in commandServer)
        {
            Execute(auxCommand, conciliateGameObject, conciliateCharacterController);
        }

        var svPos = conciliateGameObject.transform.position;

        var yPos = Math.Abs(svPos.y - client.transform.position.y) > 4 ? svPos.y : client.transform.position.y; 
        var clientPos = new Vector3( svPos.x, yPos, svPos.z);

        client.transform.position = clientPos;
    }

    private void ReadInput()
    {
        var timeout = Time.time + 2;
        Rotate(client, Input.GetAxis("Mouse X"));
        Command command = new Command(packetNumber, Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical"), timeout, 
            Input.GetKey(KeyCode.Space), canShoot(shooting) , crouch, client.transform.rotation );
        if (!onReloadingCoolDown)
        {
            animator.SetBool("isJumping", isJumping(characterController));
            animator.SetBool("shooting", shooting);
            animator.SetBool("crouch", IsCrouch(command));
            animator.SetBool("isWalking", VerticalMovePos(command));
            animator.SetBool("isWalkingBackward", VerticalMoveNeg(command));
            animator.SetBool("isWalkingRight", HorizontalMovePos(command));
            animator.SetBool("isWalkingLeft", HorizontalMoveNeg(command));
        }

        Execute(command, client, characterController);
        LocalCameraRotate();
        if (IsShooting(command))
        {
            audioSource.Play();
            Shoot(command);
        }
        commandServer.Add(command);
        packetNumber++;
        //Debug.Log("CLIENT" + packetNumber );
    }

    private bool canShoot(bool shooting)
    {
        if (shooting && !onShootingCoolDown && !onReloadingCoolDown)
        {
            shootingCoolDown = 0;
            onShootingCoolDown = true;
            bullets--;
            inGameUi.setAmmo(bullets.ToString());
            if (bullets == 0)
            {
                animator.SetBool("isReloading", true);
                animator.SetBool("shooting", false);
                onReloadingCoolDown = true;
                accumReloadingCoolDown = 0;
            }
            return true;
        }

        return false;

    }



    private String RandomId()
    {
        Random.seed = System.DateTime.Now.Millisecond;
        var id = "";
        for(int i=0; i<10; i++)
        {
            id += Random.Range(0, 9).ToString();
        }

        return id;
    }

    private void LocalCameraRotate()
    {
        float verticalRotation = Input.GetAxis("Mouse Y");
        cameraHolder.Rotate(-verticalRotation*mouseSensitivity,0,0);
        Vector3 currentRotation = cameraHolder.localEulerAngles;
        if (currentRotation.x > 180) currentRotation.x -= 360;
        currentRotation.x = Mathf.Clamp(currentRotation.x, upLimit, downLimit);
        cameraHolder.localRotation = Quaternion.Euler(currentRotation);
    }
    private void Shoot(Command command)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] allhHit = Physics.RaycastAll(ray).OrderBy(h=>h.distance).ToArray();
        foreach (var hit in allhHit)
        {
            Debug.Log(hit.transform.name);
            if (string.Compare(hit.transform.gameObject.tag, "Wall", StringComparison.Ordinal) == 0)
            {
                return;
            }
            if (string.Compare(hit.transform.gameObject.tag, "Enemy", StringComparison.Ordinal) == 0)
            {
                var damage = Vector3.Distance(hit.transform.position,hit.point) / 2;
                command.hasHit = true;
                command.damage = new Shoot(hit.transform.gameObject.name, (int) damage);
            }
        }
        
    }
}