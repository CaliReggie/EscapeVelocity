using UnityEngine;

public class Disk : Weapon
{
    [Header("Basic Disk Data")]
    public float speed = 15;
    public float lifeSpan = 5f;
    [Header("Ricochet")]
    public bool ricochet = false;
    public int maxRicochetCount = 0;
    public float rayDist = 0.6f;
    public LayerMask ricochetMask;
    private GameObject recentHitObject;
    private float recentHitTime;
    private float startTime;
    private Rigidbody rb;
    private int ricochetCount = 0;
    

    protected virtual void Awake()
    {
        startTime = Time.time;
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        TimeCheck();
    }

    protected virtual void TimeCheck()
    {
        if (lifeSpan + startTime < Time.time)
        {
            DiskEnd();
        }
    }

    void FixedUpdate()
    {
        Movement();
        if (ricochet)
        {
            RayRicochetCheck();
        }
    }

    protected virtual void Movement()
    {
        if (rb.linearVelocity.magnitude > 0.05f)
        {
            transform.forward = rb.linearVelocity.normalized;
        }
        rb.AddForce(new Vector3(0f, -5f, 0f), ForceMode.Acceleration);
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        
        if (!Utils.IsLayerInLayerMask(collision.gameObject.layer, ricochetMask))
        {
            return;
        }
        else if (ricochet)
        {
            if (recentHitObject == collision.collider.gameObject && ((Time.time - recentHitTime) < 0.25f))
            {
                return;
            } 
            Debug.Log("Collision Hit: " + collision.collider.gameObject.name);
            Ricochet(collision.contacts[0].normal);
        }
        else
        {
            DiskEnd();
        }
        
    }

    protected virtual void RayRicochetCheck()
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, rayDist, ricochetMask))
        {
            if (recentHitObject == hit.collider.gameObject && ((Time.time - recentHitTime) < 0.25f) || hit.collider.gameObject.layer == 13)
            {
                return;
            }
            Debug.Log("Raycast Hit: " + hit.collider.gameObject.name);
            Ricochet(hit.normal);
            recentHitObject = hit.collider.gameObject;
        }
    }

    protected virtual void Ricochet(Vector3 hitNormal)
    {
        ricochetCount++;
        if (ricochetCount >= maxRicochetCount)
        {
            DiskEnd();
        }
        Vector3 reflectDirection= Vector3.Reflect(transform.forward, hitNormal);
        transform.forward = reflectDirection;
        float currentSpeed = rb.linearVelocity.magnitude;
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(transform.forward * currentSpeed, ForceMode.Impulse);
        recentHitTime = Time.time;
    }
    protected virtual void DiskEnd()
    {
        Destroy(gameObject);
    }
    
}
