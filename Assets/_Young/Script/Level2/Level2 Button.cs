using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;

public class Level2Button : MonoBehaviourPun
{
    [Header("레버 설정")]
    [Tooltip("플레이어 태그 (기본값: Player)")]
    public string playerTag = "Player";

    [Header("타겟 오브젝트 설정")]
    [Tooltip("레버와 상호작용할 때 물리 재질을 적용할 오브젝트")]
    public GameObject[] targetObjects;
    public GameObject ui;
    
    [Header("물리 재질 설정")]
    [Tooltip("적용할 물리 재질(Bounce)")]
    public PhysicMaterial bounceMaterial;
    [Tooltip("true: Z축 양수 적용, false: Z축 음수 적용")]
    public bool applyToPositiveZ = true;
    
    [Header("향상된 바운스 설정")]
    [Tooltip("향상된 바운스 효과 적용 여부")]
    public bool useEnhancedBounce = true;
    [Tooltip("바운스 힘 배율 (1.0 = 기본)")]
    public float bounceForceMultiplier = 2.0f;
    [Tooltip("바운스 효과음")]
    public AudioClip bounceSound;
    [Tooltip("바운스 파티클 효과")]
    public GameObject bounceParticle;
    

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
        
        // 물리 재질 확인
        if (bounceMaterial == null)
        {
            Debug.LogWarning("물리 재질(Bounce)이 설정되지 않았습니다. 인스펙터에서 설정해주세요.");
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
        // 플레이어가 트리거 영역 내에 있을 때만 E 키 입력 처리
        if (playerInTrigger && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("E 키 입력 감지됨! 플레이어 트리거 영역 내: 활성화");
            // [네트워크 동기화] 모든 클라이언트에 레버 동작 동기화
            photonView.RPC("InteractWithLeverRPC", RpcTarget.AllBuffered);
        }
    }

    // [네트워크 동기화] 모든 클라이언트에서 실행되는 RPC 함수
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
        
