using UnityEngine;

public class WeaponDefinitions : MonoBehaviour
{
    public Weapon[] weapons;
    public T GetWeapon<T>()
    {
        foreach (Weapon w in weapons)
        {
            if (w is T specificWeapon)
            {
                return specificWeapon;
            }
        }
        return default(T);
    }
}
