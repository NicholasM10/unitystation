using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine;

public class ContextButton : MonoBehaviour {
	
	/// <summary>
	/// Method for use with objects in context menu. Initializes button and sets its label/image.
	/// </summary>
	/// <param name="labelText">Button label text</param>
	/// <param name="labelIcon">Button label icon</param>
	public void InitializeButton(string labelText, Sprite labelIcon)
	{
		gameObject.GetComponentInChildren<Text>().text = labelText; //Set label text
		GameObject imageObject = gameObject.transform.Find("Image").gameObject; //Set image object
		imageObject.SetActive(true); //Set image to active
		gameObject.transform.Find("Image").GetComponent<Image>().sprite = labelIcon; //Set sprite
	}

	/// <summary>
	/// Overloaded method to create a action button in context menu.
	/// </summary>
	/// <param name="labelText">Button label text</param>
	/// <param name="buttonAction">Button action UnityAction delegate</param>
	public void InitializeButton(string labelText, UnityAction buttonAction)
	{
		gameObject.GetComponentInChildren<Text>().text = labelText;
		gameObject.GetComponent<Button>().onClick.AddListener(buttonAction);
	}
}
