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
            Vector3 translation = new Vector3(speed * Time.deltaTime, 0, 0);
            lerpTimer = Time.deltaTime * speed*5f;
            transform.Translate(Vector3.Lerp(origin, translation, lerpTimer));
        }
        if(Input.GetKey(KeyCode.A))
        {
            Vector3 translation = new Vector3(-speed * Time.deltaTime, 0, 0);
            lerpTimer = Time.deltaTime * speed*5f;
            transform.Translate(Vector3.Lerp(origin, translation, lerpTimer));
        }
        if(Input.GetKey(KeyCode.S))
        {
            Vector3 translation = new Vector3(0, -speed * Time.deltaTime, 0);
            lerpTimer = Time.deltaTime * speed*5f;
            transform.Translate(Vector3.Lerp(origin, translation, lerpTimer));
        }
        if(Input.GetKey(KeyCode.W))
        {
            Vector3 translation = new Vector3(0, speed * Time.deltaTime, 0);
            lerpTimer = Time.deltaTime * speed*5f;
            transform.Translate(Vector3.Lerp(origin, translation, lerpTimer));
        }
    }
}
