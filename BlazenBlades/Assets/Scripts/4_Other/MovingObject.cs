using System;
using UnityEngine;
using System.Collections;
public class MovingObject : MonoBehaviour
{
    [Header("Location")]
    
    [SerializeField]
    private Transform[] movePoints;
    
    [Header("Speed")]
    
    [SerializeField]
    private float moveSpeed;
    
    [Header("Timing")]
    
    [SerializeField]
    private float waitTimeBetweenPoints;
    
    //Dynamic, Non Serialized Below
    
    private int currentPoint;

    private bool isMoving;
    
    private void Start()
    {
        if (movePoints.Length < 2)
        {
            Debug.LogError("Moving Object: Not enough move points, Destroying Object");

            Destroy(gameObject);
        }
        
        
        transform.position = movePoints[0].position;
        
        currentPoint = 0;
        
        isMoving = true;
        
        StartCoroutine(MoveThroughPoints());
    }
    
    private IEnumerator MoveThroughPoints()
    {
        while (isMoving)
        {
            if (Vector3.Distance(transform.position, movePoints[currentPoint].position) < 0.1f)
            {
                yield return new WaitForSeconds(waitTimeBetweenPoints);
                
                if (currentPoint == movePoints.Length - 1)
                {
                    currentPoint = 0;
                }
                else
                {
                    currentPoint++;
                }
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, movePoints[currentPoint].position, moveSpeed * Time.deltaTime);
            }
            
            yield return null;
        }
    }
}
