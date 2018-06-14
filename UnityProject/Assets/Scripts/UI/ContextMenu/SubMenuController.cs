
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using PlayGroup;
using UI;

public class  SubMenuController : MonoBehaviour
{

	//Prefab
	public GameObject actionButtonPrefab;

	//UnityAction delegate for the button action
	UnityAction action;
    public delegate void AddListItem();

    //Empty list to hold contextMenuItems
    [SerializeField]
	List<GameObject> contextMenuItems = new List<GameObject>();

	/// <summary>
	/// A method to be called outside of this component.
	/// When invoked, populates context menu dropdown with entries
	/// from the contextMethod attribute marked methods on 
	/// the target object t.
	/// </summary>
	/// <param name="t">Target Object</param>
	/// <param name="c">Content Holder Object</param>
	public void PopulateContextDropDown(GameObject t, RectTransform c)
	{
		//If an object was passed
		if (t != null)
		{
			//Find all monoBehaviours on object and store in a list
			MonoBehaviour[] scriptComponents = t.GetComponents<MonoBehaviour>();


			//For each monoBehaviour in the list of script components
			foreach (MonoBehaviour mono in scriptComponents)
			{
				//Declare an implicit type for mono
				var monoType = mono.GetType();

				//Iterate over each private/public instance method
				foreach (var method in monoType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
				{
					//Make sure it has the attribute on it


					var attributes = method.GetCustomAttributes(typeof(ContextMethod), true);
					//If it finds any matching attributes on the method...
					if (attributes.Length > 0)
					{
                        //Log the method to console
                        //Debug.Log("Script: " + mono + "Method: " + method);

						//Create the button as a child of contentHolder and store it as part of the list
						GameObject btn = Instantiate(actionButtonPrefab) as GameObject;
						contextMenuItems.Add(btn);
						btn.transform.SetParent(c);

						//Apply the relevant info to the button
						btn.GetComponentInChildren<Text>().text = ((ContextMethod)method.GetCustomAttributes(typeof(ContextMethod), true)[0]).contextTitle; //Text //action = (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), mono, method); //Bind the method to the UnityAction                                                                                                                               //btn.GetComponent<Toggle>()
                        if (((ContextMethod)method.GetCustomAttributes(typeof(ContextMethod), true)[0]).contextTitle == "Interact")
                        {
                            Debug.Log("Method found");
                            //action = (UnityAction)method.CreateDelegate(typeof(UnityAction)/*, mono, method*/);
                            //action = (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), PlayerManager.LocalPlayerScript.gameObject, PlayerManager.LocalPlayerScript.gameObject.transform.position, UIManager.Hands.CurrentSlot.Item, method);
                            btn.GetComponent<Button>().onClick.AddListener(() => t.GetComponent<Cupboards.ClosetControl>().Interact(PlayerManager.LocalPlayerScript.gameObject, PlayerManager.LocalPlayerScript.gameObject.transform.position, UIManager.Hands.CurrentSlot.eventName));
                        }

                    }
				}
			}
		}
	}
}
