using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraMovementPC : MonoBehaviour
{
    public Camera camera;
    public float speed = 3.5f;

    private void Awake()
    {
        if (camera == null)
        {
            camera = Camera.main;
        }
    }

    private float lerpTimer;
    private Vector3 origin = new Vector3(0, 0, 0);
    private void Update()
    {
        //ZoomIn
        Vector3 pos = transform.position;
        if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            lerpTimer = Time.deltaTime * speed ;
            transform.position = Vector3.Lerp(pos, pos - transform.forward*2, lerpTimer);
        }
        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            lerpTimer = Time.deltaTime * speed ;
            transform.position = Vector3.Lerp(pos, pos + transform.forward*2, lerpTimer);
        }
        
        //Rotate
        if(Input.GetMouseButton(0)) 
        {
            transform.Rotate(new Vector3(-Input.GetAxis("Mouse Y") * speed, Input.GetAxis("Mouse X") * speed, 0));
            float X = transform.rotation.eulerAngles.x;
            float Y = transform.rotation.eulerAngles.y;
            transform.rotation = Quaternion.Euler(X, Y, 0);
        }
        
        //Move
        if(Input.GetKey(KeyCode.D))
        {
            lerpTimer = Time.deltaTime * speed ;
            transform.position = Vector3.Lerp(pos, pos + transform.right*2, lerpTimer);
        }
        if(Input.GetKey(KeyCode.A))
        {
            lerpTimer = Time.deltaTime * speed ;
            transform.position = Vector3.Lerp(pos, pos - transform.right*2, lerpTimer);
        }
        if(Input.GetKey(KeyCode.S))
        {
            lerpTimer = Time.deltaTime * speed ;
            transform.position = Vector3.Lerp(pos, pos - transform.forward*2, lerpTimer);
        }
        if(Input.GetKey(KeyCode.W))
        {
            lerpTimer = Time.deltaTime * speed ;
            transform.position = Vector3.Lerp(pos, pos + transform.forward*2, lerpTimer);
        }
        if(Input.GetKey(KeyCode.Q))
        {
            lerpTimer = Time.deltaTime * speed ;
            transform.position = Vector3.Lerp(pos, pos - transform.up*2, lerpTimer);
        }
        if(Input.GetKey(KeyCode.E))
        {
            lerpTimer = Time.deltaTime * speed ;
            transform.position = Vector3.Lerp(pos, pos + transform.up*2, lerpTimer);
        }
    }
}
