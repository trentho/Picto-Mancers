using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public float jumpHeight = 2f;

    private bool isJumping = false;

    public float moveSpeed = 5f;
    public float turnSpeed = 10f;

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        transform.Translate(Vector3.forward * vertical * moveSpeed * Time.deltaTime);
        transform.Rotate(Vector3.up, horizontal * turnSpeed * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Space) && !isJumping)
        {
            Jump();
        }
    }

    private void Jump()
    {
        isJumping = true;

        // Play jumping animation or sound

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.AddForce(Vector3.up * Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y), ForceMode.VelocityChange);
    }

    private void OnCollisionEnter(Collision collision)
    {
        isJumping = false;
    }
}