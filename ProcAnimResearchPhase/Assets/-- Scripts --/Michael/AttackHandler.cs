using UnityEngine;
using UnityEngine.InputSystem;

public enum EWeaponType
{
    BasicDisk,
    StickyDisk,
    TeleportDisk,
}
[RequireComponent(typeof(WeaponDefinitions))]
public class AttackHandler : MonoBehaviour
{
    [Header("Inscribed")]
    public WeaponDefinitions definitions;
    public string attackInputName = "CustomAttack";
    public string teleportInputName = "CustomTeleport";
    public string swapInputName = "CustomSwap";
    public Transform spawnPoint;
    public GameObject realCam;
    [Header("Dynamic")]
    public EWeaponType currentType = EWeaponType.BasicDisk;
    
    private PlayerInput playerInput;
    private InputAction attackAction;
    private InputAction teleportAction;
    private InputAction swapAction;

    private static AttackHandler S;
    //teleport variables
    private bool activeTeleport = false;
    private GameObject teleportTarget;
    void Awake()
    {
        if (S != null)
        {
            Debug.LogError("Attack Handler already Set");
        }
        else
        {
            S = this;
        }
        playerInput = GetComponentInParent<PlayerInput>();
        attackAction = playerInput.actions.FindAction(attackInputName);
        teleportAction = playerInput.actions.FindAction(teleportInputName);
        swapAction = playerInput.actions.FindAction(swapInputName);
    }

    void Update()
    {
        if (swapAction.triggered)
        {
            switch (currentType)
            {
                case EWeaponType.BasicDisk:
                    currentType = EWeaponType.TeleportDisk;
                    break;
                case EWeaponType.TeleportDisk:
                    currentType = EWeaponType.BasicDisk;
                    break;
            }
        }
        if (attackAction.triggered)
        {
            Attack();
        }

        if (teleportAction.triggered)
        {
            if (activeTeleport)
            {
                Teleport();
            }
        }
    }
    public void Attack()
    {
        switch (currentType)
        {
            case EWeaponType.BasicDisk: 
                Disk disk = definitions.GetWeapon<Disk>();
                GameObject diskIns = Instantiate(disk.gameObject, spawnPoint.position, Quaternion.identity);
                disk.transform.forward = realCam.transform.forward;
                Rigidbody rb = diskIns.GetComponent<Rigidbody>();
                rb.AddForce(realCam.transform.forward * disk.speed, ForceMode.Impulse);
                break;
            case EWeaponType.StickyDisk:
                break;
            case EWeaponType.TeleportDisk:
                TeleportDisk teleportDisk = definitions.GetWeapon<TeleportDisk>();
                teleportTarget = Instantiate(teleportDisk.gameObject, spawnPoint.position, Quaternion.identity);
                teleportDisk.transform.forward = realCam.transform.forward;
                Rigidbody teleportRb = teleportTarget.GetComponent<Rigidbody>();
                teleportRb.AddForce(realCam.transform.forward * teleportDisk.speed, ForceMode.Impulse);
                activeTeleport = true;
                break;
        }
    }
    public void Teleport()
    {
        transform.position = teleportTarget.transform.position;
        Destroy(teleportTarget);
        S.activeTeleport = false;
    }

    public static void ActivateTeleport(GameObject teleportTar)
    {
        S.teleportTarget = teleportTar;
        S.activeTeleport = true;
    }
}
