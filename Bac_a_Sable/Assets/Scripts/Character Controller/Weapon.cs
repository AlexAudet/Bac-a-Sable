using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : ScriptableObject
{
    public enum WeaponType
    {
        Melee, Gun
    }

    public WeaponType weaponType;
}
