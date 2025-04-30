using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Level3 : MonoBehaviourPunCallbacks
{
    [Header("좌측 6개 벽 (Inspector에서 할당)")]
    public List<GameObject> leftWalls; // 좌측 벽 리스트
    [Header("우측 6개 벽 (Inspector에서 할당)")]
    public List<GameObject> rightWalls; // 우측 벽 리스트

    private List<bool> leftIsStatic = new List<bool>();
    private List<bool> rightIsStatic = new List<bool>();

    public string ballPrefabPath;
    public int ballPoolSize = 20;
    public float ballSpawnInterval = 2f;
    public float ballLifeTime = 10f;
    public Color ballGizmoColor = Color.red;

    private List<GameObject> ballPool = new List<GameObject>();
    private List<GameObject> activeBalls = new List<GameObject>();
    private Transform ballPoolParent;
    private Coroutine ballSpawnCoroutine;

    private void Awake()
    {
        InitializeBallPool(); // 공 풀 초기화
    }

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            SetRandomStaticWalls(leftWalls, leftIsStatic);
            SetRandomStaticWalls(rightWalls, rightIsStatic);
            SyncWalls(); // 벽 동기화
        }

        if (ballSpawnCoroutine != null) StopCoroutine(ballSpawnCoroutine);
        ballSpawnCoroutine = StartCoroutine(SpawnBallsRoutine()); // 공 생성 시작
    }

    private IEnumerator SpawnBallsRoutine()
    {
        if (!PhotonNetwork.IsMasterClient) yield break;

        while (true)
        {
            SpawnBall(true); // z 값 양수
            SpawnBall(false); // z 값 음수
            yield return new WaitForSeconds(ballSpawnInterval); // 공 간격
        }
    }

    private void SetRandomStaticWalls(List<GameObject> walls, List<bool> isStaticList)
    {
        isStaticList.Clear();
        if (walls == null || walls.Count == 0) return;
        int dynamicIdx = Random.Range(0, walls.Count);
        for (int i = 0; i < walls.Count; i++)
        {
            bool isStatic = (i != dynamicIdx);
            isStaticList.Add(isStatic);
            ConfigureRigidbody(walls[i], isStatic);
        }
    }

    private void SyncWalls()
    {
        photonView.RPC("SyncWalls", RpcTarget.Others, leftIsStatic.ToArray(), rightIsStatic.ToArray());
    }

    [PunRPC]
    private void SyncWalls(bool[] leftStaticArray, bool[] rightStaticArray)
    {
        for (int i = 0; i < leftWalls.Count; i++)
        {
            bool isStatic = (i < leftStaticArray.Length) ? leftStaticArray[i] : true;
            ConfigureRigidbody(leftWalls[i], isStatic);
        }

        for (int i = 0; i < rightWalls.Count; i++)
        {
            bool isStatic = (i < rightStaticArray.Length) ? rightStaticArray[i] : true;
            ConfigureRigidbody(rightWalls[i], isStatic);
        }
    }

    private void ConfigureRigidbody(GameObject obj, bool isStatic)
    {
        Rigidbody rb = obj.GetComponent<Rigidbody>() ?? obj.AddComponent<Rigidbody>();
        if (isStatic)
        {
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
        else
        {
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.None;
        }
    }

    public override void OnDisable()
    {
        if (ballSpawnCoroutine != null) StopCoroutine(ballSpawnCoroutine);
        base.OnDisable();
    }

    private void InitializeBallPool()
    {
        if (!PhotonNetwork.IsMasterClient || string.IsNullOrEmpty(ballPrefabPath)) return;

        GameObject poolParent = new GameObject("Ball_Pool");
        poolParent.transform.SetParent(transform);
        ballPoolParent = poolParent.transform;

        for (int i = 0; i < ballPoolSize; i++)
        {
            GameObject ball = PhotonNetwork.Instantiate(ballPrefabPath, Vector3.zero, Quaternion.identity);
            ball.name = "Ball_" + i;
            ball.transform.SetParent(ballPoolParent);
            ball.SetActive(false);
            ballPool.Add(ball);
        }
    }

    private GameObject GetBallFromPool()
    {
        foreach (GameObject ball in ballPool)
            if (!ball.activeSelf) return ball;
        return null;
    }

    public GameObject SpawnBall(bool positiveZ)
    {
        if (!PhotonNetwork.IsMasterClient) return null;

        GameObject ball = GetBallFromPool();
        if (ball != null)
        {
            float z = Random.Range(6f, 20f);
            if (!positiveZ) z = -z;
            ball.transform.position = new Vector3(-121.43f, 36.5f, z);

            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            ball.SetActive(true);
            activeBalls.Add(ball);

            // 모든 클라이언트에 공 활성화 알리기
            photonView.RPC("ActivateBallRPC", RpcTarget.Others, ball.name, ball.transform.position);

            StartCoroutine(DeactivateBallAfterDelay(ball, ballLifeTime));
        }

        return ball;
    }

    [PunRPC]
    private void ActivateBallRPC(string ballName, Vector3 spawnPosition)
    {
        GameObject ball = ballPool.Find(b => b.name == ballName);
        if (ball != null)
        {
            ball.transform.position = spawnPosition;

            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            ball.SetActive(true);
        }
    }

    private IEnumerator DeactivateBallAfterDelay(GameObject ball, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (ball != null && ball.activeSelf)
        {
            // 모든 클라이언트에 공 비활성화 알리기
            photonView.RPC("DeactivateBallRPC", RpcTarget.Others, ball.name);

            ball.SetActive(false);
            activeBalls.Remove(ball);
        }
    }

    [PunRPC]
    private void DeactivateBallRPC(string ballName)
    {
        GameObject ball = ballPool.Find(b => b.name == ballName);
        if (ball != null && ball.activeSelf)
        {
            ball.SetActive(false);
            activeBalls.Remove(ball);
        }
    }
}
