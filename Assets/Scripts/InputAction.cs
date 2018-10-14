using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;

public enum InputType
{
    NormalAttack,
    SpecialAttack,
    ChargeAttack,
    Jump,
    Dodge,
    AngleUp,
    AngleDown,
    Pause,
    Inventory,
    Fastfall
}

public struct InputAction
{
    public bool held;
    public bool down;
    public bool up;

    private InputType type;
    private int bufferCounter;

    //Constructor
    public InputAction(InputType it)
    {
        held = false;
        down = false;
        up = false;
        type = it;
        bufferCounter = 0;
    }

    //Decreases counter if buffer exists
    public void CountDownBuffer()
    {
        if (bufferCounter > 0)
            bufferCounter--;
    }

    //Sets the buffer
    public void SetBuffer(int frames)
    {
        bufferCounter = frames;
    }

    //Updates values
    public void UpdateInputs(Player p)
    {
        if (bufferCounter > 0)
            held = true;
        else
            held = p.GetButton(type.ToString());

        down = p.GetButtonDown(type.ToString());

        up = p.GetButtonUp(type.ToString());
    }
}
