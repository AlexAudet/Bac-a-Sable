using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class PlayerWeaponManager : MonoBehaviour
{
    public string nextWeaponINput = "NextWeapon";
    public string previousWeaponInput = "PreviousWeapon";
    public string attackInput = "Attack";

    [PropertySpace(10,0)]
    public Weapon[] weapons;
   
    Weapon currentWeapon;
    int currentWeaponIndex;

    public void WeaponAttack()
    {

    }

    void NextWeapon()
    {
        currentWeaponIndex++;

        if (currentWeaponIndex >= weapons.Length)
            currentWeaponIndex = 0;

        currentWeapon = weapons[currentWeaponIndex];
    }

    void PreviousWeapon()
    {
        currentWeaponIndex--;

        if (currentWeaponIndex < 0)
            currentWeaponIndex = weapons.Length - 1;

        currentWeapon = weapons[currentWeaponIndex];
    }

    void EquipCurrentWeapon()
    {

    }
    void DesequipCurrentWeapon()
    {

    }

}
