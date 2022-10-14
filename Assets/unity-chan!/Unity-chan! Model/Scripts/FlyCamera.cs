using System;
using UnityEngine;

public class FlyCamera : MonoBehaviour
{
    [SerializeField] private float m_speed = 3f;
    [SerializeField] private float m_lookSpeedHorizontal = 50f;
    [SerializeField] private float m_lookSpeedVertical = 50f;
    
    private void Update ()
    {
        if (Input.GetKey (KeyCode.Mouse1))
        {
            var move = new Vector3 (
                Input.GetAxis ("Horizontal"),
                Input.GetAxis ("UpDown"),
                Input.GetAxisRaw ("Vertical")
            );

            transform.position += transform.TransformDirection (move) * (Time.deltaTime * m_speed);

            var rotateUpDown = Input.GetAxis ("Mouse Y");
            var rotateLeftRight = Input.GetAxis ("Mouse X");
            
            transform.Rotate (transform.right, -rotateUpDown * Time.deltaTime * m_lookSpeedVertical, Space.World);
            transform.Rotate (Vector3.up, rotateLeftRight * Time.deltaTime * m_lookSpeedHorizontal, Space.World);
        }
    }
}
