using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 충돌 시 더 강력한 바운스 효과를 제공하는 컴포넌트
/// </summary>
using Photon.Pun;

public class BounceEnhancer : MonoBehaviourPun
{
    [Header("바운스 설정")]
    [Tooltip("바운스 힘 배율 (1.0 = 기본)")]
    public float forceMultiplier = 1.5f;
    
    [Tooltip("바운스가 적용되는 최소 속도")]
    public float minVelocityForBounce = 0.5f; // 0.5f로 낮춤 (기존 2.0f)
    
    [Tooltip("바운스 효과 최대 횟수 (0 = 무제한)")]
    public int maxBounceCount = 0;
    
    [Header("효과 설정")]
    [Tooltip("바운스 시 효과음")]
    public AudioClip bounceSound;
    
    [Tooltip("바운스 시 생성할 파티클")]
    public GameObject bounceParticle;
    
    [Header("디버깅")]
    [Tooltip("디버그 메시지 표시 여부")]
    public bool showDebugLogs = true;
    
    // 현재까지 바운스 횟수
    private int bounceCount = 0;
    // 오디오 소스 컴포넌트
    private AudioSource audioSource;
    // 마지막 바운스 시간 (연속 바운스 방지)
    private float lastBounceTime = 0f;
    
    private void Awake()
    {
        // 오디오 소스가 필요하면 추가
        if (GetComponent<AudioSource>() == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; // 3D 사운드
            audioSource.volume = 1.0f; // 볼륨 최대로 설정
            audioSource.outputAudioMixerGroup = null; // 기본 마스터 채널 사용
            
            if (showDebugLogs)
            {
                Debug.Log($"<color=cyan>AudioSource 컴포넌트 추가됨</color>: {gameObject.name}");
            }
        }
        else
        {
            audioSource = GetComponent<AudioSource>();
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"<color=cyan>BounceEnhancer 초기화됨</color>: {gameObject.name}, 힘 배율: {forceMultiplier}");
        }
    }
    
    /// <summary>
    /// 바운스 힘 배율 설정
    /// </summary>
    public void SetForceMultiplier(float multiplier)
    {
        forceMultiplier = multiplier;
        if (showDebugLogs)
        {
            Debug.Log($"<color=cyan>바운스 힘 배율 설정됨</color>: {multiplier}");
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // 네트워크: 내 캐릭터(혹은 내 소유 오브젝트)에서만 바운스 처리
        if (photonView != null && !photonView.IsMine) return;

        // 디버그 로그 추가
        if (showDebugLogs)
        {
            Debug.Log($"<color=yellow>충돌 감지됨</color>: {collision.gameObject.name}, 레이어: {LayerMask.LayerToName(collision.gameObject.layer)}");
        }
        
        // 너무 빠른 연속 바운스 방지 (0.1초 간격)
        if (Time.time - lastBounceTime < 0.1f)
        {
            return;
        }
        
        ApplyBounce(collision);
    }
    
    // OnCollisionStay 추가하여 계속 접촉 상태에서도 바운스 처리
    private void OnCollisionStay(Collision collision)
    {
        // 네트워크: 내 캐릭터(혹은 내 소유 오브젝트)에서만 바운스 처리
        if (photonView != null && !photonView.IsMine) return;

        // 너무 빠른 연속 바운스 방지 (0.5초 간격)
        if (Time.time - lastBounceTime < 0.5f)
        {
            return;
        }
        
        ApplyBounce(collision);
    }
    
    // 바운스 적용 로직을 별도 메서드로 분리
    private void ApplyBounce(Collision collision)
    {
        // 최대 바운스 횟수 확인
        if (maxBounceCount > 0 && bounceCount >= maxBounceCount)
        {
            if (showDebugLogs)
            {
                Debug.Log($"<color=orange>최대 바운스 횟수 도달</color>: {bounceCount}/{maxBounceCount}");
            }
            return;
        }

        // contacts 배열이 비었는지 체크
        if (collision.contacts == null || collision.contacts.Length == 0)
        {
            if (showDebugLogs)
                Debug.LogWarning("ContactPoint 없음");
            return;
        }
        
        // 충돌한 오브젝트의 Rigidbody 가져오기
        Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            // 충돌체에 리지드바디가 없으면 부모에서 찾아봄
            rb = collision.gameObject.GetComponentInParent<Rigidbody>();
            
            if (rb == null && showDebugLogs)
            {
                Debug.LogWarning($"<color=red>Rigidbody 없음</color>: {collision.gameObject.name}에 Rigidbody가 없습니다.");
                return;
            }
        }
        
        // 속도 확인 및 바운스 적용
        float velocityMagnitude = collision.relativeVelocity.magnitude;
        if (showDebugLogs)
        {
            Debug.Log($"충돌 속도: {velocityMagnitude}, 최소 속도: {minVelocityForBounce}");
        }
        
        // 속도가 최소값보다 큰 경우에만 바운스 적용
        if (velocityMagnitude > minVelocityForBounce)
        {
            // 충돌 지점과 노말 벡터 (표면의 수직 방향)
            ContactPoint contact = collision.contacts[0];
            Vector3 normal = contact.normal;
            
            // 입사 방향을 표면에 대해 반사
            Vector3 reflectDir = Vector3.Reflect(rb.velocity.normalized, normal);
            
            // 기존 속도 크기와 배율을 곱해 더 강한 힘으로 튀어오르게 함
            float forceMagnitude = rb.velocity.magnitude * forceMultiplier;
            
            // 최소 힘 적용 (너무 작은 힘이면 확실히 튀어오르도록)
            if (forceMagnitude < 3f)
            {
                forceMagnitude = 3f;
            }
            
            // 힘 적용
            rb.velocity = reflectDir * forceMagnitude;
            
            // 바운스 횟수 증가
            bounceCount++;
            
            // 마지막 바운스 시간 업데이트
            lastBounceTime = Time.time;
            
            // [네트워크 동기화] 바운스 효과(사운드, 파티클)를 모든 클라이언트에 동기화
            photonView.RPC("PlayBounceEffectsRPC", RpcTarget.All, contact.point);
            
            if (showDebugLogs)
            {
                Debug.Log($"<color=green>바운스 적용됨</color>: 힘={forceMagnitude}, 방향={reflectDir}, 횟수={bounceCount}");
            }
        }
    }
    
    /// <summary>
    /// [네트워크 동기화] 모든 클라이언트에서 실행되는 바운스 효과 RPC
    /// </summary>
    [PunRPC]
    private void PlayBounceEffectsRPC(Vector3 position)
    {
        PlayBounceEffects(position);
        Debug.Log("<color=cyan>이펙트 사운드 재생</color>");
    }
    /// <summary>
    /// 바운스 관련 효과 재생 (소리, 파티클 등)
    /// </summary>
    private void PlayBounceEffects(Vector3 position)
    {
        if (showDebugLogs)
        {
            Debug.Log($"<color=cyan>바운스 효과 재생</color>: 위치 = {position}");
        }
        
        // 효과음 재생
        if (bounceSound != null)
        {
            // 요청한대로 0.6초부터 재생하기 위한 설정
            float startTime = 0.6f;
            
            // 클립 길이가 0.6초보다 작은 경우 처리
            if (bounceSound.length <= startTime)
            {
                startTime = 0f;
                if (showDebugLogs)
                {
                    Debug.LogWarning($"<color=orange>클립 길이가 0.6초보다 짧습니다</color>: {bounceSound.length}초, 처음부터 재생합니다.");
                }
            }
            
            // 임시 AudioSource 생성 (PlayClipAtPoint는 시작 시간 설정을 지원하지 않음)
            GameObject tempAudio = new GameObject("TempAudio");
            tempAudio.transform.position = position;
            AudioSource tempSource = tempAudio.AddComponent<AudioSource>();
            tempSource.clip = bounceSound;
            tempSource.spatialBlend = 1.0f; // 3D 사운드
            tempSource.volume = 1.0f;
            tempSource.time = startTime; // 0.6초부터 재생 시작
            tempSource.Play();
            
            // 오디오 재생이 끝나면 임시 오브젝트 제거
            float destroyTime = bounceSound.length - startTime + 0.1f;
            Destroy(tempAudio, destroyTime);
            
            // 기존 AudioSource도 업데이트 (다음 재생을 위해)
            if (audioSource != null)
            {
                audioSource.clip = bounceSound;
                audioSource.time = startTime; // 0.6초부터 재생 시작
                audioSource.pitch = Random.Range(0.9f, 1.1f); // 약간의 변화 추가
                audioSource.Play();
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"<color=cyan>효과음 재생됨</color>: {bounceSound.name}, 위치: {position}, 시작 시간: {startTime}초");
            }
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning("<color=red>효과음 없음</color>: bounceSound가 설정되지 않았습니다. 인스펙터에서 설정해주세요!");
        }
        
        // 파티클 효과 생성
        if (bounceParticle != null)
        {
            GameObject particleObj = Instantiate(bounceParticle, position, Quaternion.identity);
            
            // 5초 후 파티클 오브젝트 제거
            Destroy(particleObj, 5f);
            
            if (showDebugLogs)
            {
                Debug.Log($"<color=cyan>파티클 생성됨</color>: {bounceParticle.name}");
            }
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning("<color=orange>파티클 없음</color>: bounceParticle이 설정되지 않았습니다.");
        }
    }
    
    /// <summary>
    /// 바운스 횟수 초기화
    /// </summary>
    public void ResetBounceCount()
    {
        bounceCount = 0;
        lastBounceTime = 0f;
        
        if (showDebugLogs)
        {
            Debug.Log("<color=cyan>바운스 횟수 초기화됨</color>");
        }
    }
    
    // 스크립트가 제거될 때
    private void OnDestroy()
    {
        if (showDebugLogs)
        {
            Debug.Log($"<color=orange>BounceEnhancer 제거됨</color>: {gameObject.name}");
        }
    }
}
