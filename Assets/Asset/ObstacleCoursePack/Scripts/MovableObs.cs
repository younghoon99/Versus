using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 장애물을 수평 또는 수직으로 이동시키는 스크립트
/// </summary>
public class MovableObs : MonoBehaviour
{
	public float distance = 5f; // 오브젝트가 이동하는 총 거리
	public bool horizontal = false; // true: 수평 이동, false: 수직 이동
	public float speed = 3f; // 이동 속도
	public float offset = 0f; // 시작 위치 오프셋 (처음 위치를 조정하고 싶을 때 사용)

	private bool isForward = true; // 현재 전진 중인지 여부
	private Vector3 startPos; // 초기 시작 위치 저장
	private Vector3 centerPos; // 중앙 위치 저장
   
    /// <summary>
    /// 게임 시작 시 초기 설정
    /// </summary>
    void Awake()
    {
		// 초기 위치 저장
		startPos = transform.position;
		
		// 오프셋 적용 (수평/수직 방향에 따라 다르게 적용)
		if (horizontal)
			transform.position += Vector3.right * offset; // 수평(X축) 방향으로 오프셋 적용
		else
			transform.position += Vector3.forward * offset; // 수직(Z축) 방향으로 오프셋 적용
			
		// 오프셋 적용 후 중앙 위치 저장
		centerPos = transform.position;
	}

    /// <summary>
    /// 매 프레임마다 오브젝트 이동 처리
    /// </summary>
    void Update()
    {
		if (horizontal) // 수평 이동일 경우
		{
			if (isForward) // 전진 방향일 때
			{
				if (transform.position.x < centerPos.x + distance/2) // 최대 거리에 도달하지 않았다면
				{
					// 오른쪽(X축 양수)으로 이동
					transform.position += Vector3.right * Time.deltaTime * speed;
				}
				else
					isForward = false; // 최대 거리 도달 시 후진으로 방향 전환
			}
			else // 후진 방향일 때
			{
				if (transform.position.x > centerPos.x - distance/2) // 최소 거리에 도달하지 않았다면
				{
					// 왼쪽(X축 음수)으로 이동
					transform.position -= Vector3.right * Time.deltaTime * speed;
				}
				else
					isForward = true; // 최소 거리 도달 시 전진으로 방향 전환
			}
		}
		else // 수직 이동일 경우
		{
			if (isForward) // 전진 방향일 때
			{
				if (transform.position.z < centerPos.z + distance/2) // 최대 거리에 도달하지 않았다면
				{
					// 앞쪽(Z축 양수)으로 이동
					transform.position += Vector3.forward * Time.deltaTime * speed;
				}
				else
					isForward = false; // 최대 거리 도달 시 후진으로 방향 전환
			}
			else // 후진 방향일 때
			{
				if (transform.position.z > centerPos.z - distance/2) // 최소 거리에 도달하지 않았다면
				{
					// 뒤쪽(Z축 음수)으로 이동
					transform.position -= Vector3.forward * Time.deltaTime * speed;
				}
				else
					isForward = true; // 최소 거리 도달 시 전진으로 방향 전환
			}
		}
    }
}
