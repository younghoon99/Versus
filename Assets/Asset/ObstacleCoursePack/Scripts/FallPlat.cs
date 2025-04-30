using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallPlat : MonoBehaviour
{
	public float fallTime = 0.5f;


	void OnCollisionEnter(Collision collision)
	{
		foreach (ContactPoint contact in collision.contacts)
		{
			//Debug.DrawRay(contact.point, contact.normal, Color.white);
			if (collision.gameObject.tag == "Player")
			{
				StartCoroutine(Fall(fallTime));
			}
		}
	}

	IEnumerator Fall(float time)
	{
		// 지정 시간 후 플랫폼 비활성화
		yield return new WaitForSeconds(time);

		// Renderer와 Collider만 비활성화
		Renderer rend = GetComponent<Renderer>();
		Collider col = GetComponent<Collider>();
		if (rend != null) rend.enabled = false;
		if (col != null) col.enabled = false;

		// 2초 후 다시 활성화
		yield return new WaitForSeconds(2f);
		if (rend != null) rend.enabled = true;
		if (col != null) col.enabled = true;
	}

}
