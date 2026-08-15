using UnityEngine;
using System.Collections;

public class Camera_Control : MonoBehaviour
{
    public Hare_Control player;
    private Vector2 velocity = Vector2.zero;
    public float smoothTime;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        //Smoothly follow player character
        Vector2 target = player.transform.position + (Vector3.up * 2.5f);

        transform.position = Vector2.SmoothDamp(transform.position, target, ref velocity, smoothTime);
        transform.position += (Vector3.back * 10);
    }

    private void LateUpdate()
    {
        
    }
}
