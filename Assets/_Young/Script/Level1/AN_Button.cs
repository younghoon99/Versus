using UnityEngine;
using Photon.Pun; // [네트워크 동기화 추가]

// [네트워크 동기화 추가]
public class AN_Button : MonoBehaviourPun
{
    [Header("레버 설정")]
    [Tooltip("플레이어 태그 (기본값: Player)")]
    public string playerTag = "Player";
    
    [Header("오브젝트 활성화 설정")]
    [Tooltip("레버와 상호작용할 때 활성화/비활성화할 오브젝트")]
    public GameObject[] targetObjects;
public GameObject ui;
    [Tooltip("오브젝트를 토글할지 여부 (활성화 상태를 반전)")]
    public bool toggleObjects = true;

    // 레버 상태
    public bool isLeverUp = false;
    // 애니메이터 컴포넌트
    private Animator anim;
    // 플레이어 트리거 내 존재 여부
    private bool playerInTrigger = false;

    void Start()
    {
        // 애니메이터 컴포넌트 가져오기
        anim = GetComponent<Animator>();

        // 시작 시 타겟 오브젝트들의 초기 상태 설정
        if (targetObjects != null && !toggleObjects)
        {
            foreach (GameObject obj in targetObjects)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }
        
        // 콜라이더 확인
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning("콜라이더가 트리거로 설정되어 있지 않습니다. 트리거로 설정하세요.");
        }
        else if (col == null)
        {
            Debug.LogError("이 오브젝트에 콜라이더가 없습니다. 콜라이더를 추가하고 트리거로 설정하세요.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInTrigger = true;
            Debug.Log($"플레이어가 트리거 영역에 들어왔습니다: {other.gameObject.name}");
        }
        
        // UI 활성화
        if (ui != null)
        {
            ui.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInTrigger = false;
            Debug.Log($"플레이어가 트리거 영역에서 나갔습니다: {other.gameObject.name}");
        }
        
        // UI 비활성화
        if (ui != null)
        {
            ui.SetActive(false);
        }        
    }

    void Update()
    {
        // 디버깅 정보 - 매 프레임마다 출력하지 않도록 E 키 입력 시에만 로그 출력
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log($"E 키 입력 감지됨! 플레이어 트리거 영역 내: {playerInTrigger}");
        }
        
        // E 키를 눌렀고 플레이어가 트리거 영역 내에 있는지 확인
        if (Input.GetKeyDown(KeyCode.E) && playerInTrigger)
        {
            Debug.Log("조건 충족! 레버 상호작용 시작");
            // [네트워크 동기화 추가] 모든 클라이언트에 레버 상호작용 동기화
            photonView.RPC("InteractWithLeverRPC", RpcTarget.AllBuffered);
        }
    }

    // [네트워크 동기화 추가] RPC로 호출되는 레버 상호작용 함수
    [PunRPC]
    void InteractWithLeverRPC()
    {
        // 레버 상태 전환
        isLeverUp = !isLeverUp;
        Debug.Log("레버 상태 전환");

        // 애니메이션 실행 (애니메이터에 "LeverUp" 파라미터가 있어야 함)
        if (anim != null)
        {
            anim.SetBool("LeverUp", isLeverUp);
            Debug.Log("애니메이션 실행");
        }
        
        // 타겟 오브젝트 상태 조작
        ToggleTargetObjects();
    }

    // [기존 함수] 단일 플레이어용(더 이상 직접 호출하지 않음)
    void InteractWithLever()
    {
        // 레버 상태 전환
        isLeverUp = !isLeverUp;
        Debug.Log("레버 상태 전환");

        // 애니메이션 실행 (애니메이터에 "LeverUp" 파라미터가 있어야 함)
        if (anim != null)
        {
            anim.SetBool("LeverUp", isLeverUp);
            Debug.Log("애니메이션 실행");
        }
        
        // 타겟 오브젝트 상태 조작
        ToggleTargetObjects();
    }
    
    // 타겟 오브젝트 상태 전환 함수
    void ToggleTargetObjects()
    {
        if (targetObjects == null || targetObjects.Length == 0)
            return;
            
        foreach (GameObject obj in targetObjects)
        {
            if (obj != null)
            {
                if (toggleObjects)
                {
                    // 현재 상태 반전
                    obj.SetActive(!obj.activeSelf);
                }
                else
                {
                    // 레버 상태에 따라 활성화/비활성화
                    obj.SetActive(isLeverUp);
                }
            }
        }
    }
}
