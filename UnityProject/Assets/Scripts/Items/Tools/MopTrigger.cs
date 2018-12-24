using UnityEngine;
using UnityEngine.Networking;

public class MopTrigger : PickUpTrigger
{
	[SyncVar]
	private bool canBeUsed = false;

    public override bool Interact (GameObject originator, Vector3 position, string hand)
    {
        //TODO:  Fill this in.

        if (UIManager.Hands.CurrentSlot.Item != gameObject)
        {
            return base.Interact (originator, position, hand);
        }
        var targetWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (/*canBeUsed && */PlayerManager.PlayerScript.IsInReach(targetWorldPos))
        {
			//TODO INSERT CLEANING FUNCTION
			//BloodSplat[] blood = MatrixManager.GetAt<BloodSplat>(targetWorldPos.CutToInt());
			if(blood != null)
			{
				//ReturnBloodToPool(blood);
			}

		}

        return base.Interact (originator, position, hand);
    }

	[Server]
	public void ReturnBloodToPool(BloodSplat blood)
	{
		PoolManager.Instance.PoolNetworkDestroy(blood.gameObject);
	}

    //Broadcast from EquipmentPool.cs **ServerSide**
    public void OnAddToPool ()
    {
        canBeUsed = true;
    }

    //Broadcast from EquipmentPool.cs **ServerSide**
    public void OnRemoveFromInventory ()
    {
        canBeUsed = false;
    }
}