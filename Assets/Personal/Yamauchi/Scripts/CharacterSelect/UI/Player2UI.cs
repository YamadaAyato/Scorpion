
public class Player2UI : PlayerUIBase
{
    private void OnEnable()
    {
        InputPlayer2.HandlerInputPlayer2 += Receive;
        WaitPlayer2.HandlerWaitPlayer2 += WReceive;
    }

    private void OnDisable()
    {
        InputPlayer2.HandlerInputPlayer2 -= Receive;
        WaitPlayer2.HandlerWaitPlayer2 -= WReceive;
    }
}
