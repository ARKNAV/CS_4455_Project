using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public GameObject player;

    private double rotation = 0;
    private Vector3 camPos = new Vector3(0,0,0);
    private float offset = 3f;

    void Update()
    {
        //Uncertain how to get right stick setup yet, this is so theres functionality here
        if (Keyboard.current.rightArrowKey.isPressed)
        {
            rotation += .01;
        } else if (Keyboard.current.leftArrowKey.isPressed)
        {
            rotation -= .01;
        }
    }

	void LateUpdate ()
	{
        camPos = new Vector3((float)Math.Cos(rotation), 1, (float)Math.Sin(rotation)) * offset;
        transform.position = player.transform.position + camPos;
        transform.LookAt(player.transform);
    }
}
