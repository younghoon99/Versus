using UnityEngine;
using UnityEngine.VFX;
using Photon.Pun; // [네트워크 동기화 추가]

namespace WindTriggerSystem
{
    // 바람 효과와 팬, 먼지 등 시각 효과를 제어하는 스크립트
    // [네트워크 동기화 추가]
    public class WindTrigger : MonoBehaviourPun
    {
        [Header("Fan Rotation")]
        // 팬(선풍기) 오브젝트의 회전 Transform
        [SerializeField] private Transform _fanRotation;
        // 팬 회전 속도
        [SerializeField] private float _fanRotateSpeed;
        // 팬 가속도(점점 빨라지는 정도)
        [SerializeField] private float _fanAcceleration = 0.2f;
        // 팬 최소 속도
        [SerializeField] private float _minFanSpeed = 0.0f;
        // 팬 최대 속도
        [SerializeField] private float _maxFanSpeed = 1500f;

        [Header("Wind Distortion")]
        // 바람 왜곡 효과를 위한 렌더러(머티리얼)
        [SerializeField] private Renderer _windDistortionRenderer;
        // 바람 왜곡 강도
        [SerializeField] private float _windDistortion;
        // 바람 왜곡 가속도
        [SerializeField] private float _windAcceleration = 0.00003f;
        // 바람 왜곡 최소값
        [SerializeField] private float _minWindDistortion = 0.0f;
        // 바람 왜곡 최대값
        [SerializeField] private float _maxWindDistortion = 0.2f;

        [Header("Wind Swirl")]
        // 바람 소용돌이(Visual Effect)
        [SerializeField] private VisualEffect _windSwirl;
        // 소용돌이 회전 속도
        [SerializeField] private float _windRotateSpeed;
        // 소용돌이 가속도
        [SerializeField] private float _windSwirlAcceleration = 0.2f;
        // 소용돌이 최소 속도
        [SerializeField] private float _minWindSpeed = 0.0f;
        // 소용돌이 최대 속도
        [SerializeField] private float _maxWindSpeed = 1500f;

        // 소용돌이 왜곡 속도
        [SerializeField] private float _windDistortionSpeed;
        // 소용돌이 왜곡 가속도
        [SerializeField] private float _windSwirlDistortionAcceleration = 0.0065f;
        // 소용돌이 왜곡 최소값
        [SerializeField] private float _minSwirlDistortionScale = 0.0f;
        // 소용돌이 왜곡 최대값
        [SerializeField] private float _maxSwirlDistortionScale = 50.0f;

        [Header("Wind Dust")]
        // 바람 먼지 효과(Visual Effect)
        [SerializeField] private VisualEffect _windDust;
        // 먼지의 빨려들어가는 속도
        [SerializeField] private float _attractionSpeed;
        // 먼지 최소 속도
        [SerializeField] private float _minAttractionSpeed = 0.0f;
        // 먼지 최대 속도
        [SerializeField] private float _maxAttractionSpeed = 0.015f;

        // 팬(바람) 활성화 여부
        public bool _isFanOn = true;

        // 게임 시작 시 각 효과의 값을 최대치로 초기화
        void Start()
        {
            _fanRotateSpeed = _maxFanSpeed;
            _windDistortion = _maxWindDistortion;
            _windRotateSpeed = _maxWindSpeed;
            _windDistortionSpeed = _maxSwirlDistortionScale;
            _attractionSpeed = _maxAttractionSpeed;

            // 바람 먼지 효과 오브젝트 활성화
            if (_windDust != null)
            {
                _windDust.gameObject.SetActive(true);
            }
        }

