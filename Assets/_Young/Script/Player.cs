using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun; // Photon 네트워크 동기화용
using System;
using SF = UnityEngine.SerializeField;
using UnityEngine.UI;
using TMPro;

// WASD키 : 리지드바디 이동
// Space키 : 리지드바디 점프
// Alt키 : 커서표시 / 숨기기
// Tab키 : 1인칭/3인칭 시점 전환
// 마우스 움직임 : 카메라 회전
// 마우스 휠 : 3인칭 카메라 줌 인/아웃
[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{

    public Rigidbody rb; // Ending.cs에서 접근할 수 있도록 public으로 변경
    private Vector3 moveDir;
    private float hRot;
    private PhotonView pv;
    [SerializeField] ParticleSystem slideJumpParticleSystem;
    [SerializeField] AudioClip jumpSound;
    [SerializeField] AudioClip DieSound;
    [SerializeField] AudioClip HitSound;
    [SerializeField] AudioClip CeremonySound;

    // Player.cs에 추가
    public int teamId = 1; // 기본값은 1 (싱글플레이 또는 플레이어 1)

    private Animator animator;

    // 입력 상태
    private bool isCursorLocked = false;
    private bool isMoving = false;

    // 시상식 모드 관련
    private bool ceremonyModeActive = false;
    private float ceremonyModeTimer = 0f;
    private Vector3 ceremonyLookTarget;

    private bool isRotating = false;
    private bool isJumpRequired = false;
    private bool isGrounded = false; // Ground 확인 변수 추가
    private bool isBouncePlatform = false; // BouncePlatform 확인 변수 추가

    // 키 설정
    [SF] private KeyCode cursorLockKey = KeyCode.LeftAlt;
    [SF] private KeyCode jumpKey = KeyCode.Space;

    // 계수 설정
    [SF, Range(0f, 100f)] private float moveSpeed = 10f;
    [SF, Range(0f, 200f)] private float rotateSpeed = 100f;
    [SF, Range(0f, 100f)] private float jumpForce = 5f;
    [Header("슬라이드 점프 설정")]
    [SerializeField] private KeyCode slideJumpKey = KeyCode.LeftShift;
    private float slideJumpForwardForce = 50f; // 앞으로 힘
    private float slideJumpUpForce = 2f;       // 위로 힘


    private float slideJumpCooldown = 1f;    // 쿨타임
    private bool canSlideJump = true;


    // 스턴 관련 변수 추가
    [Header("스턴 설정")]
    [SF, Range(0f, 10f)] private float defaultStunTime = 0.5f; // 기본 스턴 지속 시간 (초)
    private bool canMove = true;         // 플레이어가 이동 가능한 상태인지 (false면 스턴 상태)
    private bool isStuned = false;       // 플레이어가 현재 스턴 상태인지
    private bool wasStuned = false;      // 스턴 상태에서 중첩 스턴이 들어왔는지 체크
    private float pushForce;             // 스턴 상태에서 밀려나는 힘의 크기
    private Vector3 pushDir;             // 스턴 상태에서 밀려나는 방향

    // 사망 관련 변수 및 기능
    [Header("사망 설정")]
    [SF] private bool isDead = false;                // 플레이어가 현재 사망 상태인지

    [SF] private float respawnDelay = 3f;            // 자동 리스폰 대기 시간
    [SF] private GameObject deathUIPanel;            // 사망 시 표시할 UI 패널
    [SF] private TextMeshProUGUI countdownText;                 // 카운트다운 텍스트
    private Coroutine respawnCoroutine;              // 리스폰 코루틴 참조

    /// <summary>
    /// 플레이어 사망 처리
    /// </summary>
    public void Die()
    {
        if (isDead) return;
        isDead = true;
        canMove = false; // 이동, 점프 등 모든 컨트롤 차단

        // 물리 동작 정지 및 입력 차단
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true; // 원하면 잠시 비활성화
        }

        // 사망 애니메이션 실행
        if (animator != null)
        {
            animator.ResetTrigger("Respawn"); // 혹시 남아있을 트리거 초기화
            animator.SetTrigger("Die");
        }

        AudioSource.PlayClipAtPoint(DieSound, transform.position);

        // 사망 UI 표시
        if (deathUIPanel != null && pv.IsMine)
            deathUIPanel.SetActive(true);

        if (respawnCoroutine != null) StopCoroutine(respawnCoroutine);
        respawnCoroutine = StartCoroutine(RespawnCountdown());

        Debug.Log($"플레이어 {teamId} 사망");
    }
    /// <summary>
    /// 자동 리스폰 카운트다운 코루틴
    /// </summary>
    private IEnumerator RespawnCountdown()
    {
        float remainingTime = respawnDelay;
        while (remainingTime > 0)
        {
            if (countdownText != null)
                countdownText.text = Mathf.CeilToInt(remainingTime).ToString();
            yield return new WaitForSeconds(1f);
            remainingTime -= 1f;
        }
        if (deathUIPanel != null)
            deathUIPanel.SetActive(false);
        Respawn();
    }




    private void Awake()
    {
        // Animator, PhotonView 모두 안전하게 할당
        animator = GetComponent<Animator>();
        pv = GetComponent<PhotonView>();
    }

    private void Start()
    {
        if (!TryGetComponent(out rb))
            rb = gameObject.AddComponent<Rigidbody>();

        rb.freezeRotation = true; // 다른 강체에 부딪혔을 때 회전하지 않도록 설정한다.
        isCursorLocked = false;   // 마우스 커서 초기 상태 : 커서 표시 & 미잠금

        // Unity의 기본 중력 사용
        rb.useGravity = true;

        // FrameRate가 너무 높으면 FixedUpdate에 의한 회전이 부자연스러워진다.
        // 추후, targetFrameRate 설정 코드는 여기서 제거하고 매니저 클래스로 옮기는 것이 좋다.
        Application.targetFrameRate = 60;

        // 카메라 컴포넌트 초기화


        // 사망 UI 패널 초기 비활성화
        InitDeathUIPanel();
    }

    /// <summary>
    /// 사망 UI 패널 초기 비활성화
    /// </summary>
    private void InitDeathUIPanel()
    {
        if (deathUIPanel != null)
        {
            deathUIPanel.SetActive(false);
        }
    }





    /// <summary>
    /// 필요한 컴포넌트가 할당되었는지 체크
    /// </summary>
    private void LogNotInitializedComponentError<T>(T component, string componentName) where T : Component
    {
        if (component == null)
            Debug.LogError($"{componentName} 컴포넌트를 인스펙터에 넣어주세요");
    }

    private void Update()
    {
        if (!pv.IsMine) return;

        // 채팅 입력 중이면 모든 입력 무시
        if (ChatManager.Instance != null && ChatManager.Instance.IsChatting)
        {
            // 한글 주석: 채팅 입력 중에는 이동, 회전, 점프, 슬라이딩 등 모든 입력을 차단합니다.
            return;
        }

        // 시상식 모드 중이면 5초간 움직임 금지 및 카메라 바라보기
        if (ceremonyModeActive)
        {
            canMove = false;
            // 카메라 바라보기(플레이어 회전)
            Vector3 lookDir = ceremonyLookTarget - transform.position;
            lookDir.y = 0f;
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
            ceremonyModeTimer -= Time.deltaTime;
            if (ceremonyModeTimer <= 0f)
            {
                ceremonyModeActive = false;
                canMove = true;
            }
            return; // 시상식 중엔 나머지 입력 무시
        }

        ToggleCursorLock();
        Move();
        Rotate();
        Jump();
        Silding();

        // Ground 체크
        CheckGrounded();
        Animation();
    }
    private void Animation()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            animator.SetTrigger("Emotion1");
            animator.SetTrigger("Respawn");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            animator.SetTrigger("Emotion2");
            animator.SetTrigger("Respawn");
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            animator.SetTrigger("Emotion3");
            animator.SetTrigger("Respawn");
        }
    }
    private bool hasAirSlideJumped = false; // 공중 슬라이딩 점프 1회 제한용 플래그

    private void Silding()
    {
        if (isDead || !canMove) return; // 사망/조작불가 시 입력 차단
        // 땅에 있으면 플래그 리셋
        if (isGrounded)
        {
            hasAirSlideJumped = false;
        }

        // 슬라이딩 점프 입력
        if (Input.GetKeyDown(slideJumpKey) && canSlideJump)
        {
            // 공중이면 1회만 허용
            if (!isGrounded)
            {
                if (hasAirSlideJumped) return; // 이미 썼으면 무시
                hasAirSlideJumped = true;
            }
            // 모든 플레이어에게 슬라이딩 점프 애니메이션 동기화
            pv.RPC("RpcSlideJump", RpcTarget.All);
            AudioSource.PlayClipAtPoint(jumpSound, transform.position, 20f); // 볼륨 20배 (더 크게)
        }
    }
    // 모든 플레이어에게 슬라이딩 점프 애니메이션 동기화 (RPC)
    [PunRPC]
    private void RpcSlideJump()
    {
        StartCoroutine(SlideJump());
    }

    // 실제 슬라이딩 점프 동작 (애니메이션, 힘 적용 등)
    // 슬라이딩 점프 후, 착지 시점에서만 stun(조작 차단) 적용 구조
    private bool needLandingStun = false; // 착지 stun 필요 여부 플래그

    private IEnumerator SlideJump()
    {
        canSlideJump = false;

        // 애니메이션 재생 (동기화됨)
        animator.SetTrigger("SlideJump");

        // 항상 같은 힘(정면+위)으로 슬라이딩 점프 (이동 중이든 멈춰있든 동일)
        rb.velocity = Vector3.zero; // 모든 속도 제거
        Vector3 jumpDirection = transform.forward * slideJumpForwardForce + Vector3.up * slideJumpUpForce;
        rb.AddForce(jumpDirection, ForceMode.VelocityChange); // 동일한 힘 적용

        // Respawn 애니메이션 재생
        animator.SetTrigger("Respawn");

        // 파티클 이펙트 재생 (슬라이딩 점프 효과)
        if (slideJumpParticleSystem != null)
        {
            slideJumpParticleSystem.Play(); // 파티클 시작
        }

        // 착지 시 stun이 필요함을 표시
        needLandingStun = true;

        // 쿨타임 기다림
        yield return new WaitForSeconds(slideJumpCooldown);
        canSlideJump = true;
    }

    // 착지 판정 함수 내에서 호출 필요
    private IEnumerator LandingStunCoroutine()
    {
        canMove = false;
        yield return new WaitForSeconds(0.3f);
        canMove = true;
    }

    // 시상식 모드 진입용 RPC (Ending에서 호출)
    [PunRPC]
    public void RpcMoveToPodium(Vector3 pos, Quaternion rot)
    {
        if (pv.IsMine)
        {
            transform.position = pos;
            transform.rotation = rot;
        }
    }

    [PunRPC]
    public void RpcEnterCeremonyMode(Vector3 lookTarget, float duration, string emotionName)
    {
        // 카메라 모두 비활성화
        var camSetup = GetComponent<CameraSetup>();
        if (camSetup != null)
            camSetup.DisableAllCameras();
        // 시상식 모드 활성화 및 타겟/타이머 설정
        ceremonyModeActive = true;
        ceremonyModeTimer = duration;
        ceremonyLookTarget = lookTarget;
        // 애니메이션 트리거 처리
        if (animator != null && !string.IsNullOrEmpty(emotionName))
            animator.SetTrigger(emotionName);

        // 시상식 음악 재생 (모든 클라이언트에서 5초간 반복)
        if (pv != null)
            pv.RPC("RpcPlayCeremonySound", RpcTarget.All, transform.position);
    }

    // 감정 표현 동기화용 RPC 함수 (Photon)
    // 박수 소리를 5초간 반복 재생하는 PunRPC
    [PunRPC]
    public void RpcPlayCeremonySound(Vector3 pos)
    {
        // 한글 주석: 5초간 박수 소리를 반복 재생하는 코루틴 실행
        StartCoroutine(PlayClapSoundCoroutine(pos));
    }

    // 박수 소리를 5초간 반복 재생하는 코루틴
    private IEnumerator PlayClapSoundCoroutine(Vector3 pos)
    {
        float duration = 5f;
        float elapsed = 0f;
        float interval = CeremonySound != null ? CeremonySound.length : 1f;
        while (elapsed < duration)
        {
            if (CeremonySound != null)
                AudioSource.PlayClipAtPoint(CeremonySound, pos);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
    }

    [PunRPC]
    public void RpcPlayEmotion(string emotionName)
    {
        // Animator에 해당 트리거를 전달하여 감정 애니메이션 재생
        if (animator != null)
        {
            animator.SetTrigger(emotionName); // 예: "Emotion1", "Emotion2", "doJump" 등
        }
    }

    private void ToggleCursorLock()
    {
        // NOTE : cursorLockKey는 토글 키로 사용되며, 커서 잠금 및 표시 상태를 전환한다.
        if (Input.GetKeyDown(cursorLockKey))
        {
            isCursorLocked = !isCursorLocked;
            Cursor.lockState = isCursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isCursorLocked;
        }
    }

    private void Move()
    {
        if (isDead || !canMove) return; // 사망/조작불가 시 이동 차단
        // NOTE : GetAxis()를 사용할 경우, 정지 시 조금씩 미끄러지며 멈춘다.
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        isMoving = (h != 0f || v != 0f);

        if (isMoving)
        {
            moveDir = transform.TransformDirection(new Vector3(h, 0f, v));
            moveDir.Normalize();
        }
        animator.SetBool("isRun", isMoving);

        // 감정 표현 입력 처리 (동기화)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            pv.RPC("RpcPlayEmotion", RpcTarget.All, "Emotion1");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            pv.RPC("RpcPlayEmotion", RpcTarget.All, "Emotion2");
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            pv.RPC("RpcPlayEmotion", RpcTarget.All, "Emotion3");
        }
    }

    private void Rotate()
    {
        // NOTE 1 : "Mouse X"에 대한 GetAxis(), GetAxisRaw()는 차이가 없다.
        // NOTE 2 : 커서 잠금 및 미표시 상태에서만 회전하도록 한다.
        if (isCursorLocked)
        {
            hRot = Input.GetAxis("Mouse X");
            isRotating = (hRot != 0f);
        }
        else
            isRotating = false;
    }

    // 점프 입력 처리
    private void Jump()
    {
        if (isDead || !canMove) return; // 사망/조작불가 시 점프 차단
        // Ground 태그 있는 오브젝트 위에서만 점프 가능하도록 수정
        if (Input.GetKeyDown(jumpKey) && (isGrounded || isBouncePlatform))
        {
            animator.SetBool("isJump", true);
            pv.RPC("RpcPlayEmotion", RpcTarget.All, "doJump");
            AudioSource.PlayClipAtPoint(jumpSound, transform.position, 20f); // 볼륨 20배 (더 크게)
            isJumpRequired = true;
        }
    }

    // Ground 체크 함수 (바닥 감지 및 슬라이딩 점프 후 조작 복구)
    private bool wasGrounded = false; // 이전 프레임의 착지 상태 저장
    private void CheckGrounded()
    {
        float rayDistance = 0.2f;
        isGrounded = false;
        isBouncePlatform = false;

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, rayDistance))
        {
            if (hitInfo.collider.CompareTag("Ground"))
                isGrounded = true;
            else if (hitInfo.collider.CompareTag("BouncePlatform"))
                isBouncePlatform = true;
        }
        animator.SetBool("isJump", false);

        // // 슬라이딩 점프 후 조작 불가 상태에서 바닥에 착지하면 조작 복구
        // if (isGrounded && !canMove)
        // {
        //     canMove = true;
        // }

        // 슬라이딩 점프 후 조작 불가 상태에서 바닥에 착지하면 stun 코루틴 실행
        if (!wasGrounded && isGrounded && needLandingStun)
        {
            Debug.Log("착지 stun");
            StartCoroutine(LandingStunCoroutine());
            needLandingStun = false;
        }

        wasGrounded = isGrounded; // 현재 프레임의 착지 상태 저장
    }

    private void FixedUpdate()
    {
        float fixedDeltaTime = Time.fixedDeltaTime;

        // 스턴 상태가 아닐 때만 일반 이동 처리
        if (canMove)
        {
            if (isMoving)
            {
                // velocity를 이용한 이동 (y축 속도는 기존 값 유지)
                Vector3 velocity = moveDir * moveSpeed;
                velocity.y = rb.velocity.y;
                rb.velocity = velocity;
            }
            else
            {
                // 이동 입력 없을 때 수평 이동 멈춤, y축 속도만 유지
                Vector3 velocity = rb.velocity;
                velocity.x = 0f;
                velocity.z = 0f;
                rb.velocity = velocity;
            }

            if (isRotating)
            {
                float rotAngle = hRot * rotateSpeed * fixedDeltaTime;
                rb.rotation = Quaternion.AngleAxis(rotAngle, Vector3.up) * rb.rotation; // 좌우 회전
            }

            if (isJumpRequired)
            {
                rb.AddForce(new Vector3(0f, jumpForce, 0f), ForceMode.VelocityChange);
                isJumpRequired = false;
            }
        }
        else
        {
            // 스턴 상태일 때 밀려나는 처리
            Vector3 currentVelocity = rb.velocity;
            rb.velocity = new Vector3(pushDir.x * pushForce, currentVelocity.y, pushDir.z * pushForce);
        }

        // Unity의 기본 중력을 사용하므로 여기서 중력 적용하지 않음
    }

    /// <summary>
    /// 플레이어를 스턴 상태로 만들고 특정 방향으로 밀어내는 함수
    /// </summary>
    /// <param name="velocityF">밀어내는 방향과 힘을 결합한 벡터</param>
    /// <param name="time">스턴 지속 시간 (초)</param>
    public void HitPlayer(Vector3 velocityF, float time = 0f)
    {
        // 스턴 지속 시간이 0이하면 기본값 사용
        if (time <= 0f)
            time = defaultStunTime;

        // 밀려나는 힘과 방향 저장
        pushForce = velocityF.magnitude;
        pushDir = Vector3.Normalize(velocityF);

        // 현재 수직 속도 유지하며 수평 방향만 설정
        Vector3 currentVelocity = rb.velocity;
        rb.velocity = new Vector3(velocityF.x, currentVelocity.y, velocityF.z);

        // 스턴 효과 적용 코루틴 시작
        StartCoroutine(Decrease(velocityF.magnitude, time));

        // 애니메이터가 있다면 스턴 애니메이션 트리거 (필요시 추가)
        if (animator != null)
        {
            animator.SetTrigger("Emotion2"); // 스턴 애니메이션 트리거가 있다면 주석 해제
            animator.SetTrigger("Respawn");
        }

        // 오디오 재생 디버깅 및 null 체크
        Debug.Log("HitPlayer 호출됨");
        if (HitSound == null)
        {
            Debug.LogWarning("HitSound AudioClip이 할당되어 있지 않습니다!");
        }
        else
        {
            AudioSource.PlayClipAtPoint(HitSound, transform.position, 2f); // 볼륨 2배 (더 크게)
            Debug.Log("HitSound 재생 시도 (볼륨 2f)");
        }
    }

    /// <summary>
    /// 스턴 효과를 일정 시간 동안 적용하고 점차 감소시키는 코루틴
    /// </summary>
    /// <param name="value">초기 밀려나는 힘의 크기</param>
    /// <param name="duration">스턴 지속 시간</param>
    /// <returns></returns>
    private IEnumerator Decrease(float value, float duration)
    {
        // 현재 스턴 상태에서 중첩 스턴이 들어온 경우
        if (isStuned)
            wasStuned = true;

        // 스턴 상태로 설정
        isStuned = true;
        canMove = false;

        // 지속 시간 동안 힘 감소율 계산
        float delta = value / duration;

        // 지정된 시간 동안 힘 감소
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            yield return null;

            // 일반 바닥에서는 힘 감소 (미끄러운 바닥에서는 감소하지 않음)
            bool isSlippery = false; // 미끄러운 바닥 체크 로직 필요 시 추가

            if (!isSlippery)
            {
                // 시간에 따라 밀려나는 힘 감소
                pushForce = pushForce - Time.deltaTime * delta;
                pushForce = pushForce < 0 ? 0 : pushForce;
            }

            // 코루틴에서 중력 적용 제거
        }

        // 중첩 스턴인 경우
        if (wasStuned)
        {
            wasStuned = false;
        }
        else
        {
            // 스턴 해제
            isStuned = false;
            canMove = true;
        }
    }







    private void OnCollisionEnter(Collision collision)
    {
        // Die 태그를 가진 오브젝트와 충돌했을 때 사망 처리
        if (collision.gameObject.CompareTag("Die"))
        {
            Die();
        }

        // 기존의 OnCollisionEnter 로직이 있다면 여기에 추가
    }

    private void OnTriggerEnter(Collider other)
    {
        // Die 태그를 가진 오브젝트와 트리거 충돌했을 때 사망 처리
        if (other.CompareTag("Die"))
        {
            Die();
        }

        // 기존의 OnTriggerEnter 로직이 있다면 여기에 추가
    }


    /// <summary>
    /// 플레이어 리스폰 처리
    /// </summary>
    public void Respawn()
    {
        // 사망 UI 숨기기
        if (deathUIPanel != null)
            deathUIPanel.SetActive(false);

        // 애니메이터 상태 초기화
        if (animator != null)
        {
            animator.ResetTrigger("Die");
            animator.SetTrigger("Respawn");
        }

        isDead = false;
        canMove = true; // 이동/점프 등 다시 허용

        if (rb != null)
        {
            rb.isKinematic = false; // 다시 활성화
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        SavePointManager.Instance.RespawnPlayer(gameObject, teamId);

        Debug.Log($"플레이어 {teamId} 리스폰");
    }
}