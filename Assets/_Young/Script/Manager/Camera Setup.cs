using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class CameraSetup : MonoBehaviourPun
{
    [Header("카메라 리그 분리")]
    [SerializeField] private GameObject fpCameraRig; // 1인칭 카메라 리그
    [SerializeField] private GameObject tpCameraRig; // 3인칭 카메라 리그
    [Header("카메라 참조")]
    [SerializeField] private Camera fpCamera; // 1인칭 카메라
    [SerializeField] private Camera tpCamera; // 3인칭 카메라


    private bool isFirstPerson = true; // 현재 시점
    private Player playerController; // 플레이어 컨트롤러 참조
    private Transform target; // 카메라가 따라갈 대상
    private PhotonView pv; // Photon View 참조 저장

    // Start is called before the first frame update
    void Start()
    {
        // Photon View 가져오기
        pv = GetComponent<PhotonView>();
        // 플레이어 컨트롤러 참조 가져오기
        playerController = GetComponent<Player>();
        // 1인칭/3인칭 카메라 리그 자동 할당
        if (fpCameraRig == null)
        {
            Transform fpRig = transform.Find("FP Root 1인칭/FP Camera Rig");
            if (fpRig != null)
                fpCameraRig = fpRig.gameObject;
        }
        if (tpCameraRig == null)
        {
            Transform tpRig = transform.Find("TP Camera Rig 3인칭");
            if (tpRig != null)
                tpCameraRig = tpRig.gameObject;
        }
        // 카메라 찾기
        if (fpCamera == null && fpCameraRig != null)
            fpCamera = fpCameraRig.GetComponentInChildren<Camera>();
        if (tpCamera == null && tpCameraRig != null)
            tpCamera = tpCameraRig.GetComponentInChildren<Camera>();
        // 둘 다 없으면 MainCamera 사용(비추천)
        if (fpCamera == null && tpCamera == null)
        {
            fpCamera = Camera.main;
        }
        if (pv.IsMine)
        {
            // 자신이 로컬 플레이어라면 카메라가 자신을 추적하게 함
            SetupLocalPlayer();
        }
        else
        {
            // 원격 플레이어라면 카메라 비활성화
            DisableCamera();
        }
    }
    void Update()
    {
        // 내 플레이어만 카메라 입력 처리
        if (pv == null || !pv.IsMine) return;

        // 시점 전환(Tab)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SwitchCameraView();
        }

        // 마우스 회전 처리
        HandleCameraRotation();

        // 3인칭 카메라 줌(마우스 휠)
        HandleTpCameraZoom();
    }

    /// <summary>
    /// 마우스 움직임에 따른 카메라 회전 (1인칭/3인칭 모두)
    /// </summary>
    private void HandleCameraRotation()
    {
        if (!Cursor.lockState.Equals(CursorLockMode.Locked)) return;
        float t = Time.deltaTime;
        float h = Input.GetAxisRaw("Mouse X") * 100f * t;
        float v = -Input.GetAxisRaw("Mouse Y") * 100f * t;

        // 플레이어 전체 좌우 회전
        if (Mathf.Abs(h) > 0.001f)
        {
            Quaternion hRotation = Quaternion.AngleAxis(h, Vector3.up);
            transform.rotation = hRotation * transform.rotation;
        }

        // 카메라 리그 상하 회전
        if (Mathf.Abs(v) > 0.001f)
        {
            if (isFirstPerson && fpCameraRig != null)
            {
                float xAngle = fpCameraRig.transform.localEulerAngles.x + v;
                if (xAngle > 180f) xAngle -= 360f;
                if (-45f < xAngle && xAngle < 45f)
                {
                    fpCameraRig.transform.localEulerAngles = new Vector3(xAngle, 0f, 0f);
                }
            }
            else if (!isFirstPerson && tpCameraRig != null)
            {
                float xAngle = tpCameraRig.transform.localEulerAngles.x + v;
                if (xAngle > 180f) xAngle -= 360f;
                if (-45f < xAngle && xAngle < 45f)
                {
                    tpCameraRig.transform.localEulerAngles = new Vector3(xAngle, 0f, 0f);
                }
            }
        }
    }

    /// <summary>
    /// 3인칭 카메라 줌 인/아웃 처리 (마우스 휠)
    /// </summary>
    /// <summary>
    /// 3인칭 카메라 줌 인/아웃 처리 (마우스 휠)
    /// Player.cs 방식 구조 이식 (Rig-카메라 거리 기반)
    /// </summary>
    private float tpCamZoomInitialDistance = 0f;
    private float tpCameraWheelInput = 0f;
    private float currentWheel = 0f;
    private float zoomInDistance = 3f;  // 최대 줌 인 거리
    private float zoomOutDistance = 3f; // 최대 줌 아웃 거리
    private float zoomSpeed = 20f;      // 줌 속도
    private float zoomAccel = 0.1f;     // 줌 가속

    private void HandleTpCameraZoom()
    {
        // 3인칭 카메라만 줌 가능
        if (isFirstPerson || tpCamera == null || tpCameraRig == null) return;

        // 마우스 휠 입력 처리
        tpCameraWheelInput = Input.GetAxisRaw("Mouse ScrollWheel");
        currentWheel = Mathf.Lerp(currentWheel, tpCameraWheelInput, zoomAccel);

        // 충분한 입력이 없으면 처리하지 않음
        if (Mathf.Abs(currentWheel) < 0.01f) return;

        Transform tpCamTr = tpCamera.transform;
        Transform tpCamRig = tpCameraRig.transform;

        // 초기 거리 세팅 (최초 1회만)
        if (tpCamZoomInitialDistance == 0f)
            tpCamZoomInitialDistance = Vector3.Distance(tpCamTr.position, tpCamRig.position);

        float zoom = Time.deltaTime * zoomSpeed;
        float currentCamToRigDist = Vector3.Distance(tpCamTr.position, tpCamRig.position);
        Vector3 move = Vector3.forward * zoom * currentWheel * 10f;

        // Zoom In (휠 위로)
        if (currentWheel > 0.01f)
        {
            if (tpCamZoomInitialDistance - currentCamToRigDist < zoomInDistance)
            {
                tpCamTr.Translate(move, Space.Self);
            }
        }
        // Zoom Out (휠 아래로)
        else if (currentWheel < -0.01f)
        {
            if (currentCamToRigDist - tpCamZoomInitialDistance < zoomOutDistance)
            {
                tpCamTr.Translate(move, Space.Self);
            }
        }
    }

    // 로컬 플레이어 카메라 설정
    private void SetupLocalPlayer()
    {
        if (fpCameraRig == null && tpCameraRig == null)
        {
            Debug.LogError("카메라 리그가 할당되지 않았습니다!");
            return;
        }
        if (fpCamera == null && tpCamera == null)
        {
            Debug.LogError("1인칭/3인칭 카메라가 할당되지 않았습니다!");
            return;
        }
        // 카메라 리그 활성화 (1인칭만)
        isFirstPerson = true;
        if (fpCameraRig != null)
            fpCameraRig.SetActive(true);
        if (tpCameraRig != null)
            tpCameraRig.SetActive(false);
        // 카메라 활성화
        if (fpCamera != null)
        {
            fpCamera.gameObject.SetActive(true);
            fpCamera.tag = "MainCamera";
        }
        if (tpCamera != null)
        {
            tpCamera.gameObject.SetActive(false);
            tpCamera.tag = "Untagged";
        }
        // 카메라가 따라갈 대상 설정
        target = transform;
        // Player 스크립트에 1인칭 카메라 참조 전달(필요시)
        // playerController.SetCamera(...) 호출 제거 (이제 필요 없음)
        if (playerController == null)
        {
            Debug.LogWarning("Player 컴포넌트를 찾을 수 없습니다!");
        }
    }

    // 모든 카메라(1인칭/3인칭/리그 포함) 비활성화 (시상식 등)
    public void DisableAllCameras()
    {
        if (fpCameraRig != null)
            fpCameraRig.SetActive(false);
        if (tpCameraRig != null)
            tpCameraRig.SetActive(false);
        if (fpCamera != null)
            fpCamera.gameObject.SetActive(false);
        if (tpCamera != null)
            tpCamera.gameObject.SetActive(false);
    }

    // 원격 플레이어 카메라 비활성화
    private void DisableCamera()
    {
        if (fpCameraRig != null)
            fpCameraRig.SetActive(false);
        if (tpCameraRig != null)
            tpCameraRig.SetActive(false);
        if (fpCamera != null)
            fpCamera.gameObject.SetActive(false);
        if (tpCamera != null)
            tpCamera.gameObject.SetActive(false);
    }
    // 시점 전환 (1인칭 <-> 3인칭)
    public void SwitchCameraView()
    {
        // 내 플레이어(PV.IsMine)만 시점 전환 및 카메라 활성화/비활성화/태그 처리
        if (pv != null && !pv.IsMine) return;
        isFirstPerson = !isFirstPerson;
        // 카메라 리그 활성화/비활성화
        if (fpCameraRig != null)
            fpCameraRig.SetActive(isFirstPerson);
        if (tpCameraRig != null)
            tpCameraRig.SetActive(!isFirstPerson);
        // 카메라 활성화/비활성화 및 태그 처리
        if (fpCamera != null)
        {
            fpCamera.gameObject.SetActive(isFirstPerson);
            fpCamera.tag = isFirstPerson ? "MainCamera" : "Untagged";
        }
        if (tpCamera != null)
        {
            tpCamera.gameObject.SetActive(!isFirstPerson);
            tpCamera.tag = isFirstPerson ? "Untagged" : "MainCamera";
        }
    }
}