        // 매 프레임마다 시각 효과 및 속도 값 갱신
        void Update()
        {
            // [네트워크 동기화 추가] 마스터 클라이언트만 효과 값 갱신
            if (!PhotonNetwork.IsMasterClient) return;

            // 팬 오브젝트 회전
            if (_fanRotation != null)
            {
                _fanRotation.Rotate(Vector3.up * _fanRotateSpeed * Time.deltaTime);
            }

            // 바람 왜곡 머티리얼 값 갱신
            if (_windDistortionRenderer != null && _windDistortionRenderer.material != null)
            {
                _windDistortionRenderer.material.SetFloat("_DistortionAmount", _windDistortion);
            }

            // 소용돌이 효과 값 갱신
            if (_windSwirl != null)
            {
                _windSwirl.SetFloat("SwirlRotationSpeed", _windRotateSpeed);
                _windSwirl.SetFloat("SwirlDistortionScale", _windDistortionSpeed);
            }

            // 먼지 효과 값 갱신 및 활성화
            if (_windDust != null)
            {
                _windDust.SetFloat("AttractionSpeed", _attractionSpeed);
                _windDust.gameObject.SetActive(true);
            }

            // 팬이 꺼져 있으면 다시 켬(항상 켜짐 유지)
            if (!_isFanOn)
            {
                _isFanOn = true;
            }

            // 각 효과 값을 항상 최대치로 유지
            _fanRotateSpeed = _maxFanSpeed;
            _windDistortion = _maxWindDistortion;
            _windRotateSpeed = _maxWindSpeed;
            _windDistortionSpeed = _maxSwirlDistortionScale;
            _attractionSpeed = _maxAttractionSpeed;
        }

        // 효과 값들을 가속(점점 증가)시키는 함수
        // [네트워크 동기화 추가] 효과 값 가속도 변경시 RPC로 동기화
        public void AccelerationNetwork()
        {
            photonView.RPC("AccelerationRPC", RpcTarget.AllBuffered);
        }

        [PunRPC]
        private void AccelerationRPC()
        {
            _fanRotateSpeed += _fanAcceleration;
            _windDistortion += _windAcceleration;
            _windRotateSpeed += _windSwirlAcceleration;
            _windDistortionSpeed += _windSwirlDistortionAcceleration;
            _attractionSpeed += _windAcceleration;

            // 최대값 제한
            if (_fanRotateSpeed > _maxFanSpeed)
            {
                _fanRotateSpeed = _maxFanSpeed;
            }

            if (_windDistortion > _maxWindDistortion)
            {
                _windDistortion = _maxWindDistortion;
            }

            if (_windRotateSpeed > _maxWindSpeed)
            {
                _windRotateSpeed = _maxWindSpeed;
            }

            if (_windDistortionSpeed > _maxSwirlDistortionScale)
            {
                _windDistortionSpeed = _maxSwirlDistortionScale;
            }

            if (_attractionSpeed > _maxAttractionSpeed)
            {
                _attractionSpeed = _maxAttractionSpeed;
            }
        }

        // 효과 값들을 감속(점점 감소)시키는 함수
        // [네트워크 동기화 추가] 효과 값 감속도 변경시 RPC로 동기화
        public void DecelerationNetwork()
        {
            photonView.RPC("DecelerationRPC", RpcTarget.AllBuffered);
        }

        [PunRPC]
        private void DecelerationRPC()
        {
            _fanRotateSpeed -= _fanAcceleration;
            _windDistortion -= _windAcceleration;
            _windRotateSpeed -= _windSwirlAcceleration;
            _windDistortionSpeed -= _windSwirlDistortionAcceleration;
            _attractionSpeed -= _windAcceleration;

            // 최소값 제한
            if (_fanRotateSpeed < _minFanSpeed)
            {
                _fanRotateSpeed = _minFanSpeed;
            }

            if (_windDistortion < _minWindDistortion)
            {
                _windDistortion = _minWindDistortion;
            }

            if (_windRotateSpeed < _minWindSpeed)
            {
                _windRotateSpeed = _minWindSpeed;
            }

            if (_windDistortionSpeed < _minSwirlDistortionScale)
            {
                _windDistortionSpeed = _minSwirlDistortionScale;
            }

            if (_attractionSpeed < _minAttractionSpeed)
            {
                _attractionSpeed = _minAttractionSpeed;
            }
        }
    }
}