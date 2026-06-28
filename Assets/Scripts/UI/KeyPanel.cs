using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyPanel : MonoBehaviour
{
    public KeyCode keyCode;

    public GameObject panel;

    public UIPanel uiPanel;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(keyCode))
        {
            if(panel!=null) panel.SetActive(true);
            if(uiPanel!= null) uiPanel.ShowPanel();
        }
    }
    
}
