using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class SimulationTest : MonoBehaviour
{

    private Channel channel;

    private float accum = 0f;
    private float clientTime = 0f;
    public int pps = 10;
    public int requiredSnapshots = 3;
    private int packetNumber = 0;
    private bool clientPlaying = false;

    private bool connected = true;

    [SerializeField] private GameObject cubeServer;
    [SerializeField] private GameObject cubeClient;

    List<Snapshot> interpolationBuffer = new List<Snapshot>();

    // Start is called before the first frame update
    void Start() {
        channel = new Channel(9000);
    }

    private void OnDestroy() {
        channel.Disconnect();
    }

    // Update is called once per frame
    void Update() {
        accum += Time.deltaTime;
        //apply input
        if (Input.GetKeyDown(KeyCode.Space)) {
            cubeServer.GetComponent<Rigidbody>().AddForceAtPosition(Vector3.up * 5, Vector3.zero, ForceMode.Impulse);
        }
        if (Input.GetKeyDown(KeyCode.D)) {
            connected = !connected;
        }

        if(connected) {
          UpdateServer();
        }
        UpdateClient();
    }

    private void UpdateClient() {
        var packet = channel.GetPacket();

        if (packet != null) {
            var cubeEntity = new CubeEntity(cubeClient);
            var snapshot = new Snapshot(-1, cubeEntity);
            var buffer = packet.buffer;

            snapshot.Deserialize(buffer);

            int size = interpolationBuffer.Count;
            if(size == 0 || snapshot.packetNumber > interpolationBuffer[size - 1].packetNumber) {
                interpolationBuffer.Add(snapshot);
            }
        }

        if (interpolationBuffer.Count >= requiredSnapshots) {
            clientPlaying = true;
        }
        else if (interpolationBuffer.Count <= 1) {
            clientPlaying = false;
        }
        if (clientPlaying) {
            clientTime += Time.deltaTime;
            Interpolate();
        }
    }

    private void UpdateServer() {
        // If we want to send 10 pckts persecond we need a sendRate of 1/10
        float sendRate = (1f/pps);
        if (accum >= sendRate) {
            packetNumber += 1;
            //serialize
            var packet = Packet.Obtain();
            var cubeEntity = new CubeEntity(cubeServer);
            var snapshot = new Snapshot(packetNumber, cubeEntity);
            snapshot.Serialize(packet.buffer);
            packet.buffer.Flush();

            string serverIP = "127.0.0.1";
            int port = 9000;
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
            channel.Send(packet, remoteEp);
            packet.Free();
            // Restart accum
            accum -= sendRate;
        }
    }

    private void Interpolate() {
        var previousTime = (interpolationBuffer[0]).packetNumber * (1f/pps);
        var nextTime =  interpolationBuffer[1].packetNumber * (1f/pps);
        var t =  (clientTime - previousTime) / (nextTime - previousTime); 
        var interpolatedSnapshot = Snapshot.CreateInterpolated(interpolationBuffer[0], interpolationBuffer[1], t);
        interpolatedSnapshot.Apply();

        if(clientTime > nextTime) {
            interpolationBuffer.RemoveAt(0);
        }
    }
}