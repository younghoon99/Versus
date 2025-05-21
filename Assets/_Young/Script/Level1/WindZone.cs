using UnityEngine;
using System.Collections.Generic;
using Photon.Pun; // [네트워크 동기화 추가]

// 바람이 부는 영역을 제어하는 스크립트
// [네트워크 동기화 추가]
public class WindZone : MonoBehaviourPun
{
    // 레버(버튼) 오브젝트 참조
    [SerializeField] private AN_Button an_button;
    // 바람 영역 오브젝트
    [SerializeField] private GameObject windzone;
    // 바람이 플레이어에게 미치는 힘의 세기
    [SerializeField] private float windForce = 2000f;

    // 바람 영역에 들어온 플레이어의 Rigidbody 목록
    private List<Rigidbody> playersInZone = new List<Rigidbody>();
    
    // 바람 효과 활성화 여부
    private bool isWindActive = false;

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
        // 프레임마다 레버 상태를 확인하여 바람 효과 활성화 여부를 갱신
        UpdateWindState();
        
        // 바람 효과가 활성화된 경우, 영역 내 플레이어들에게 힘을 가함
        if (isWindActive)
        {
            ApplyWindForce();
        }
    }

    // [네트워크 동기화 추가] 레버 상태 변경시 RPC로 동기화
    public void SetLeverState(bool leverUp)
    {
        photonView.RPC("SetLeverStateRPC", RpcTarget.AllBuffered, leverUp);
    }

    // [네트워크 동기화 추가] 모든 클라이언트에서 상태 동기화
    [PunRPC]
    public void SetLeverStateRPC(bool leverUp)
    {
        isWindActive = leverUp;
    }

    // 기존 UpdateWindState는 내부적으로만 사용(직접 레버 상태 갱신 X)
    private void UpdateWindState()
    {
        if (an_button != null)
        {
            isWindActive = an_button.isLeverUp;
        }
        else
        {
            isWindActive = false;
        }
    }
    
    // [네트워크 동기화 추가] 내 플레이어만 바람 힘 적용
    private void ApplyWindForce()
    {
        List<Rigidbody> tempList = new List<Rigidbody>(playersInZone);
        foreach (Rigidbody rb in tempList)
        {
            var pv = rb.GetComponent<PhotonView>();
            // [네트워크 동기화] 내 플레이어만 AddForce 적용
            if (rb != null && pv != null && pv.IsMine)
            {
                rb.AddForce(Vector3.right * windForce);
            }
            else if (rb == null)
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