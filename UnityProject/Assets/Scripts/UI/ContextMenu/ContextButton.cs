using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class ContextButton : MonoBehaviour {
	public GameObject target;

	private void Awake()
	{
		gameObject.GetComponentInChildren<Text>().text = target.name;
		gameObject.transform.Find("Image").GetComponent<Image>().sprite = target.GetComponentInChildren<SpriteRenderer>().sprite;
	}
}
