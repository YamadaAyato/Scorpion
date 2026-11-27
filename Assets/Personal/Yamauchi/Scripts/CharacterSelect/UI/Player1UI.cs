using UnityEngine;
using UnityEngine.UI;

public class Player1UI : PlayerUIBase
{
    private void OnEnable()
    {
        InputPlayer1.HandlerInputPlayer1 += Receive;
        WaitPlayer1.HandlerWaitPlayer1 += WReceive;
    }

    private void OnDisable()
    {
        InputPlayer1.HandlerInputPlayer1 -= Receive;
        WaitPlayer1.HandlerWaitPlayer1 -= WReceive;
    }   
}
