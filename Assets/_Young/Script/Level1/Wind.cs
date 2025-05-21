using UnityEngine;
using System.Collections.Generic;
using Photon.Pun; // [네트워크 동기화 추가]

// 바람이 부는 영역을 제어하는 스크립트
// [네트워크 동기화 추가]
public class Wind : MonoBehaviourPun
{
   
    // 바람 영역 오브젝트
    [SerializeField] private GameObject windzone;
    // 바람이 플레이어에게 미치는 힘의 세기
    [SerializeField] private float windForce = 2000f;

    // 인스펙터에서 바람 방향을 조절할 수 있도록 public 변수로 선언
    [Header("바람 방향 (예: (1,0,0) = 오른쪽, (0,0,1) = 앞, (-1,0,0) = 왼쪽 등)")]
    public Vector3 windDirection = Vector3.right;

    // 바람 영역에 들어온 플레이어의 Rigidbody 목록
    private List<Rigidbody> playersInZone = new List<Rigidbody>();

    // 바람 효과 활성화 여부
    private bool isWindActive = true;

    // 게임 시작 시 초기화
    private void Start()
    {
        // 게임 시작 시 windzone 오브젝트를 항상 활성화 상태로 설정
        if (windzone != null)
        {
            windzone.SetActive(true);
        }
    }

    // 매 프레임마다 호출되는 함수
    private void Update()
    {
        // 바람 효과가 활성화된 경우, 영역 내 플레이어들에게 힘을 가함
        if (isWindActive)
        {
            ApplyWindForce();
        }
    }





    // [네트워크 동기화 추가] 내 플레이어만 바람 힘 적용
    private void ApplyWindForce()
    {
        // WindZone.cs 참고: 내 플레이어(PhotonView.IsMine)만 바람 효과 적용
        List<Rigidbody> tempList = new List<Rigidbody>(playersInZone);
        foreach (Rigidbody rb in tempList)
        {
            if (rb != null)
            {
                PhotonView pv = rb.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    // windDirection을 인스펙터에서 조절 가능, 항상 정규화하여 사용
                    rb.AddForce(windDirection.normalized * windForce);
                    Debug.Log($"바람 적용: {windDirection.normalized * windForce}");
                }
            }
            else
            {
                playersInZone.Remove(rb);
            }
        }
    }

    // 플레이어가 바람 영역에 진입했을 때 호출되는 함수
    private void OnTriggerEnter(Collider other)
    {
        // "Player" 태그를 가진 오브젝트만 처리
        if (other.CompareTag("Player"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null && !playersInZone.Contains(rb))
            {
                // 플레이어의 Rigidbody를 목록에 추가
                playersInZone.Add(rb);

                // 디버그용 로그 출력
                Debug.Log("플레이어가 바람 영역에 들어왔습니다.");
            }
        }
    }

    // 플레이어가 바람 영역에서 나갔을 때 호출되는 함수
    private void OnTriggerExit(Collider other)
    {
        // "Player" 태그를 가진 오브젝트만 처리
        if (other.CompareTag("Player"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // 플레이어의 Rigidbody를 목록에서 제거
                playersInZone.Remove(rb);

                // 디버그용 로그 출력
                Debug.Log("플레이어가 바람 영역에서 나갔습니다.");
            }
        }
    }

    // WindZone 스크립트가 비활성화될 때 플레이어 목록을 초기화하여 메모리 누수 방지
    private void OnDisable()
    {
        playersInZone.Clear();
    }
}