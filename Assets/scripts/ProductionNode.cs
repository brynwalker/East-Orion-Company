﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProductionNode : MonoBehaviour, IndustryNode {

	[SerializeField]
	public List<ResourceFlow> inputs = new List<ResourceFlow> ();

	[SerializeField]
	public List<ResourceFlow> outputs = new List<ResourceFlow> ();

	[Range(1, 1000)]
	public float cycleTime = 3f;
	public bool producing = false;

	public StorageNode connectedStorageNode;

	public void Update () {
		if (RequirementsMet () && !producing)
			StartCoroutine (Produce());
	}

	private bool RequirementsMet() {
		foreach (ResourceFlow resource in inputs) {
			if (connectedStorageNode.HasResource (resource.type, resource.amount))
				continue;
			else
				return false;
		}

		return true;
	}

	private void DeductResources() {
		foreach (ResourceFlow resource in inputs)
			connectedStorageNode.Take(resource.type, resource.amount);
	}

	private IEnumerator Produce() {
		producing = true;
		DeductResources ();
		yield return new WaitForSeconds (cycleTime);
		foreach (ResourceFlow resource in outputs)
			connectedStorageNode.Put(resource.type, resource.amount);

		producing = false;
	}

	public string ObjectInfo() {
		string result = "ProductionNode\nInputs:\n\t";

		foreach (ResourceFlow resource in inputs)
			result += "Type: " + resource.type.ToString() + "\tAmount: " + resource.amount + "\n";

		result += "Outputs:\t";

		foreach (ResourceFlow resource in outputs)
			result += "\tType: " + resource.type.ToString() + "\n\tProduced per cycle: " + resource.amount + "\n\t" + "Cycle time: " + cycleTime + "\n";

		return result;
	}
}
