using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 장애물과 충돌할 때 튕겨내는 기능과 일시적인 스턴 효과를 적용하는 스크립트
/// </summary>
public class Bounce : MonoBehaviour
{
	 float force = 30f; // 플레이어를 밀어내는 힘의 크기 (값이 클수록 더 멀리 튕겨냄)
	 float stunTime = 1f; // 플레이어가 스턴에 걸리는 시간 (초 단위)
	private Vector3 hitDir; // 충돌 방향을 저장하는 변수

	/// <summary>
	/// 다른 오브젝트와 충돌했을 때 호출되는 이벤트 함수
	/// </summary>
	/// <param name="collision">충돌한 객체의 정보</param>
	void OnCollisionEnter(Collision collision)
	{
		// 충돌 지점들을 순회하며 처리
		foreach (ContactPoint contact in collision.contacts)
		{
			// 디버그 목적으로 충돌 지점과 방향을 시각화 (Scene 뷰에서만 보임)
			Debug.DrawRay(contact.point, contact.normal, Color.white);
			
			// 충돌한 객체가 "Player" 태그를 가진 경우에만 처리
			if (collision.gameObject.tag == "Player")
			{
				// 충돌 표면의 법선 벡터를 저장 (어느 방향에서 충돌했는지)
				hitDir = contact.normal;
				
				// 플레이어의 CharacterControls 컴포넌트에 접근하여 HitPlayer 함수 호출
				// -hitDir: 충돌 방향의 반대 방향으로 플레이어를 밀어냄
				// force: 밀어내는 힘의 크기
				// stunTime: 플레이어가 제어 불능 상태로 있는 시간
				collision.gameObject.GetComponent<Player>().HitPlayer(-hitDir * force, stunTime);
				return; // 첫 번째 충돌 지점만 처리하고 함수 종료
			}
		}
		
		/* 주석 처리된 코드 (이전 버전의 충돌 처리 로직)
		if (collision.relativeVelocity.magnitude > 2)
		{
			if (collision.gameObject.tag == "Player")
			{
				//Debug.Log("Hit");
				collision.gameObject.GetComponent<CharacterControls>().HitPlayer(-hitDir*force, stunTime);
			}
			//audioSource.Play();
		}*/
	}
}
