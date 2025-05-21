using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;

public class Ending : MonoBehaviourPun
{
    [Header("엔딩 카운트다운 UI")]
    public TMPro.TextMeshProUGUI endingCountdownText; // Inspector에서 연결

    public static Ending Instance;

    [Header("시상식 위치")]
    public Transform podium1; // 1등 위치
    public Transform podium2; // 2등 위치
    public Transform ceremonyLookTarget; // 시상식 카메라가 바라볼 위치(카메라 타겟)

    private List<int> arrivedPlayerIds = new List<int>(); // 도착한 플레이어 ID 목록
    private bool isCountdownStarted = false;
    private float countdownTime = 10f;
    private Coroutine countdownCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // FinishLine에 플레이어가 도착했을 때 호출
    public void PlayerArrived(int playerId)
    {
        if (!PhotonNetwork.IsMasterClient) return; // 마스터 클라이언트만 처리

        // 이미 도착한 플레이어라면 무시 (한 번만 작동)
        if (arrivedPlayerIds.Contains(playerId))
            return;
        arrivedPlayerIds.Add(playerId);

        if (!isCountdownStarted)
        {
            isCountdownStarted = true;
            countdownCoroutine = StartCoroutine(CountdownRoutine());
        }
        // 도착 시점에도 UI를 모든 플레이어에게 동기화
        SyncEndingCountdownUI(countdownTime);

        // 모든 플레이어가 도착했으면 즉시 시상식 이동
        if (arrivedPlayerIds.Count >= PhotonNetwork.CurrentRoom.PlayerCount)
        {
            if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
            CeremonyMoveAllPlayers();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<Player>();
        if (player != null && Ending.Instance != null)
        {
            var pv = player.GetComponent<PhotonView>();
            if (pv != null)
                Ending.Instance.PlayerArrived(pv.OwnerActorNr);
        }
    }
    // 시상식 진입 시 모든 플레이어에게 박수 소리 재생 명령
    void PlayCeremonyClapSound()
    {
        Vector3 ceremonyPos = podium1.position;
        foreach (var player in FindObjectsOfType<Player>())
        {
            var pv = player.GetComponent<PhotonView>();
            if (pv != null)
                pv.RPC("RpcPlayCeremonySound", RpcTarget.All, ceremonyPos);
        }
    }

    // 10초 카운트다운 코루틴
    private IEnumerator CountdownRoutine()
    {
        float t = countdownTime;
        while (t > 0f)
        {
            // 모든 플레이어에게 UI 정보 동기화
            SyncEndingCountdownUI(t);
            yield return new WaitForSeconds(1f);
            t -= 1f;
        }
        SyncEndingCountdownUI(0);
        CeremonyMoveAllPlayers();
    }

    // 모든 클라이언트에 UI 갱신 RPC 호출 (마스터에서만)
    private void SyncEndingCountdownUI(float remain)
    {
        // Null 체크 및 상세 로그
        if (arrivedPlayerIds == null)
        {
            Debug.LogError("[Ending] arrivedPlayerIds가 null입니다!");
            return;
        }
        if (photonView == null)
        {
            Debug.LogError("[Ending] photonView가 null입니다!");
            return;
        }
        if (PhotonNetwork.CurrentRoom == null)
        {
            Debug.LogError("[Ending] PhotonNetwork.CurrentRoom이 null입니다!");
            return;
        }
        int arrived = arrivedPlayerIds.Count;
        int total = PhotonNetwork.CurrentRoom.PlayerCount;
        photonView.RPC(nameof(RpcUpdateEndingCountdownUI), RpcTarget.All, remain, arrived, total);
    }

    // RPC 함수: 모든 클라이언트에서 UI 갱신
    [PunRPC]
    private void RpcUpdateEndingCountdownUI(float remain, int arrived, int total)
    {
        if (endingCountdownText != null)
        {
            if (remain <= 0f)
            {
                endingCountdownText.gameObject.SetActive(false); // 0초가 되면 UI 숨김
            }
            else
            {
                endingCountdownText.gameObject.SetActive(true); // 카운트다운 중에는 항상 보임
                endingCountdownText.text = $"{remain:0}초";
            }
        }
        else
        {
            Debug.LogError("[Ending] endingCountdownText가 null입니다! Inspector에서 연결 확인 필요");
        }
    }


    // 모든 플레이어를 시상식 위치로 이동시키는 RPC
    // 모든 플레이어를 시상식 위치(등수별 podium)에 배치하고, 5초간 시상식 모드 진입 명령
    // 플레이어가 2명일 때 podium3(3등)은 항상 비워둡니다.
    // 1등만 들어와도 10초 후 나머지 한 명을 podium2(2등)로 강제 배치합니다.
    private void CeremonyMoveAllPlayers()
    {
        List<Player> allPlayers = new List<Player>(FindObjectsOfType<Player>());
        int totalPlayers = allPlayers.Count;

        // 1. 도착한 플레이어 podium1, podium2에만 배치, podium3는 항상 비움
        for (int i = 0; i < arrivedPlayerIds.Count && i < 2; i++)
        {
            var player = allPlayers.Find(p => p.GetComponent<Photon.Pun.PhotonView>().OwnerActorNr == arrivedPlayerIds[i]);
            if (player != null)
            {
                Transform podium = (i == 0) ? podium1 : podium2;
                if (podium != null)
                {
                    player.GetComponent<Photon.Pun.PhotonView>().RPC("RpcMoveToPodium", Photon.Pun.RpcTarget.All, podium.position, podium.rotation);
                }
                string emotionName = (i == 0) ? "Emotion3" : "Emotion2";
                player.GetComponent<Photon.Pun.PhotonView>().RPC("RpcEnterCeremonyMode", Photon.Pun.RpcTarget.All, ceremonyLookTarget.position, 5f, emotionName);
            }
        }

        // 2. 아직 도착하지 않은 플레이어가 있으면 2등(podium2)로 강제 배치
        if (arrivedPlayerIds.Count == 1 && totalPlayers == 2)
        {
            var player = allPlayers.Find(p => !arrivedPlayerIds.Contains(p.GetComponent<Photon.Pun.PhotonView>().OwnerActorNr));
            if (player != null)
            {
                player.GetComponent<Photon.Pun.PhotonView>().RPC("RpcMoveToPodium", Photon.Pun.RpcTarget.All, podium2.position, podium2.rotation);
                player.GetComponent<Photon.Pun.PhotonView>().RPC("RpcEnterCeremonyMode", Photon.Pun.RpcTarget.All, ceremonyLookTarget.position, 5f, "Emotion2");
            }
        }
    }

    [PunRPC]
    private void RpcMoveToCeremony(Vector3 pos)
    {
        // 모든 Player 오브젝트를 ceremonyPosition으로 이동
        foreach (var player in FindObjectsOfType<Player>())
        {
            player.transform.position = pos;
            if (player.rb != null) player.rb.velocity = Vector3.zero; // 이동 후 속도 제거
        }
    }
}