        // 레버가 활성화 상태일 때 물리 재질 적용
        if (isLeverUp)
        {
            // 모든 오브젝트의 물리 재질 초기화
            ResetPhysicMaterials();
            
            // 새로운 물리 재질 적용
            ApplyPhysicMaterialToBouncePlatforms();
            Debug.Log($"물리 재질(Bounce) 적용됨");
        }
        else
        {
            // 레버가 비활성화되면 모든 물리 재질 초기화
            ResetPhysicMaterials();
            Debug.Log("모든 오브젝트의 물리 재질이 초기화되었습니다.");
        }
    }
    
    // 모든 오브젝트의 물리 재질 초기화
    void ResetPhysicMaterials()
    {
        // 태그가 'BouncePlatform'인 모든 오브젝트에 적용
        GameObject[] platforms = GameObject.FindGameObjectsWithTag("BouncePlatform");
        foreach (GameObject platform in platforms)
        {
            if (platform != null)
            {
                Collider[] colliders = platform.GetComponentsInChildren<Collider>(true);
                foreach (Collider collider in colliders)
                {
                    if (!collider.isTrigger)
                    {
                        collider.material = null;
                        
                        // BounceEnhancer 컴포넌트 제거
                        BounceEnhancer enhancer = collider.gameObject.GetComponent<BounceEnhancer>();
                        if (enhancer != null)
                        {
                            Destroy(enhancer);
                        }
                    }
                }
            }
        }
    }
    
    // 태그가 'BouncePlatform'인 모든 오브젝트에만 물리 재질 적용하는 함수
    void ApplyPhysicMaterialToBouncePlatforms()
    {
        // 물리 재질이 없으면 리턴
        if (bounceMaterial == null) 
        {
            Debug.LogWarning("물리 재질이 없습니다.");
            return;
        }
        
        // 적용된 오브젝트 카운트
        int appliedCount = 0;
        
        // 태그가 'BouncePlatform'인 모든 오브젝트에만 물리 재질 적용
        GameObject[] platforms = GameObject.FindGameObjectsWithTag("BouncePlatform");
        foreach (GameObject platform in platforms)
        {
            if (platform != null)
            {
                // 오브젝트의 Z축 위치 확인
                float zPosition = platform.transform.position.z;
                bool isPositiveZ = zPosition >= 0;
                
                // Z축 양수/음수 설정에 따라 적용
                bool shouldApply = (applyToPositiveZ && isPositiveZ) || (!applyToPositiveZ && !isPositiveZ);
                
                if (shouldApply)
                {
                    // 모든 콜라이더 컴포넌트 가져오기
                    Collider[] colliders = platform.GetComponentsInChildren<Collider>(true);
                    int colliderCount = 0;
                    
                    foreach (Collider collider in colliders)
                    {
                        // 트리거가 아닌 콜라이더에만 적용
                        if (!collider.isTrigger)
                        {
                            collider.material = bounceMaterial;
                            colliderCount++;
                        }
                    }
                    
                    if (colliderCount > 0)
                    {
                        appliedCount++;
                        
                        // 향상된 바운스 효과 적용
                        if (useEnhancedBounce)
                        {
                            ApplyEnhancedBounce(platform);
                        }
                        
                        Debug.Log($"<color=green>발판1에 물리 재질 및 바운스 효과 적용 완료</color>: {platform.name}, Z위치: {zPosition}, 적용된 콜라이더 수: {colliderCount}");
                    }
                }
                else
                {
                    Debug.Log($"물리 재질 적용 제외: {platform.name}, Z위치: {zPosition}, applyToPositiveZ: {applyToPositiveZ}");
                }
            }
        }
        
        Debug.Log($"<color=blue>총 {appliedCount}개 발판1에 물리 재질 적용 완료</color> ({(applyToPositiveZ ? "Z축 양수" : "Z축 음수")} 영역)");
    }
    
    /// <summary>
    /// 지정된 플랫폼에 향상된 바운스 기능을 적용합니다.
    /// </summary>
    private void ApplyEnhancedBounce(GameObject platform)
    {
        // 이미 BounceEnhancer가 있는지 확인
        BounceEnhancer enhancer = platform.GetComponent<BounceEnhancer>();
        if (enhancer == null)
        {
            // 플랫폼 게임오브젝트에 직접 컴포넌트 추가
            enhancer = platform.AddComponent<BounceEnhancer>();
            
            // 디버그 로깅 활성화
            enhancer.showDebugLogs = true;
            
            // 바운스 힘 배율 설정
            enhancer.forceMultiplier = 1.5f;
            
            // 최소 바운스 속도 낮춤
            enhancer.minVelocityForBounce = 0.5f;
            
            // 효과음 설정 (필요한 경우)
            if (bounceSound != null)
            {
                enhancer.bounceSound = bounceSound;
            }
            
            // 파티클 설정 (필요한 경우)
            if (bounceParticle != null)
            {
                enhancer.bounceParticle = bounceParticle;
            }
            
            Debug.Log($"<color=#00FF00>바운스 향상 기능이 적용됨</color>: {platform.name}");
        }
        else
        {
            Debug.Log($"<color=#FFFF00>이미 바운스 향상 기능이 적용되어 있음</color>: {platform.name}");
        }
        
        // 물리 매터리얼도 적용
        Collider[] colliders = platform.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            foreach (Collider col in colliders)
            {
                if (!col.isTrigger)
                {
                    // 트리거가 아닌 콜라이더에만 물리 재질 적용
                    col.material = bounceMaterial;
                    Debug.Log($"<color=#00FFFF>물리 재질 적용됨</color>: {col.name}");
                }
            }
        }
        else
        {
            Debug.LogWarning($"<color=#FF0000>콜라이더가 없음</color>: {platform.name}에 콜라이더가 없습니다!");
        }
    }
}
