using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallMovable : MonoBehaviour
{
	public bool isDown = true; // 벽이 처음에 내려가 있는지 여부, 아니면 false로 설정해야 함
	public bool isRandom = true; // 벽이 무작위로 내려갈지 여부 설정
	public float speed = 2f; // 벽의 이동 속도

	private float height; // 벽의 높이
	private float posYDown; // Y 좌표의 시작 위치 (최하단 위치)
	private bool isWaiting = false; // 벽이 위 또는 아래에서 대기 중인지 여부
	private bool canChange = true; // 벽이 내려갈지 여부를 결정 중인지

	void Awake()
    {
		InitializePosition();
	}
	
	// 오브젝트 풀링으로 재활성화될 때마다 호출됨
	void OnEnable()
    {
		InitializePosition();
		
		// 상태 초기화
		isWaiting = false;
		canChange = true;
		
		// 처음 상태가 내려가 있는 상태라면 확실히 내려가도록 위치 조정
		if(isDown)
		{
			transform.position = new Vector3(transform.position.x, posYDown, transform.position.z);
		}
		else
		{
			transform.position = new Vector3(transform.position.x, posYDown + height, transform.position.z);
		}
	}
	
	// 위치 초기화 함수
	private void InitializePosition()
	{
		height = transform.localScale.y; // 벽의 높이 계산
		if(isDown)
			posYDown = transform.position.y; // 초기 위치가 내려간 상태면 현재 Y 위치 저장
		else
			posYDown = transform.position.y - height; // 초기 위치가 올라간 상태면 내려간 위치 계산
	}

    // Update is called once per frame
    void Update()
    {
		if (isDown)
		{
			if (transform.position.y < posYDown + height) // 최대 높이에 도달하지 않았으면
			{
				transform.position += Vector3.up * Time.deltaTime * speed; // 위로 이동
			}
			else if (!isWaiting)
				StartCoroutine(WaitToChange(0.25f)); // 최대 높이에 도달했고 대기 중이 아니면 상태 변경 대기
		}
		else
		{
			if (!canChange)
				return; // 상태 변경을 결정 중이면 리턴

			if (transform.position.y > posYDown) // 최소 높이보다 높으면
			{
				transform.position -= Vector3.up * Time.deltaTime * speed; // 아래로 이동
			}
			else if (!isWaiting)
				StartCoroutine(WaitToChange(0.25f)); // 최소 높이에 도달했고 대기 중이 아니면 상태 변경 대기
		}
	}

	// 올라가거나 내려가기 전에 대기하는 함수
	IEnumerator WaitToChange(float time)
	{
		isWaiting = true;
		yield return new WaitForSeconds(time); // 지정된 시간만큼 대기
		isWaiting = false;
		isDown = !isDown; // 상태 전환 (올라감 <-> 내려감)

		if (isRandom && !isDown) // 랜덤 모드이고 벽이 올라간 상태이면
		{
			int num = Random.Range(0, 2); // 0 또는 1 랜덤 생성
			//Debug.Log(num);
			if (num == 1)
				StartCoroutine(Retry(1.5f)); // 1이면 내려갈지 재시도 결정
		}
	}

	// 벽이 내려갈 수 있는지 1.25초마다 확인하는 함수
	IEnumerator Retry(float time)
	{
		canChange = false; // 상태 변경 불가능하게 설정
		yield return new WaitForSeconds(time); // 지정된 시간만큼 대기
		int num = Random.Range(0, 2); // 0 또는 1 랜덤 생성
		//Debug.Log("2-"+num);
		if (num == 1)
			StartCoroutine(Retry(1.25f)); // 1이면 다시 재시도
		else
			canChange = true; // 0이면 상태 변경 가능하게 설정 (내려갈 수 있음)
	}
	
	// 수동으로 위치 초기화를 위한 공개 메서드
	public void ResetPosition()
	{
		InitializePosition();
	}
}
