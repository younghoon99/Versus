using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
  public float speed = 3f;
  private Rigidbody rb;

  void Start()
  {
    rb = GetComponent<Rigidbody>();
    rb.isKinematic = true; // 직접 회전 제어, 중력 X
  }

  void FixedUpdate()
  {
    Quaternion deltaRotation = Quaternion.Euler(0f, 0f, speed * Time.fixedDeltaTime / 0.01f);
    rb.MoveRotation(rb.rotation * deltaRotation);
  }
}
