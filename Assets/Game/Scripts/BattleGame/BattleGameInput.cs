using Photon.Deterministic;
using Quantum;
using UnityEngine;
using UInput = UnityEngine.Input;
using QInput = Quantum.Input;

public class BattleGameInput : MonoBehaviour
{
    private bool LeftDown = false;
    private float LeftDownTime = 0;

    private bool RightDown = false;
    private float RightDownTime = 0;

    private float Joystick_X = 0;
    private float Joystick_Y = 0;

    private bool PickThrowButton = false;

    private QInput input = new QInput();
    private void OnEnable()
    {
        QuantumCallback.Subscribe<CallbackPollInput>(this, PollInput);

        // EnableNewUnityInput();
    }

    private void OnDisable()
    {
        QuantumCallback.UnsubscribeListener<CallbackPollInput>(this);

        // DisableNewUnityInput();
    }

    private void PollInput(CallbackPollInput pollInput)
    {
        PollUnityInput(pollInput);
    }

    public void ClearInput()
    {

        input.Direction = FPVector2.Zero;
        input.JumpButton = false;
        input.PickThrowButton = false;
        input.LeftButton = false;
        input.RightButton = false;

    }
    private void PollUnityInput(CallbackPollInput pollInput)
    {
        ClearInput();

        input.Direction = new FPVector2(Joystick_X.ToFP(), Joystick_Y.ToFP());
        input.LeftButton = UnityEngine.Input.GetKeyDown(KeyCode.A);
        input.RightButton = UnityEngine.Input.GetKeyDown(KeyCode.D);
        /*
        if (LeftDown == true && Time.unscaledTime < LeftDownTime)
        {
            input.LeftButton = true;

            LeftDown = false;
        }
        else if (RightDown == true && Time.unscaledTime < RightDownTime)
        {
            input.RightButton = true;

            RightDown = false;
        }*/

        if (PickThrowButton == true)
        {
            input.PickThrowButton = true;

            PickThrowButton = false;
        }

        pollInput.SetInput(input, DeterministicInputFlags.Repeatable);
    }

    public void LeftButtonDown()
    {
        LeftDown = true;
        LeftDownTime = Time.unscaledTime + 0.1f;
        RightDown = false;
    }

    public void RightButtonDown()
    {
        LeftDown = false;
        RightDownTime = Time.unscaledTime + 0.1f;
        RightDown = true;
    }

    public void SetJoyStickDir(float x, float y)
    {
        Joystick_X = x;
        Joystick_Y = y;
    }

    public void PickThrowDown()
    {
        PickThrowButton = true;
    }
}
