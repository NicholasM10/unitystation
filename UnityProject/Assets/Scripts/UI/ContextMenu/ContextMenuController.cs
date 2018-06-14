using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContextMenuController : MonoBehaviour {

	public RectTransform contentHolder;
	public GameObject target;
	public GameObject subMenu;

	// Use this for initialization
	void Start () {
        contentHolder = contentHolder.GetChild(0).GetComponent<RectTransform>();
		subMenu.GetComponent<SubMenuController>().PopulateContextDropDown(target, contentHolder);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
