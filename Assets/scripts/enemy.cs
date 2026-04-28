using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyPatrol : MonoBehaviour
{
    public Transform[] waypoints;
    public float rotationSpeed = 5f;
    public float moveSpeed = 3f;
    public float waitTime = 1f;
    private int currentWaypointIndex = 0;
    private bool isWaiting = false;
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        if (waypoints.Length > 0)
        {
            StartCoroutine(Patrol());
        }
    }

    IEnumerator Patrol()
    {
        while (true)
        {
            if (!isWaiting)
            {
                Transform targetWaypoint = waypoints[currentWaypointIndex];
                agent.SetDestination(targetWaypoint.position);
                
                while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
                {
                    yield return null;
                }
                
                isWaiting = true;
                yield return new WaitForSeconds(waitTime);
                isWaiting = false;
                
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            }
            yield return null;
        }
    }
}
