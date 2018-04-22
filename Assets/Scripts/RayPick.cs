using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayPick : MonoBehaviour {

    [SerializeField]CreateMesh cmesh;
    private void Update()
    {       
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            if (Physics.Raycast(ray.origin, ray.direction, out hitInfo))
            {
                cmesh.AddPoint(hitInfo.point);
              
            }
           
        }
            
    }
}
