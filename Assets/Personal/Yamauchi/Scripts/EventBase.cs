using UnityEngine;

public class EventBase : MonoBehaviour
{
    private void OnEnable()
    {

    }

    private void OnDisable()
    {

    }
    //↑時間で発行するイベントを登録するなど…

    public virtual void Execute() { }
}
