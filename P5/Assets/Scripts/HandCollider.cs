using UnityEngine;

public class HandCollider : MonoBehaviour
{
    public Transform position;
    public Transform target;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        position.position = target.position;
    }

    // Update is called once per frame
    void Update()
    {
        position.position = target.position;
    }
}
