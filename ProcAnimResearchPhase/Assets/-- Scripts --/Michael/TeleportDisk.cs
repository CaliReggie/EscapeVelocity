using UnityEngine;

public class TeleportDisk : Disk
{
    [Header("Teleport Disk")]
    public GameObject teleporterPrefab;
    public float playerHeight;
    private bool frozen = false;

    protected override void Movement()
    {
        if (!frozen)
        {
            base.Movement();
        }
    }
    protected override void DiskEnd()
    {
        frozen = true;
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeAll;
        GetComponent<Collider>().enabled = false;
        ricochet = false;
    }
}
