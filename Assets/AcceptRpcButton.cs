using UnityEngine;

public class AcceptRpcButton : MonoBehaviour
{
    public void Accept()
    {
        RPCManager.instance.AcceptRpc();
    }

    public void Cancel()
    {
        RPCManager.instance.CancelRpc();
    }
}