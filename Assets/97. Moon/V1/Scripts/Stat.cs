using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public enum ClassType
{
    Warrior,
    Assassin,
    Cleric,
    Wizard,
}

[Serializable]
public class Stat
{
    private ClassType classType;

    [Header("Base Stats (Growth)")]
    public int STR;
    public int AGI;
    public int INT;

    [Header("Calculated Results")]
    public int max_Hp;
    public int current_Hp;
    public int def;
    public int atk;

    public int speed;
    public float crt;
    public float acc;

    public float Insanity_Ctrl;
    public float Reaction_Ctrl;
    public int Sp_Atk;

    private BaseCharacter character;

    public void Init(BaseCharacter character)
    {
        this.character = character;
    }
}