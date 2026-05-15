using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class timer : MonoBehaviour
{
    public UnityEvent onTimeUp;
    public float timeLeft = 2.0f;
    bool timerDone = false;
    // Start is called before the first frame update
    void Start()
    {
        onTimeUp = new UnityEvent();
    }

    // Update is called once per frame
    void Update()
    {
        if(!timerDone) timeLeft-= Time.deltaTime;
        if(timeLeft < 0)
        {
            Debug.Log("Time is up!");
            timeLeft = 0; 
            onTimeUp.Invoke();
            timerDone = true;
        }
    }
}
